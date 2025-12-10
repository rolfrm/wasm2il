using Mono.Cecil;
using Wasm;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the TYPE section of a WASM module.
    /// </summary>
    public class TypeSectionReader : WasmSectionReaderBase
    {
        public TypeSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            var typeCount = reader.ReadU32Leb();
            for (uint i = 0; i < typeCount; i++)
            {
                var header = reader.ReadU8();
                Assert.AreEqual(0x60, header);

                var paramCount = reader.ReadU32Leb();
                var paramTypes = new TypeReference[paramCount];
                for (int i2 = 0; i2 < paramCount; i2++)
                {
                    var t = reader.ReadU8();
                    paramTypes[i2] = Context.ByteToTypeReference(t);
                }

                var returnCount = reader.ReadU32Leb();
                Assert.IsTrue(returnCount < 2);
                TypeReference returnType = Context.VoidType;
                for (int i2 = 0; i2 < returnCount; i2++)
                    returnType = Context.ByteToTypeReference(reader.ReadU8());

                Context.Types[i] = new TypeId
                {
                    ReturnCount = returnCount,
                    ParamCount = paramCount,
                    ParamTypes = paramTypes,
                    ReturnType = returnType
                };
            }
        }
    }
}
