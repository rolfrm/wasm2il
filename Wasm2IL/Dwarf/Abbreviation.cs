namespace Wasm2IL.Dwarf;

public class Abbreviation
{
    public ulong Code;
    public DwarfTag Tag;
    public bool HasChildren;
    public List<AttributeData> Attributes { get; set; } = new();

    public override string ToString() 
    => $"[{Code} {Tag}: {(HasChildren ? "leaf" : "node")}";
}