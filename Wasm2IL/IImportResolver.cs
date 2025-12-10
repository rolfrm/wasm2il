namespace Wasm2IL
{
    /// <summary>
    /// Interface for resolving imported methods from WASM modules.
    /// </summary>
    public interface IImportResolver
    {
        /// <summary>
        /// Resolves an imported method by module name and method name.
        /// </summary>
        object ResolveImportedMethod(string moduleName, string name);
    }
}
