using Mono.Cecil.Cil;
using Wasm;
using Wasm2IL.Utils;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the MEMORY section of a WASM module.
    /// </summary>
    public class MemorySectionReader : WasmSectionReaderBase
    {
        public MemorySectionReader(TransformerContext context) : base(context)
        {
        }

        public override unsafe void Read(BinReader reader)
        {
            var memCount = reader.ReadU32Leb();
            Assert.AreEqual<uint>(1, memCount);

            for (uint i = 0; i < memCount; i++)
            {
                var type = reader.ReadU8();
                var min = reader.ReadU32Leb();

                var cctoril = Context.MainClass.GetStaticConstructor().Body.GetILProcessor();
                cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);

                if (type == 0)
                {
                    Log.WriteLine("Memory: {0} pages", min);
                    cctoril.Emit(OpCodes.Ldc_I4, (int)(min * TransformerContext.PageSize));
                    cctoril.Emit(OpCodes.Ldc_I8, 4L * 1024L * 1024L * 1024L); // Allocate 4GB of virtual memory
                    cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                    cctoril.Emit(OpCodes.Stsfld, Context.MemoryField);
                    cctoril.Emit(OpCodes.Stsfld, Context.MemoryFieldSize);
                }
                else if (type == 1)
                {
                    var max = reader.ReadU32Leb();
                    Log.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max,
                        min * TransformerContext.PageSize, max * TransformerContext.PageSize);

                    cctoril.Emit(OpCodes.Ldc_I8, 4L * 1024L * 1024L * 1024L); // Allocate 4GB of virtual memory
                    cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                    cctoril.Emit(OpCodes.Stsfld, Context.MemoryField);
                    cctoril.Emit(OpCodes.Ldc_I4, (int)(min * TransformerContext.PageSize));
                    cctoril.Emit(OpCodes.Stsfld, Context.MemoryFieldSize);
                }

                cctoril.Emit(OpCodes.Ret);
            }
        }
    }
}
