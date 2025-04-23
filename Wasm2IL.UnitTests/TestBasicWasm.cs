using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;

namespace Wasm2IL.UnitTests;

[TestFixture]
public class TestBasicWasm
{

    public class _Env
    {
        public static void assert(int x)
        {
            if (x == 0)
                throw new Exception("??");
        }
    }
    
    [Test]
    public void LoadAndRunBasic()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(_Env));
        using var file = File.OpenRead("w1.wasm");
        transformer.Transform(file, "W1", "./w1.dll");
        
        var asm = Assembly.LoadFrom("./w1.dll");
        
        var type = asm.ExportedTypes.FirstOrDefault();
        var incf = type.GetMethod("incf");
        var multiply = type.GetMethod("multiply");
        var multiplyVec = type.GetMethod("multiply_vec");
        var tryVec = type.GetMethod("try_vec");
        var test = type.GetMethod("test");
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
        test.Invoke(null, []);
    }
    

    public class Import
    {
        public static void log(int logThings)
        {
            Console.WriteLine(logThings);
        }
    }
    
    //[Test]
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

    public interface IW1Wasm
    {
        // copies the string as UTF8 and frees it afterwards.
        [Wasm("get_strlen")]
        int GetStrLen(string x);

        // returns a pointer to wasm memory.
        [Wasm]
        unsafe byte* malloc(int len);

        [Wasm]
        unsafe void free(byte* ptr);

        // pointers should point to wasm memory.
        [Wasm]
        unsafe int pointerOffset(byte* ptr);    

        // Spans and ReadOnlySpans are copied to wasm memory.
        // Spans are copied back after use.
        [Wasm("fnv1a")]
        void InOutTest(ReadOnlySpan<byte> inBuffer, int len, Span<byte> buffer1);
    }
    
    [Test]
    public unsafe void TestStrLen()
    {
        var tform = new Transformer();
        tform.LoadImportModule("env", typeof(LibC));
        var asm = tform.LoadWasmAssembly("w1.wasm", "W2");
        var len = asm.Invoke("get_strlen", "string");
        Assert.AreEqual(6, len);
        
        var w1wasm = asm.AsImplementation<IW1Wasm>();
        int len2 = w1wasm.GetStrLen("stringstring");
        Assert.AreEqual(12, len2);

        var p = w1wasm.malloc(10);
        var offset = w1wasm.pointerOffset(p);
        byte[] testData = [1, 2, 3, 4, 5, 6, 7, 9];
        byte[] outData = [1, 2, 3, 4, 5, 6, 7, 8];
        w1wasm.InOutTest(testData, testData.Length, outData );

    }
    
    [Test]
    public void TestMemCpy()
    {
        var tform = new Transformer();
        tform.LoadImportModule("env", typeof(LibC));
        var asm = tform.LoadWasmAssembly("w1.wasm", "W2");
        var len = asm.Invoke("get_strlen", "string");
        Assert.AreEqual(6, len);
        
        asm.Invoke("test_memcpy", asm.Malloc(6), asm.Malloc(6), 6);
    }
    [Test]
    public void TestMemFill()
    {
        var tform = new Transformer();
        tform.LoadImportModule("env", typeof(LibC));
        var asm = tform.LoadWasmAssembly("w1.wasm", "W2");
        
        var p = (int)asm.Invoke("test_memfill", 10, 16);
        var h = asm.GetHeap();
        var arr = h.Slice(p, 10).ToArray();
        Assert.IsTrue(arr.SequenceEqual(Enumerable.Repeat((byte)16, 10)));
    }
    
    [Test]
    public void TestMemFill2()
    {
        var tform = new Transformer();
        tform.LoadImportModule("env", typeof(LibC));
        var asm = tform.LoadWasmAssembly("w1.wasm", "W2");
        
        var p = (int)asm.Invoke("test_memfill2", 10, 16);
        var h = asm.GetHeap();
        var arr = h.Slice(p, 10).ToArray();
        Assert.IsTrue(arr.SequenceEqual(Enumerable.Repeat((byte)16, 10)));
    }
}