using Wasm;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Base class for all WASM section readers.
    /// </summary>
    public abstract class WasmSectionReaderBase
    {
        protected TransformerContext Context { get; }

        protected WasmSectionReaderBase(TransformerContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Reads the section from the binary reader.
        /// </summary>
        public abstract void Read(BinReader reader);
    }
}
