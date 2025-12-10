using Wasm;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the IMPORT section of a WASM module.
    /// </summary>
    public class ImportSectionReader : WasmSectionReaderBase
    {
        public ImportSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            var importCount = reader.ReadU32Leb();
            for (uint i = 0; i < importCount; i++)
            {
                var moduleName = reader.ReadStrN();
                var itemName = reader.ReadStrN();
                var type = (ImportType)reader.ReadU8();

                switch (type)
                {
                    case ImportType.FUNC:
                        var typeid = reader.ReadU32Leb();
                        var funid = (uint)Context.ImportFuncs.Count;
                        Context.ImportFuncs[funid] = new ImportFunc
                        {
                            Name = itemName,
                            TypeId = typeid,
                            Index = funid,
                            Module = moduleName
                        };
                        break;

                    case ImportType.TABLE:
                        var elemType = reader.ReadU8();
                        Assert.AreEqual(elemType, 0x70);
                        byte limitt = reader.ReadU8();
                        uint min = 0, max = 0;
                        if (limitt == 0)
                        {
                            min = reader.ReadU32Leb();
                            max = min;
                        }
                        else
                        {
                            min = reader.ReadU32Leb();
                            max = reader.ReadU32Leb();
                        }
                        Log.WriteLine("Table: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                        break;

                    case ImportType.GLOBAL:
                        var valType = reader.ReadU8();
                        bool mut = reader.ReadU8() > 0;
                        Log.WriteLine("Global: {0}.{1} {2}-{3}", moduleName, itemName, valType, mut);
                        break;

                    case ImportType.MEM:
                        limitt = reader.ReadU8();
                        if (limitt == 0)
                        {
                            min = reader.ReadU32Leb();
                            max = min;
                        }
                        else
                        {
                            min = reader.ReadU32Leb();
                            max = reader.ReadU32Leb();
                        }
                        Log.WriteLine("Memory: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                        break;
                }
            }
        }
    }
}
