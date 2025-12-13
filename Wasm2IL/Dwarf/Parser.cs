namespace Wasm2IL.Dwarf;

internal class Parser
{
    readonly AbbreviationTable _abbreviationTable = new();
    int _addressSize = 4;

    public List<DwarfCompilationUnit> ParseDebugInfo(BinReader reader)
    {
        var compilationUnits = new List<DwarfCompilationUnit>();
        while (!reader.IsAtEnd)
        {
            var cu = ParseCompilationUnit(reader);
            if (cu == null) break;
            compilationUnits.Add(cu);
        }
        return compilationUnits;
    }

    DwarfCompilationUnit? ParseCompilationUnit(BinReader reader)
    {
        var unitLength = reader.ReadU32();
        if (unitLength == 0) return null;

        var version = reader.ReadU16();
        var debugAbbrevOffset = reader.ReadU32();
        var addressSize = reader.ReadByte();

        _addressSize = addressSize;

        return new DwarfCompilationUnit
        {
            UnitLength = unitLength,
            Version = version,
            DebugAbbrevOffset = debugAbbrevOffset,
            AddressSize = addressSize,
            RootDIE = ParseDIE(reader)
        };
    }

    public void ParseAbbrev(BinReader reader)
    {
        while (true)
        {
            var code = reader.ReadU64Leb();
            if (code == 0) break;

            var tag = (DwarfTag)reader.ReadU64Leb();
            var hasChildren = reader.ReadByte() == 1;

            var abbrev = new Abbreviation
            {
                Code = code,
                Tag = tag,
                HasChildren = hasChildren
            };

            while (true)
            {
                var attrName = (AttributeEncoding)reader.ReadU64Leb();
                var attrForm = (DwarfForm)reader.ReadU64Leb();
                if (attrName == 0 && attrForm == 0) break;
                abbrev.Attributes.Add(new AttributeData(attrName, attrForm));
            }

            _abbreviationTable.Add(abbrev);
        }
    }

    DwarfDIE? ParseDIE(BinReader reader)
    {
        var abbreviationCode = reader.ReadU64Leb();
        if (abbreviationCode == 0) return null;

        var abbreviation = _abbreviationTable.GetAbbreviation(abbreviationCode)
            ?? throw new InvalidDataException($"Unknown abbreviation code: {abbreviationCode}");

        var die = new DwarfDIE
        {
            AbbreviationCode = abbreviationCode,
            Tag = abbreviation.Tag
        };

        foreach (var attrSpec in abbreviation.Attributes)
        {
            die.Attributes[attrSpec.Name] = new DwarfAttributeValue
            {
                Form = attrSpec.Form,
                Value = ReadAttributeValue(reader, attrSpec.Form)
            };
        }

        if (abbreviation.HasChildren)
        {
            while (true)
            {
                var child = ParseDIE(reader);
                if (child == null) break;
                die.Children.Add(child);
            }
        }

        return die;
    }

    object ReadAttributeValue(BinReader reader, DwarfForm form) => form switch
    {
        DwarfForm.DW_FORM_addr => ReadAddress(reader),
        DwarfForm.DW_FORM_block1 => reader.ReadBytes(reader.ReadByte()),
        DwarfForm.DW_FORM_block2 => reader.ReadBytes(reader.ReadU16()),
        DwarfForm.DW_FORM_block4 => reader.ReadBytes((int)reader.ReadU32()),
        DwarfForm.DW_FORM_block => reader.ReadBytes((int)reader.ReadU64Leb()),
        DwarfForm.DW_FORM_data1 => reader.ReadByte(),
        DwarfForm.DW_FORM_data2 => reader.ReadU16(),
        DwarfForm.DW_FORM_data4 => reader.ReadU32(),
        DwarfForm.DW_FORM_data8 => reader.ReadU64(),
        DwarfForm.DW_FORM_sdata => reader.ReadSLeb64(),
        DwarfForm.DW_FORM_udata => reader.ReadU64Leb(),
        DwarfForm.DW_FORM_string => ReadNullTerminatedString(reader),
        DwarfForm.DW_FORM_strp => reader.ReadU32(),
        DwarfForm.DW_FORM_flag => reader.ReadU8() != 0,
        DwarfForm.DW_FORM_flag_present => true,
        DwarfForm.DW_FORM_ref1 => reader.ReadU8(),
        DwarfForm.DW_FORM_ref2 => reader.ReadU16(),
        DwarfForm.DW_FORM_ref4 => reader.ReadU32(),
        DwarfForm.DW_FORM_ref8 => reader.ReadU64(),
        DwarfForm.DW_FORM_ref_udata => reader.ReadU64Leb(),
        DwarfForm.DW_FORM_ref_addr => ReadAddress(reader),
        DwarfForm.DW_FORM_ref_sig8 => reader.ReadU64(),
        DwarfForm.DW_FORM_sec_offset => reader.ReadU32(),
        DwarfForm.DW_FORM_exprloc => reader.ReadBytes((int)reader.ReadU64Leb()),
        DwarfForm.DW_FORM_indirect => ReadAttributeValue(reader, (DwarfForm)reader.ReadU64Leb()),
        _ => throw new NotImplementedException($"DWARF form {form} not implemented")
    };

    ulong ReadAddress(BinReader reader) => _addressSize switch
    {
        4 => reader.ReadU32(),
        8 => reader.ReadU64(),
        _ => throw new InvalidDataException($"Unsupported address size: {_addressSize}")
    };

    static string ReadNullTerminatedString(BinReader reader)
    {
        var bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadU8()) != 0)
            bytes.Add(b);
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    class AbbreviationTable
    {
        readonly Dictionary<ulong, Abbreviation> _data = new();

        public void Add(Abbreviation abbrev) => _data[abbrev.Code] = abbrev;

        public Abbreviation? GetAbbreviation(ulong code) =>
            _data.TryGetValue(code, out var abbrev) ? abbrev : null;
    }
}

public class DwarfCompilationUnit
{
    public uint UnitLength { get; set; }
    public ushort Version { get; set; }
    public uint DebugAbbrevOffset { get; set; }
    public byte AddressSize { get; set; }
    public DwarfDIE? RootDIE { get; set; }
}

public class DwarfDIE
{
    public ulong AbbreviationCode { get; set; }
    public DwarfTag Tag { get; set; }
    public Dictionary<AttributeEncoding, DwarfAttributeValue> Attributes { get; } = new();
    public List<DwarfDIE> Children { get; } = new();
}

public class DwarfAttributeValue
{
    public DwarfForm Form { get; set; }
    public object? Value { get; set; }
}
