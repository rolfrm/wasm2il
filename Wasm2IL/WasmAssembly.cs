using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace Wasm2IL;

public class WasmAssembly
{
    public string Name => code.Name;
    private readonly Assembly asm;
    private readonly Type code;
    private readonly MethodInfo malloc;
    private readonly MethodInfo free;
    private readonly FieldInfo memory;
    private readonly FieldInfo memorySize;
    private readonly FieldInfo functionTable;
    private object[] functionTableArray;
    private List<int> freeFunctions = new();

    public Assembly Assembly => asm;
    
    public WasmAssembly(Assembly asm)
    {
        this.asm = asm;
        this.code = asm.ExportedTypes.FirstOrDefault();
        malloc = code.GetMethod("malloc");
        free = code.GetMethod("free");
        memory = code.GetField("Memory");
        memorySize = code.GetField("MemorySize");
        functionTable = code.GetField("FunctionTable");
        
    }

    public int AssignCallbackFunction(Delegate d)
    {
        functionTableArray ??= functionTable.GetValue(null) as object[] ?? [];
        if (freeFunctions.Any())
        {
            var idx = freeFunctions.Last();
            freeFunctions.RemoveAt(freeFunctions.Count - 1);
            functionTableArray[idx] = d;
            return idx;
        }
        else
        {
            var idx = functionTableArray.Length;
            functionTableArray = [.. functionTableArray, null];
            functionTableArray[idx] = d;
            functionTable.SetValue(null, functionTableArray);
            return idx;
        }
    }

    public void FreeCallbackFunction(int idx)
    {
        functionTableArray ??= functionTable.GetValue(null) as object[];
        functionTableArray[idx] = null;
        freeFunctions.Add(idx);
    }

    public int Malloc(int len)
    {
        if (malloc == null)
        {
            return FakeMalloc(len);
        }
        int ptr = (int)malloc.Invoke(null, [len]);
        return ptr;
    }

    public void Free(int ptr)
    {
        if (malloc == null)
            return;
            // leak
        free.Invoke(null, [ptr]);
    }

    public unsafe int FakeMalloc(int len)
    {
        var p = new IntPtr(Pointer.Unbox(code.GetField("Memory").GetValue(null))); 
        int memSize = (int)memorySize.GetValue(null);
        
        p = Marshal.ReAllocHGlobal(p, memSize + len);
        code.GetField("Memory").SetValue(null, Pointer.Box(p.ToPointer(), typeof(byte*)));
        code.GetField("MemorySize").SetValue(null, memSize + len);
        return memSize;
    }

    public unsafe Span<byte> GetHeap()
    {
        return  new Span<byte>((System.Reflection.Pointer.Unbox(memory.GetValue(null))), 
            (int)code.GetField("MemorySize").GetValue(null));
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

    public object Invoke(string methodName, params object[] args)
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

            if (args[i] is Delegate d)
            {
                var idx =  this.AssignCallbackFunction(d);
                args[i] = idx;
                fcnToFree = fcnToFree.Add(idx);

            }
        }
        var m = code.GetMethod(methodName);
        var result = m
            .Invoke(null, args);
        foreach (var ptr in toFree)
            Free(ptr);
        foreach (var fcn in fcnToFree)
            FreeCallbackFunction(fcn);
        return result;
    }

    public MethodInfo GetMethod(string name)
    {
        return this.code.GetMethod(name);
    }
    
    public FieldInfo GetField(string name)
    {
        return this.code.GetField(name);
    }

    public Span<byte> GetHeapSpan(int i, int len)
    {
        return GetHeap().Slice(i, len);
    }
    public Span<T> GetHeapSpan<T>(int i, int len) where T: struct
    {
        return MemoryMarshal.Cast<byte, T>(GetHeap().Slice(i, len * Marshal.SizeOf<T>()));
    }

    public T GetHeapObject<T>(int ptr) where T: struct
    {
        var size = Marshal.SizeOf<T>();
        Span<byte> bytes = GetHeapSpan(ptr, size);
        Span<T> typedSpan = MemoryMarshal.Cast<byte, T>(bytes);
        return typedSpan[0];
    }

    public string GetHeapString(int ptr)
    {
        return new CString(GetHeap(), ptr).ToString();
    }

    public static int ReadOnlySpanLength(ReadOnlySpan<byte> span) =>  span.Length;
    public static int SpanLength(Span<byte> span) =>  span.Length;
    public static unsafe void CopyFromSpan(byte * data, Span<byte> span)
    {
        span.CopyTo(new Span<byte>(data, span.Length));
    }
    public static unsafe void CopyFromReadOnlySpan(byte * data, ReadOnlySpan<byte> span)
    {
        span.CopyTo(new Span<byte>(data, span.Length));
    }
    public static unsafe void CopyToSpan(Span<byte> span, byte * data)
    {
        new Span<byte>(data, span.Length).CopyTo(span);
    }

    public object LookupFunction(int i)
    {
        var ftable = (Array)code.GetField("FunctionTable", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        return ftable.GetValue(i);
    }
    static AssemblyBuilder asmBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("asdasd"), AssemblyBuilderAccess.RunAndCollect);
    private static ModuleBuilder moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");
    public T AsImplementation<T>()
    {
        var t = typeof(T);
        if (t.IsInterface == false)
            throw new ArgumentException("T");
        
        var typeBuilder = moduleBuilder.DefineType(t.Name + "Wrapper");
        typeBuilder.AddInterfaceImplementation(typeof(T));
        foreach (var method in typeof(T).GetMethods())
        {   
            var wasmAttr = method.GetCustomAttribute<WasmAttribute>();
            if (wasmAttr == null)
                throw new InvalidOperationException($"Method {method.Name} missing [Wasm] attribute");

            var staticMethod = code.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == (wasmAttr.ExportName ?? method.Name));
            
            if (staticMethod == null)
                throw new InvalidOperationException($"Static method {wasmAttr.ExportName ?? method.Name} not found in {code.Name}");

            var methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.ReturnType,
                method.GetParameters().Select(p => p.ParameterType).ToArray());

            var il = methodBuilder.GetILGenerator();
            if (method.ReturnType == typeof(void) && staticMethod.ReturnType != typeof(void))
            {
                throw new ImplementException(
                    $"API Specifies return void, but function {staticMethod.Name} returns a value.");
            }
            if (method.ReturnType != typeof(void) && staticMethod.ReturnType == typeof(void))
            {
                throw new ImplementException(
                    $"API Specifies returning a value, but function {staticMethod.Name} returns void.");
            }
            if (method.ReturnType.IsPointer)
            {
                il.Emit(OpCodes.Ldsfld, memory);
            }
            // Load parameters
            var paramters = method.GetParameters();
            List<LocalBuilder> freeLocals = new();
            List<(LocalBuilder, int)> copyBack = new(); 
            for (int i = 0; i < paramters.Length; i++)
            {
                var p = paramters[i];
                if (p.ParameterType == typeof(string))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    freeLocals.Add(loc);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    var lm = GetType().GetMethod(nameof(StringByteLength));
                    il.EmitCall(OpCodes.Call, lm, null);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    
                    il.EmitCall(OpCodes.Call, malloc, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                 
                    il.Emit(OpCodes.Ldsfld, memory);   
                    il.Emit(OpCodes.Add);
                    
                    var stringmethod = GetType().GetMethod(nameof(StringToHeap2));
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, stringmethod, [typeof(byte*), typeof(string)]);
                }else if (p.ParameterType == typeof(Span<byte>))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    il.Emit(OpCodes.Ldsfld, memory);
                    
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(SpanLength)), null);
                    il.EmitCall(OpCodes.Call, malloc, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                    freeLocals.Add(loc);
                    
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyFromSpan)), null);
                    il.Emit(OpCodes.Ldloc, loc);

                    copyBack.Add((loc, i + 1));
                    // copy the data to a
                }else if (p.ParameterType == typeof(ReadOnlySpan<byte>))
                {
                    var loc = il.DeclareLocal(typeof(int));
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(ReadOnlySpanLength)), null);
                    il.EmitCall(OpCodes.Call, malloc, null);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, loc);
                    freeLocals.Add(loc);
                    il.Emit(OpCodes.Ldsfld, memory);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyFromReadOnlySpan)), null);
                    il.Emit(OpCodes.Ldloc, loc);
                }
                else if (p.ParameterType.IsAssignableTo(typeof(Delegate)))
                {
                    throw new ImplementException(
                        "Delegates not yet supported in this API. They can be wrabbed manually");

                }
                else
                {
                    if (p.ParameterType.IsPointer)
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
            }

            LocalBuilder retLoc = null;
            // Call the static method
            il.Emit(OpCodes.Call, staticMethod);

            foreach (var (l, idx) in copyBack)
            {
                
                // get the span
                il.Emit(OpCodes.Ldarg, idx);
                
                // get the byte pointer address
                il.Emit(OpCodes.Ldloc, l);
                il.Emit(OpCodes.Ldsfld, memory);
                il.Emit(OpCodes.Add);
                // copy back to the span.
                il.EmitCall(OpCodes.Call, GetType().GetMethod(nameof(CopyToSpan)), null);
                
            }
            if (freeLocals.Any())
            {
                if (staticMethod.ReturnType != typeof(void))
                {
                    retLoc = il.DeclareLocal(staticMethod.ReturnType);
                    il.Emit(OpCodes.Stloc, retLoc);
                }

                foreach (var local in freeLocals)
                {
                    il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Call, free);
                }
            }

            if (retLoc != null)
                il.Emit(OpCodes.Ldloc, retLoc);
            if (method.ReturnType.IsPointer)
                il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        return (T) Activator.CreateInstance(typeBuilder.CreateType());
    }
}

public class ImplementException : Exception
{
    public ImplementException(string s) : base(s)
    {
        
    }
}