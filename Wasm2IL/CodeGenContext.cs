using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm2IL.Utils;

namespace Wasm2IL;

internal class CodeGenContext
{
    readonly BinReader reader;
    public CodeGenModuleContext ModuleContext { get; }
    public ILProcessor IL { get; }
    public MethodDefinition Method { get; }
    public Dictionary<int, Dictionary<TypeReference, VariableDefinition>> HelperVars { get; } = new();
    public Stack<TypeReference> TypeStack { get; } = new();
    public VariableDefinition HeapVar { get; }
    public bool HeapInited { get; set; }
    public TypeReference VoidType => ModuleContext.VoidType;
    public FieldDefinition MemoryField => ModuleContext.MemoryField;

    public CodeGenContext(
        CodeGenModuleContext moduleContext, ILProcessor il,
        MethodDefinition method,
        VariableDefinition heapVar,
        BinReader reader)
    {
        this.reader = reader;
        ModuleContext = moduleContext;
        IL = il;
        Method = method;
        HeapVar = heapVar;
    }

    public void EmitCallForOpcode(object instr)
    {
        if (ModuleContext.LibMethods.TryGetValue(instr, out var method3) ||
            (instr is Wasm.Instruction instr2 && (method3 = Lib.GetOthers(instr2)) != null))
        {
            if (method3.ReturnParameter.ParameterType == typeof(MethodInfo))
                method3 = (MethodInfo) method3.Invoke(null, method3.GetParameters().Length == 1 ? [instr] : []);

            foreach (var param in method3.GetParameters().Reversed())
            {
                if (param.GetCustomAttribute<WasmConstAttribute>() != null)
                    continue;
                var poppedType = PopType();
                var r = ModuleContext.TypeToRef(param.ParameterType);
                if (poppedType.Name != r.Name)
                {
                    // this might be a good place to check for errors, but 
                    // the stack is not currently correctly tracked.
                }
                else
                {

                }
            }

            foreach (var param in method3.GetParameters())
            {
                if (param.GetCustomAttribute<WasmConstAttribute>() != null)
                {
                    if (param.ParameterType == typeof(byte))
                    {
                        var b = reader.ReadByte();
                        IL.Emit(OpCodes.Ldc_I4, (int)b);
                    }

                }
            }

            if (method3.ReturnType != typeof(void))
            {
                PushType(ModuleContext.TypeToRef(method3.ReturnType));
            }
            
            EmitCall(method3);
        }
        else
        {
            throw new Exception("Unsupported opcode: " + instr);
        }
    }

    public void EmitCall(MethodInfo method)
    {
        var declType = IL.Body.Method.DeclaringType;
        var reference = declType.Module.ImportReference(method);
        reference = ModuleContext.MaybeWrap(reference);
        IL.Emit(OpCodes.Call, reference);
    }
    
    public VariableDefinition GetHelperVariable(TypeReference tr, int idx = 0)
    {
        if (!HelperVars.ContainsKey(idx))
            HelperVars[idx] = new();
        var dict = HelperVars[idx];
        if (tr == VoidType) throw new Exception("void type");
        if (dict.TryGetValue(tr, out var x))
            return x;
        var v = new VariableDefinition(tr);
        Method.Body.Variables.Add(v);
        dict[tr] = v;
        return v;
    }

    public void PushType(TypeReference tr)
    {
        if (tr == null) throw new Exception("??");
        if (tr != VoidType)
            TypeStack.Push(tr);
    }

    public TypeReference PopType(int count = 1)
    {
        if (count == 0) return null;
        while (count > 1)
        {
            TypeStack.Pop();
            count--;
        }

        return TypeStack.Pop();
    }

    public void LoadMemory()
    {
        if (!HeapInited)
        {
            HeapInited = true;
            IL.InsertAfter(0, IL.Create(OpCodes.Ldsfld, MemoryField));
            IL.InsertAfter(1, IL.Create(OpCodes.Stloc, HeapVar));
        }

        IL.Emit(OpCodes.Ldloc, HeapVar);
    }
}