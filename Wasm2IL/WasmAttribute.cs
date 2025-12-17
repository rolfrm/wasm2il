namespace Wasm2IL;

/// <summary>
/// Marks a method with WASM import/export name. If null, uses the method name.
/// </summary>
public class WasmAttribute(string exportName = null) : Attribute
{
    public string ExportName { get; } = exportName;
}
