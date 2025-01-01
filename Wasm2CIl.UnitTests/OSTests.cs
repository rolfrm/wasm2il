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
        // basic
        if (true)
        {
            var catBytes = File.ReadAllBytes("cat.wasm");

            var process = os.StartProcess(WasmCode.FromBytes(catBytes), "cat", ["test.txt", "test.txt"]);
            process.WaitForExit();
        }

        var scBytes = File.ReadAllBytes("simple_call.wasm");
        
        var shprocess = os.StartProcess(WasmCode.FromBytes(scBytes), "simple_call", []);
        
        shprocess.WaitForExit();


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