using Wasm2Cil;
using Wasm2Cil.UnitTests;

namespace Wasm2CIl.UnitTests;

[TestFixture]
public class TestCrypto
{
    [Test]
    public void LoadAndRunCrypto()
    {
        var transformer = new Transformer();
        
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadOverrideModule(typeof(LibC.LibCOverride));
        var asm = transformer.LoadWasmAssembly("crypto.wasm", "CryptoWasm", "CryptoWasm.dll");
        asm.Invoke("set_stdout", 12345);
        int err = (int) asm.Invoke("main", 0, 0);

    }
    
}