using Wasm;
using Wasm2IL.Dwarf;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads CUSTOM sections of a WASM module, including DWARF debug info.
    /// </summary>
    public class CustomSectionReader : WasmSectionReaderBase
    {
        public CustomSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            var name = reader.ReadStrN();
            Log.WriteLine("Custom section name: {0}", name);

            if (name == "name")
            {
                ReadNameSection(reader);
            }
            else if (name == ".debug_abbrev")
            {
                Context.DwarfParser.ParseAbbrev(reader);
            }
            else if (name == ".debug_info")
            {
                var debugInfo = Context.DwarfParser.ParseDebugInfo(reader);
                Context.CompilationUnit = debugInfo.First();
            }
            else if (name == ".debug_str")
            {
                ReadDebugStringSection(reader);
            }
            else if (name == ".debug_types")
            {
                // not used yet.
            }
        }

        private void ReadNameSection(BinReader reader)
        {
            for (int i = 0; i < 3; i++)
            {
                var id = reader.ReadU8();
                var len = reader.ReadU32Leb();
                var next = reader.Position + len;
                Log.WriteLine("Name section {0} ({1} bytes)", id, len);

                if (id == 1)
                {
                    var names = reader.ReadU32Leb();
                    for (var nameId = 0; nameId < names; nameId++)
                    {
                        var idx = reader.ReadU32Leb();
                        var fname = reader.ReadStrN().Replace(":", "_");

                        if (idx < Context.ImportFuncs.Count)
                        {
                            var imp = Context.ImportFuncs[idx];
                            imp.CustomName = fname;
                        }
                        else
                        {
                            var f = Context.FuncDecls[(uint)(idx - Context.ImportFuncs.Count)];
                            f.ImportName = fname;
                            if (f.IsDefaultName)
                                f.Method.Name = fname;
                        }
                    }
                }

                reader.Position = next;
            }
        }

        private void ReadDebugStringSection(BinReader reader)
        {
            if (Context.CompilationUnit == null)
                return;

            var strTable = new DwarfStringTable(reader.ReadAllBytes());
            var root = Context.CompilationUnit.RootDIE;

            foreach (var thing in root.Children)
            {
                if (thing.Tag == DwarfTag.DW_TAG_subprogram)
                {
                    if (thing.Attributes.TryGetValue(AttributeEncoding.DW_AT_name, out var subProgramNameId)
                        && strTable.TryGetString((uint)subProgramNameId.Value, out var subProgramName))
                    {
                        Context.ParameterNames[subProgramName] =
                            thing.Children.Where(die => die.Tag == DwarfTag.DW_TAG_formal_parameter)
                                .Select(param =>
                                    param.Attributes
                                        .FirstOrDefault(attr => attr.Key == AttributeEncoding.DW_AT_name).Value)
                                .Select(attrValue =>
                                    attrValue?.Form == DwarfForm.DW_FORM_strp
                                        ? strTable.GetString((uint)attrValue.Value)
                                        : null)
                                .ToArray();
                    }
                }
            }
        }
    }
}
