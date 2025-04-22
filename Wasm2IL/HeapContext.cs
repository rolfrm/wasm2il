namespace Wasm2IL;

public struct HeapContext
{
    public Type Module { get; private set; }

    public static HeapContext Create(RuntimeTypeHandle module)
    {
        return new HeapContext{Module = Type.GetTypeFromHandle(module)};
    }
}