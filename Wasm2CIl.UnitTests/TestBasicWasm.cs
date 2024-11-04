using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;

namespace Wasm2Cil.UnitTests;

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
        var multiplyVec = type.GetMethod("multiply_vec");
        var tryVec = type.GetMethod("try_vec");
        var a = (int)incf.Invoke(null, []);
        var b= (int)incf.Invoke(null, []);
        var c= (int)incf.Invoke(null, []);
        var d = multiply.Invoke(null, [3, 5]);
        var e = (Vector128<byte>)multiplyVec.Invoke(null, [new Vector4(1, 2, 3, 4).AsVector128().AsByte(), new Vector4(5, 4, 3, 2).AsVector128().AsByte()]);
        var f = (Vector128<byte>) tryVec.Invoke(null, Array.Empty<object>());
        Assert.AreEqual(a, 75601);
        Assert.AreEqual(b, 75602);
        Assert.AreEqual(c, 75603);
        Assert.AreEqual(d, 15);
        Assert.AreEqual(e.AsSingle().AsVector4(), new Vector4(5, 8, 9, 8));
    }
    

    public class Import
    {
        public static void log(int logThings)
        {
            Console.WriteLine(logThings);
        }
    }
    
    [Test]
    public void LoadAndRunImportFromStream()
    {
        List<int> results = new List<int>();
        foreach (string wasmFile in new []{"w1.wasm", "w2.wasm"})
        {
            var transformer = new Transformer();
            transformer.LoadImportModule("console", typeof(Import));
            using var file = File.OpenRead(wasmFile);
            using var mem = new MemoryStream();
            transformer.Transform(file, "W1", mem);
            mem.Position = 0;

            var asm = Assembly.Load(mem.ToArray());
            var testLog = asm.ExportedTypes.FirstOrDefault()?.GetMethod("testLog");
            var r = (int)testLog.Invoke(null, [5]);
            results.Add(r);
        }

        Assert.IsTrue(results.SequenceEqual([5, 10]));
    }
}