namespace Wasm2IL;

public class WasmAttribute : Attribute
{
    /// <summary>
    /// If export name is null, just use the name of the method.
    /// </summary>
    public string ExportName { get; set; }
    public WasmAttribute(string importExportName)
    {
        ExportName = importExportName;
    }

    public WasmAttribute()
    {
            
    }
}