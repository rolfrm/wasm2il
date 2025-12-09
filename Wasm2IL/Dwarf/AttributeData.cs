namespace Wasm2IL.Dwarf;

public class AttributeData(AttributeEncoding attribute, DwarfForm form)
{
    public DwarfForm Form => form;
    public AttributeEncoding Name => attribute;
    
    public override string ToString() 
        => $"{Name} {Form}";
}