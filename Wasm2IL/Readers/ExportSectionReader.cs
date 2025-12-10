using Wasm;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the EXPORT section of a WASM module.
    /// </summary>
    public class ExportSectionReader : WasmSectionReaderBase
    {
        public ExportSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            var exportCount = reader.ReadU32Leb();
            for (uint i = 0; i < exportCount; i++)
            {
                var name = reader.ReadStrN();
                var type = (ImportType)reader.ReadU8();

                switch (type)
                {
                    case ImportType.FUNC:
                        uint index = reader.ReadU32Leb();
                        if (Context.ExportFuncs.ContainsKey(index))
                        {
                            Log.WriteLine("Export already defined: {0} {1} - {2}",
                                index, name, Context.ExportFuncs[index].Name);
                        }
                        else
                        {
                            Context.ExportFuncs[index] = new ImportFunc { Name = name, Index = index };
                        }
                        break;

                    case ImportType.TABLE:
                        index = reader.ReadU32Leb();
                        Context.ExportTables[index] = new ExportTable { Name = name, Index = index };
                        break;

                    case ImportType.MEM:
                        var memIndex = reader.ReadU32Leb();
                        Log.WriteLine("Memory: {0}", memIndex);
                        break;

                    case ImportType.GLOBAL:
                        var idx = reader.ReadU32Leb();
                        if (Context.Globals.TryGetValue(idx, out var glob))
                        {
                            glob.Field.Name = name;
                        }
                        Log.WriteLine("Global import: {0}   {1}", idx, name);
                        break;
                }
            }
        }
    }
}
