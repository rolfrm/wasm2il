using Mono.Cecil;

namespace Wasm2IL;

public class ResolveImportEventArgs : EventArgs
{
    public TypeDefinition TypeBuilder { get; set; }
    public string ModuleName { get; set; }
    public string Name { get; set; }
    public object Result { get; set; }
    public bool Handled { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
}