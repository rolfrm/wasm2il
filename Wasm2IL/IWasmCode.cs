namespace Wasm2Cil;

public interface IWasmCode
{
    public Stream GetCodeStream();
}

public static class WasmCode
{
    class WasmCodeBytes(byte[] bytes) : IWasmCode
    {
        public Stream GetCodeStream()
        {
            return new MemoryStream(bytes);
        }
    }
    
    public static IWasmCode FromBytes(byte[] bytes)
    {
        return new WasmCodeBytes(bytes);
    }
    
}