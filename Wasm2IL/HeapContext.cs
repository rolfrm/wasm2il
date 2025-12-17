namespace Wasm2IL;

public struct HeapContext
{
    public Type Module { get; private set; }

    public static HeapContext Create(RuntimeTypeHandle module) =>
        new() { Module = Type.GetTypeFromHandle(module) };
}
