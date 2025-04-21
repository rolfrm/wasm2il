using Wasm2Cil;
using Wasm2Cil.OperatingSystem;

namespace Wasm2CIl.UnitTests;

[TestFixture]
public class OSTests
{
    [Test]
    public void TryRunOs()
    {
        var os = new OS();
        os.Load();
        // basic
        if (false)
        {
            var catBytes = File.ReadAllBytes("cat.wasm");

            var process = os.StartProcess(WasmCode.FromBytes(catBytes), "cat", ["test.txt", "test.txt"]);
            process.WaitForExit();
        }

        if (false)
        {
            var scBytes = File.ReadAllBytes("simple_call.wasm");

            var shprocess = os.StartProcess(WasmCode.FromBytes(scBytes), "simple_call", []);

            shprocess.WaitForExit();
        }

        if (false)
        {

            var scBytes = File.ReadAllBytes("simple_call2.wasm");

            var shprocess = os.StartProcess(WasmCode.FromBytes(scBytes), "simple_call2", ["test1"]);

            var result = shprocess.WaitForExit();
        }

        if (true)
        {

            var scBytes = File.ReadAllBytes("term_text.wasm");

            var shprocess = os.StartProcess(WasmCode.FromBytes(scBytes), "term_text", [""]);

            var result = shprocess.WaitForExit();
        }
    } 
     
}


class ShApp
{
    static string readline()
    {

        return "";
        
    }
    
    public static void Main(int argc, string[] args)
    {
        while (true)
        {
            var line = readline();
            line.Split("#").First();
        }
    }
}