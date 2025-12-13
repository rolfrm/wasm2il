namespace Wasm2IL;

public interface IWasmCode
{
    Stream GetCodeStream();
}

public static class WasmCode
{
    public static IWasmCode FromBytes(byte[] bytes) => new WasmCodeBytes(bytes);

    class WasmCodeBytes(byte[] bytes) : IWasmCode
    {
        public Stream GetCodeStream() => new MemoryStream(bytes);
    }
}
