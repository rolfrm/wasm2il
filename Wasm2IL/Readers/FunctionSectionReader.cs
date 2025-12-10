using Mono.Cecil;
using Wasm;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the FUNCTION section of a WASM module.
    /// </summary>
    public class FunctionSectionReader : WasmSectionReaderBase
    {
        public FunctionSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            uint funcCount = reader.ReadU32Leb();
            Log.WriteLine("Func count: {0}", funcCount);

            for (uint i = 0; i < funcCount; i++)
            {
                uint typeid = reader.ReadU32Leb();

                Context.FuncDecls[i] = new FuncDeclType
                {
                    TypeId = typeid,
                    Method = new MethodDefinition("func" + i,
                        MethodAttributes.Static | MethodAttributes.Public,
                        Context.VoidType)
                };
            }
        }
    }
}
