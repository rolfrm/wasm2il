using Wasm2IL;
using Wasm2IL.UnitTests;

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
        int b = asm.StringToHeap("Hello, OpenSSL!");
        int c = asm.StringToHeap("Hello, OpenSSL!!");
        int d = asm.StringToHeap("jajajaja");
        int err = (int) asm.Invoke("hashstr", b);
        err = (int) asm.Invoke("hashstr", c);
        err = (int) asm.Invoke("hashstr", d);
        err = (int) asm.Invoke("hashstr", b);

    }
    
}