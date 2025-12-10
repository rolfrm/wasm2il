using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm;

namespace Wasm2IL.CodeGen
{
    /// <summary>
    /// Holds the state for generating a single method's IL code.
    /// </summary>
    public class MethodGenerationContext
    {
        public TransformerContext TransformerContext { get; }
        public MethodDefinition Method { get; }
        public ILProcessor IL { get; }
        public BinReader Reader { get; }
        public TypeId FunctionType { get; }

        // Stack type tracking for SELECT instruction
        public Stack<TypeReference> TypeStack { get; } = new();

        // Label stack for control flow
        public List<LabelType> LabelStack { get; } = new();

        // Helper variables
        public Dictionary<int, Dictionary<TypeReference, VariableDefinition>> HelperVars { get; } = new();
        public VariableDefinition HeapAddress { get; }
        public VariableDefinition HeapVar { get; }

        // Memory initialization flag
        public bool HeapInited { get; set; }

        public MethodGenerationContext(TransformerContext transformerContext,
            MethodDefinition method,
            ILProcessor il,
            BinReader reader,
            TypeId functionType)
        {
            TransformerContext = transformerContext;
            Method = method;
            IL = il;
            Reader = reader;
            FunctionType = functionType;

            HeapAddress = new VariableDefinition(transformerContext.I32Type);
            method.Body.Variables.Add(HeapAddress);

            HeapVar = new VariableDefinition(transformerContext.ByteType.MakePointerType());
            method.Body.Variables.Add(HeapVar);

            LabelStack.Add(new LabelType()); // base label
        }

        public VariableDefinition GetVariable(TypeReference tr, int idx = 0)
        {
            if (!HelperVars.ContainsKey(idx))
                HelperVars[idx] = new();

            var dict = HelperVars[idx];
            if (tr == TransformerContext.VoidType)
                throw new Exception("void type");

            if (dict.TryGetValue(tr, out var x))
                return x;

            var v = new VariableDefinition(tr);
            Method.Body.Variables.Add(v);
            dict[tr] = v;
            return v;
        }

        public void Push(TypeReference? tr)
        {
            if (tr == null) throw new Exception("??");
            if (tr != TransformerContext.VoidType)
                TypeStack.Push(tr);
        }

        public TypeReference Pop(int count = 1)
        {
            if (count == 0) return default;
            while (count > 1)
            {
                TypeStack.Pop();
                count--;
            }
            return TypeStack.Pop();
        }
    }

    public class LabelType
    {
        public byte Type;
        public Instruction? EndLabel;
        public bool Forward;
        public Instruction? StartLabel;
    }
}
