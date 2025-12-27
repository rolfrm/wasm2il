namespace Wasm2IL;

public class FunctionExportAttribute : Attribute
{
    public FunctionExportAttribute(int id)
    {
        Id = id;
    }

    public int Id { get; }
}