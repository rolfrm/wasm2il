using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Wasm2IL.Utils;

namespace Wasm2IL;

internal class CodeGenModuleContext
{
    
    public Dictionary<object, MethodInfo> LibMethods = new();
    
    TypeReference f32Type, f64Type, i64Type, i16Type, i32Type, voidType, byteType, intPtrType, voidPtrType;
    TypeReference v128Type;

    
    public CodeGenModuleContext(TypeReference voidType, FieldDefinition memoryField, ModuleDefinition module,
        TypeDefinition @class)
    {
        VoidType = voidType;
        MemoryField = memoryField;
        Module = module;
        Class = @class;

        f32Type = Module.TypeSystem.Single;
        f64Type = Module.TypeSystem.Double;
        i64Type = Module.TypeSystem.Int64;
        i32Type = Module.TypeSystem.Int32;
        i16Type = Module.TypeSystem.Int16;
        voidType = Module.TypeSystem.Void;
        byteType = Module.TypeSystem.Byte;
        intPtrType = Module.TypeSystem.IntPtr;
        voidPtrType = Module.TypeSystem.Void.MakePointerType();
        v128Type = Module.ImportReference(typeof(Vector128<byte>));

        foreach (var libMethods in typeof(Lib).GetMethods())
        {
            foreach (var attr in libMethods.GetCustomAttributes<WasmOpcodeAttribute>())
            {
                var key = attr.Key;
                LibMethods.Add(key, libMethods);
                if (!libMethods.IsStatic)
                    throw new InvalidOperationException("Methods must be static for injecting at compile time.");
            }
        }
    }
    
    public Type RefToType(TypeReference r)
    {
        if (r == i32Type) return typeof(int);
        if (r == i64Type) return typeof(long);
        if (r == f32Type) return typeof(float);
        if (r == f64Type) return typeof(double);
        if (r == v128Type) return typeof(Vector128<byte>);
        return typeof(void);
    }

    public TypeReference TypeToRef(Type r)
    {
        if (r == typeof(bool)) return i32Type;
        if (r == typeof(byte)) return i32Type;
        if (r == typeof(sbyte)) return i32Type;
        if (r == typeof(short)) return i32Type;
        if (r == typeof(ushort)) return i32Type;
        if (r == typeof(int)) return i32Type;
        if (r == typeof(uint)) return i32Type;
        if (r == typeof(long)) return i64Type;
        if (r == typeof(ulong)) return i64Type;
        if (r == typeof(float)) return f32Type;
        if (r == typeof(double)) return f64Type;
        if (r == typeof(Vector128<byte>)) return v128Type;
        if (r == typeof(Vector128<sbyte>)) return v128Type;
        if (r == typeof(Vector128<int>)) return v128Type;
        if (r == typeof(Vector128<short>)) return v128Type;
        if (r == typeof(Vector128<uint>)) return v128Type;
        if (r == typeof(Vector128<ushort>)) return v128Type;
        if (r == typeof(Vector128<ulong>)) return v128Type;
        if (r == typeof(Vector128<long>)) return v128Type;
        if (r == typeof(Vector128<float>)) return v128Type;
        
        if (r == typeof(Vector128<double>)) return v128Type;
        if (r == typeof(void)) return voidType;
        if (r.IsPointer) return i32Type;
        throw new Exception("unrecognized type");
    }
    
    public FieldDefinition MemoryField { get; }
    public ModuleDefinition Module { get; }
    public TypeDefinition Class { get; }

    public TypeReference VoidType { get; }

    public VariableDefinition HeapVar { get; }
    readonly Dictionary<MethodReference, MethodReference> wrappedMethods = new();
    public MethodReference MaybeWrap(MethodReference fcn)
    {
        if (wrappedMethods.TryGetValue(fcn, out var fcn2))
            return fcn2;
        fcn2 = fcn;
        foreach (var param in fcn.Parameters)
        {
            if (param.ParameterType.Name == "CString"
                || param.ParameterType.IsPointer
                || param.ParameterType.Name == "HeapContext")
            {
                fcn2 = WrapMethod(fcn2);
                break;
            }
        }

        wrappedMethods[fcn] = fcn2;
        return fcn2;
    }
    
    unsafe MethodReference WrapMethod(MethodReference m)
        {
            if (Class.Methods.FirstOrDefault(x => x.Name == m.Name + "__wrap") is { } ext)
                return ext;

            var m2 = new MethodDefinition(m.Name + "__wrap",
                Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Public, m.ReturnType);
            var methodImplConstructor = typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]);
            var attr = new CustomAttribute(Module.ImportReference(methodImplConstructor));
            attr.ConstructorArguments.Add(new CustomAttributeArgument(
                Module.ImportReference(typeof(MethodImplOptions)), MethodImplOptions.AggressiveInlining));
            m2.CustomAttributes.Add(attr);

            Class.Methods.Add(m2);
            var il2 = m2.Body.GetILProcessor();
            int argidx = 0;
            foreach (var p in m.Parameters)
            {
                var p2 = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                m2.Parameters.Add(p2);
                if (p.ParameterType.Name == "HeapContext")
                {
                    p2.ParameterType = Module.ImportReference(typeof(Type));
                    il2.Emit(OpCodes.Ldtoken, Class);
                    il2.EmitCall(() => HeapContext.Create);
                    argidx--;
                    m2.Parameters.Remove(p2);
                }
                else if (p.ParameterType.Name == "CString")
                {
                    p2.ParameterType = i32Type;

                    il2.Emit(OpCodes.Ldsfld, MemoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.EmitCall(() => CString.New2);
                }
                else if (p.ParameterType.IsPointer)
                {
                    p2.ParameterType = i32Type;
                    il2.Emit(OpCodes.Ldsfld, MemoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.Emit(OpCodes.Add);
                }
                else
                {
                    il2.Emit(OpCodes.Ldarg, argidx);
                }

                argidx++;
            }

            il2.Emit(OpCodes.Call, m);
            if (m.ReturnType.IsPointer)
            {
                il2.Emit(OpCodes.Ldsfld, MemoryField);
                il2.Emit(OpCodes.Sub);
                il2.Emit(OpCodes.Conv_I4);
                m2.ReturnType = i32Type;
            }

            il2.Emit(OpCodes.Ret);
            return m2;
        }
}