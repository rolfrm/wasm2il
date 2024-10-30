using System.Reflection;

namespace Wasm2Cil;

[TestFixture]
public class TestBasicWasm
{
    [Test]
    public void LoadAndRunBasic()
    {
        var transformer = new Transformer();
        using var file = File.OpenRead("w1.wasm");
        transformer.Transform(file, "W1", "./w1.dll");
        
        var asm = Assembly.LoadFrom("./w1.dll");
        
        var type = asm.ExportedTypes.FirstOrDefault();
        var incf = type.GetMethod("incf");
        var multiply = type.GetMethod("multiply");
        var a = (int)incf.Invoke(null, []);
        var b= (int)incf.Invoke(null, []);
        var c= (int)incf.Invoke(null, []);
        var d = multiply.Invoke(null, [3, 5]);
        Assert.AreEqual(a, 75601);
        Assert.AreEqual(b, 75602);
        Assert.AreEqual(c, 75603);
        Assert.AreEqual(d, 15);
    }

    public class Import
    {
        public static void log(int logThings)
        {
            Console.WriteLine(logThings);
        }
    }
    
    [Test]
    public void LoadAndRunImport()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("console", typeof(Import));
        using var file = File.OpenRead("w1.wasm");
        transformer.Transform(file, "W1_2", "./w1_2.dll");
        
        var asm = Assembly.LoadFrom("./w1_2.dll");
        var testLog = asm.ExportedTypes.FirstOrDefault()?.GetMethod("testLog");
        testLog.Invoke(null, [5]);

    }
}