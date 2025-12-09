namespace Wasm2IL.Dwarf;

internal class Parser
{
    public class AbbreviationTable
    {
        public Dictionary<ulong, Abbreviation> AbbreviationData = new();
        
        public void Add(Abbreviation abbrev)
        {
            AbbreviationData[abbrev.Code] = abbrev;
        }
        
        public Abbreviation? GetAbbreviation(ulong code)
        {
            return AbbreviationData.TryGetValue(code, out var abbrev) ? abbrev : null;
        }
    }
    public List<DwarfCompilationUnit> ParseDebugInfo(BinReader _reader)
    {
        var compilationUnits = new List<DwarfCompilationUnit>();

        while (_reader.ReadToEnd() == false)
        {
            var cu = ParseCompilationUnit(_reader);
            if (cu == null) break;
            compilationUnits.Add(cu);
        }

        return compilationUnits;
    }

    private DwarfCompilationUnit ParseCompilationUnit(BinReader _reader)
    {
        
        // Read compilation unit header
        var unitLength = _reader.ReadU32();
        if (unitLength == 0) return null; // End of debug_info

        var version = _reader.ReadU16();
        var debugAbbrevOffset = _reader.ReadU32();
        var addressSize = _reader.ReadByte();

        _addressSize = addressSize;

        var cu = new DwarfCompilationUnit
        {
            UnitLength = unitLength,
            Version = version,
            DebugAbbrevOffset = debugAbbrevOffset,
            AddressSize = addressSize
        };

        // Parse the DIE tree (root should be DW_TAG_compile_unit)
        cu.RootDIE = ParseDIE(_reader);

        return cu;
    }

    private AbbreviationTable _abbreviationTable = new();
    public void ParseAbbrev(BinReader reader)
    {
        var table = _abbreviationTable;
        
        while (true)
        {
            var code = reader.ReadU64Leb();
            if (code == 0) 
            {
                // End of abbreviation table
                break;
            }

            var tag = (DwarfTag)reader.ReadU64Leb();
            var hasChildren = reader.ReadByte() == 1;

            var abbrev = new Abbreviation
            {
                Code = code,
                Tag = tag,
                HasChildren = hasChildren
            };

            // Read attributes until we hit 0, 0
            while (true)
            {
                var attrName = (AttributeEncoding)reader.ReadU64Leb();
                var attrForm = (DwarfForm) reader.ReadU64Leb();

                if (attrName == 0 && attrForm == 0) 
                {
                    // End of this abbreviation's attributes
                    break;
                }

                abbrev.Attributes.Add(new AttributeData(attrName, attrForm));
            }

            table.Add(abbrev);
        }

    }
    DwarfDIE ParseDIE(BinReader reader)
    {
        var abbreviationCode = reader.ReadU64Leb();
        
        if (abbreviationCode == 0)
        {
            // Null entry - signals end of children
            return null;
        }

        var abbreviation = _abbreviationTable.GetAbbreviation(abbreviationCode);
        if (abbreviation == null)
        {
            throw new InvalidDataException($"Unknown abbreviation code: {abbreviationCode}");
        }

        var die = new DwarfDIE
        {
            AbbreviationCode = abbreviationCode,
            Tag = abbreviation.Tag
        };
        if (die.Tag == DwarfTag.DW_TAG_formal_parameter)
        {
            
        }
        // Read all attributes for this DIE
        foreach (var attrSpec in abbreviation.Attributes)
        {
            var value = ReadAttributeValue(reader, attrSpec.Form);
            die.Attributes[attrSpec.Name] = new DwarfAttributeValue
            {
                Form = attrSpec.Form,
                Value = value
            };
        }

        // If this DIE has children, parse them recursively
        if (abbreviation.HasChildren)
        {
            while (true)
            {
                var child = ParseDIE(reader);
                if (child == null) break; // Null entry = end of children
                die.Children.Add(child);
            }
        }

        return die;
    }

    private object ReadAttributeValue(BinReader _reader, DwarfForm form)
    {
        switch (form)
        {
            case DwarfForm.DW_FORM_addr:
                return ReadAddress(_reader);

            case DwarfForm.DW_FORM_block1:
                {
                    var length = _reader.ReadByte();
                    return _reader.ReadBytes(length);
                }

            case DwarfForm.DW_FORM_block2:
                {
                    var length = _reader.ReadU16();
                    return _reader.ReadBytes(length);
                }

            case DwarfForm.DW_FORM_block4:
                {
                    var length = (int)_reader.ReadU32();
                    return _reader.ReadBytes(length);
                }

            case DwarfForm.DW_FORM_block:
                {
                    var length = _reader.ReadU64Leb();
                    return _reader.ReadBytes((int)length);
                }

            case DwarfForm.DW_FORM_data1:
                return _reader.ReadByte();

            case DwarfForm.DW_FORM_data2:
                return _reader.ReadU16();

            case DwarfForm.DW_FORM_data4:
                return _reader.ReadU32();

            case DwarfForm.DW_FORM_data8:
                return _reader.ReadU64();

            case DwarfForm.DW_FORM_sdata:
                return _reader.ReadSLeb64();

            case DwarfForm.DW_FORM_udata:
                return _reader.ReadU64Leb();

            case DwarfForm.DW_FORM_string:
                return ReadNullTerminatedString(_reader);

            case DwarfForm.DW_FORM_strp:
                return _reader.ReadU32(); // Offset into .debug_str section

            case DwarfForm.DW_FORM_flag:
                return _reader.ReadByte() != 0;

            case DwarfForm.DW_FORM_flag_present:
                return true; // Presence of attribute means true

            case DwarfForm.DW_FORM_ref1:
                return _reader.ReadByte();

            case DwarfForm.DW_FORM_ref2:
                return _reader.ReadU16();

            case DwarfForm.DW_FORM_ref4:
                return _reader.ReadU32();

            case DwarfForm.DW_FORM_ref8:
                return _reader.ReadU64();

            case DwarfForm.DW_FORM_ref_udata:
                return _reader.ReadU64Leb();

            case DwarfForm.DW_FORM_ref_addr:
                return ReadAddress(_reader);

            case DwarfForm.DW_FORM_ref_sig8:
                return _reader.ReadU64(); // Type signature

            case DwarfForm.DW_FORM_sec_offset:
                return _reader.ReadU32(); // Or UInt64 in 64-bit DWARF

            case DwarfForm.DW_FORM_exprloc:
            {
                var length = _reader.ReadU64Leb();
                    return _reader.ReadBytes((int)length);
                }

            case DwarfForm.DW_FORM_indirect:
                // The actual form follows as ULEB128
                var actualForm = (DwarfForm)_reader.ReadU64Leb();
                return ReadAttributeValue(_reader, actualForm);

            default:
                throw new NotImplementedException($"Form {form} not implemented");
        }

       
    }
    int _addressSize = 4;
    private ulong ReadAddress(BinReader _reader)
    {
        switch (_addressSize)
        {
            case 4:
                return _reader.ReadU32();
            case 8:
                return _reader.ReadU64();
            default:
                throw new InvalidDataException($"Unsupported address size: {_addressSize}");
        }
    }

    private string ReadNullTerminatedString(BinReader _reader)
    {
        var bytes = new List<byte>();
        byte b;
        while ((b = _reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
}

public class DwarfCompilationUnit
{
    public uint UnitLength { get; set; }
    public ushort Version { get; set; }
    public uint DebugAbbrevOffset { get; set; }
    public byte AddressSize { get; set; }
    public DwarfDIE RootDIE { get; set; }
}

public class DwarfDIE
{
    public ulong AbbreviationCode { get; set; }
    public DwarfTag Tag { get; set; }
    public Dictionary<AttributeEncoding, DwarfAttributeValue> Attributes { get; set; }
    public List<DwarfDIE> Children { get; set; }

    public DwarfDIE()
    {
        Attributes = new Dictionary<AttributeEncoding, DwarfAttributeValue>();
        Children = new List<DwarfDIE>();
    }
}

public class DwarfAttributeValue
{
    public DwarfForm Form { get; set; }
    public object Value { get; set; }  // Can be various types depending on form
}