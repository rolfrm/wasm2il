using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace Wasm2IL;

public class WasmAssembly
{
    static readonly AssemblyBuilder AsmBuilder = AssemblyBuilder.DefineDynamicAssembly(
        new AssemblyName("WasmWrapper"), AssemblyBuilderAccess.RunAndCollect);

    static readonly ModuleBuilder ModuleBuilder = AsmBuilder.DefineDynamicModule("MainModule");

    readonly Assembly asm;
    readonly Type code;
    readonly MethodInfo? malloc;
    readonly MethodInfo? free;
    readonly FieldInfo memory;
    readonly FieldInfo memorySize;
    readonly FieldInfo? functionTable;
    readonly List<int> freeFunctions = new();
    object[]? functionTableArray;

    public string Name => code.Name;
    public Assembly Assembly => asm;

    public WasmAssembly(Assembly asm)
    {
        this.asm = asm;
        code = asm.ExportedTypes.FirstOrDefault()
            ?? throw new InvalidOperationException("No exported types in assembly");
        malloc = code.GetMethod("malloc");
        free = code.GetMethod("free");
        memory = code.GetField("Memory")
            ?? throw new InvalidOperationException("Memory field not found");
        memorySize = code.GetField("MemorySize")
            ?? throw new InvalidOperationException("MemorySize field not found");
        functionTable = code.GetField("FunctionTable");
    }

    public int AssignCallbackFunction(Delegate d)
    {
        if (functionTable == null)
            throw new InvalidOperationException("FunctionTable not available");

        functionTableArray ??= functionTable.GetValue(null) as object[] ?? [];

        if (freeFunctions.Count > 0)
        {
            var idx = freeFunctions[^1];
            freeFunctions.RemoveAt(freeFunctions.Count - 1);
            functionTableArray[idx] = d;
            return idx;
        }

        var newIdx = functionTableArray.Length;
        functionTableArray = [.. functionTableArray, d];
        functionTable.SetValue(null, functionTableArray);
        return newIdx;
    }

    public void FreeCallbackFunction(int idx)
    {
        if (functionTableArray == null)
            throw new InvalidOperationException("FunctionTable not initialized");
        functionTableArray[idx] = null!;
        freeFunctions.Add(idx);
    }

    public int Malloc(int len)
    {
        if (malloc == null)
            return FakeMalloc(len);
        return (int)malloc.Invoke(null, [len])!;
    }

    public void Free(int ptr)
    {
        if (free == null) return;
        free.Invoke(null, [ptr]);
    }

    public unsafe int FakeMalloc(int len)
    {
        int memSize = (int)memorySize.GetValue(null)!;
        memorySize.SetValue(null, memSize + len);
        return memSize;
    }

    public unsafe Span<byte> GetHeap()
    {
        var ptr = Pointer.Unbox(memory.GetValue(null)!);
        var size = (int)memorySize.GetValue(null)!;
        return new Span<byte>(ptr, size);
    }

    public static int StringByteLength(string str) => System.Text.Encoding.UTF8.GetByteCount(str);

    public static unsafe void StringToHeap2(byte* buffer, string str)
    {
        var bc = System.Text.Encoding.UTF8.GetByteCount(str);
        var span = new Span<byte>(buffer, bc + 1);
        span[bc] = 0;
        System.Text.Encoding.UTF8.GetBytes(str, span);
    }

    public int StringToHeap(string str)
    {
        var bc = System.Text.Encoding.UTF8.GetByteCount(str);
        int s = Malloc(bc + 1);
        var span = GetHeap().Slice(s, bc + 1);
        span[bc] = 0;
        System.Text.Encoding.UTF8.GetBytes(str, span);
        return s;
    }

    public object? Invoke(string methodName, params object[] args)
    {
        var toFree = ImmutableList<int>.Empty;
        var fcnToFree = ImmutableList<int>.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is string str)
            {
                var ptr = StringToHeap(str);
                args[i] = ptr;
                toFree = toFree.Add(ptr);
            }
            else if (args[i] is Delegate d)
            {
                var idx = AssignCallbackFunction(d);
                args[i] = idx;
                fcnToFree = fcnToFree.Add(idx);
            }
        }

        var m = code.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found");
        var result = m.Invoke(null, args);

        foreach (var ptr in toFree)
            Free(ptr);
        foreach (var fcn in fcnToFree)
            FreeCallbackFunction(fcn);

        return result;
    }

    public MethodInfo? GetMethod(string name) => code.GetMethod(name);
    public FieldInfo? GetField(string name) => code.GetField(name);

    public Span<byte> GetHeapSpan(int i, int len) => GetHeap().Slice(i, len);

    public Span<T> GetHeapSpan<T>(int i, int len) where T : struct =>
        MemoryMarshal.Cast<byte, T>(GetHeap().Slice(i, len * Marshal.SizeOf<T>()));

    public T GetHeapObject<T>(int ptr) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        return MemoryMarshal.Cast<byte, T>(GetHeapSpan(ptr, size))[0];
    }

    public string GetHeapString(int ptr) => new CString(GetHeap(), ptr).ToString();

    public static int ReadOnlySpanLength(ReadOnlySpan<byte> span) => span.Length;
    public static int SpanLength(Span<byte> span) => span.Length;

    public static unsafe void CopyFromSpan(byte* data, Span<byte> span) =>
        span.CopyTo(new Span<byte>(data, span.Length));

    public static unsafe void CopyFromReadOnlySpan(byte* data, ReadOnlySpan<byte> span) =>
        span.CopyTo(new Span<byte>(data, span.Length));

    public static unsafe void CopyToSpan(Span<byte> span, byte* data) =>
        new Span<byte>(data, span.Length).CopyTo(span);

    public object? LookupFunction(int i)
    {
        var ftable = code.GetField("FunctionTable", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as Array;
        return ftable?.GetValue(i);
    }

    public T AsImplementation<T>()
    {
        var t = typeof(T);
        if (!t.IsInterface)
            throw new ArgumentException($"Type {t.Name} must be an interface", nameof(T));

        var typeName = t.Name + "Wrapper";

        if (ModuleBuilder.GetType(typeName) is { } existingType)
            return (T)Activator.CreateInstance(existingType)!;

        var typeBuilder = ModuleBuilder.DefineType(typeName);
        typeBuilder.AddInterfaceImplementation(typeof(T));

        foreach (var method in typeof(T).GetMethods())
        {
            var wasmAttr = method.GetCustomAttribute<WasmAttribute>() ?? new WasmAttribute(method.Name);
            var targetName = wasmAttr.ExportName ?? method.Name;

            var staticMethod = code.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == targetName)
                ?? throw new InvalidOperationException($"Static method '{targetName}' not found in {code.Name}");

            if (method.ReturnType == typeof(void) && staticMethod.ReturnType != typeof(void))
                throw new ImplementException(
                    $"Interface specifies void return but {staticMethod.Name} returns a value");

            if (method.ReturnType != typeof(void) && staticMethod.ReturnType == typeof(void))
                throw new ImplementException(
                    $"Interface specifies return value but {staticMethod.Name} returns void");

            var methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.ReturnType,
                method.GetParameters().Select(p => p.ParameterType).ToArray());

            var il = methodBuilder.GetILGenerator();

            if (method.ReturnType.IsPointer)
                il.Emit(OpCodes.Ldsfld, memory);

            var parameters = method.GetParameters();
            List<LocalBuilder> freeLocals = new();
            List<(LocalBuilder, int)> copyBack = new();

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.ParameterType == typeof(string))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    freeLocals.Add(loc);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(StringByteLength))!, null);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    il.EmitCall(OpCodes.Call, malloc!, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                    il.Emit(OpCodes.Ldsfld, memory);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(StringToHeap2))!, [typeof(byte*), typeof(string)]);
                }
                else if (p.ParameterType == typeof(Span<byte>))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    il.Emit(OpCodes.Ldsfld, memory);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(SpanLength))!, null);
                    il.EmitCall(OpCodes.Call, malloc!, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                    freeLocals.Add(loc);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyFromSpan))!, null);
                    il.Emit(OpCodes.Ldloc, loc);
                    copyBack.Add((loc, i + 1));
                }
                else if (p.ParameterType == typeof(ReadOnlySpan<byte>))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(ReadOnlySpanLength))!, null);
                    il.EmitCall(OpCodes.Call, malloc!, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                    freeLocals.Add(loc);
                    il.Emit(OpCodes.Ldsfld, memory);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyFromReadOnlySpan))!, null);
                    il.Emit(OpCodes.Ldloc, loc);
                }
                else if (p.ParameterType.IsAssignableTo(typeof(Delegate)))
                {
                    throw new ImplementException("Delegates not yet supported. They can be wrapped manually.");
                }
                else if (p.ParameterType.IsPointer)
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.Emit(OpCodes.Ldsfld, memory);
                    il.Emit(OpCodes.Sub);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                }
            }

            LocalBuilder? retLoc = null;
            il.Emit(OpCodes.Call, staticMethod);

            foreach (var (l, idx) in copyBack)
            {
                il.Emit(OpCodes.Ldarg, idx);
                il.Emit(OpCodes.Ldloc, l);
                il.Emit(OpCodes.Ldsfld, memory);
                il.Emit(OpCodes.Add);
                il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyToSpan))!, null);
            }

            if (freeLocals.Count > 0)
            {
                if (staticMethod.ReturnType != typeof(void))
                {
                    retLoc = il.DeclareLocal(staticMethod.ReturnType);
                    il.Emit(OpCodes.Stloc, retLoc);
                }

                foreach (var local in freeLocals)
                {
                    il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Call, free!);
                }
            }

            if (retLoc != null)
                il.Emit(OpCodes.Ldloc, retLoc);
            if (method.ReturnType.IsPointer)
                il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        return (T)Activator.CreateInstance(typeBuilder.CreateType()!)!;
    }
}

public class ImplementException : Exception
{
    public ImplementException(string s) : base(s) { }
}
