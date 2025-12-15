using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL;

class CodeGenContext
{
    public ILProcessor IL { get; }
    public MethodDefinition Method { get; }
    public Dictionary<int, Dictionary<TypeReference, VariableDefinition>> HelperVars { get; } = new();
    public Stack<TypeReference> TypeStack { get; } = new();
    public VariableDefinition HeapVar { get; }
    public bool HeapInited { get; set; }
    public TypeReference VoidType { get; }
    public FieldDefinition MemoryField { get; }

    public CodeGenContext(
        ILProcessor il,
        MethodDefinition method,
        VariableDefinition heapVar,
        TypeReference voidType,
        FieldDefinition memoryField)
    {
        IL = il;
        Method = method;
        HeapVar = heapVar;
        VoidType = voidType;
        MemoryField = memoryField;
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

    public void PushType(TypeReference? tr)
    {
        if (tr == null) throw new Exception("??");
        if (tr != VoidType)
            TypeStack.Push(tr);
    }

    public TypeReference PopType(int count = 1)
    {
        if (count == 0) return default;
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