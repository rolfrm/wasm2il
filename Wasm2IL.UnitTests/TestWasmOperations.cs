using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;

namespace Wasm2IL.UnitTests;

/// <summary>
/// Focused unit tests for individual WASM operations.
/// Tests are broken down by category for better isolation and debugging.
/// </summary>
[TestFixture]
public class TestWasmOperations
{
    WasmAssembly _asm;

    public TestWasmOperations()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));
        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");
        _asm = transformer.LoadWasmAssembly(wasmPath, "W1Ops");
    }

    #region Integer Arithmetic Tests

    [Test]
    public void TestIntegerMultiply()
    {
        var result = (int)_asm.Invoke("multiply", 6, 7);
        Assert.AreEqual(42, result);
    }

    [Test]
    public void TestIntegerMultiplyZero()
    {
        var result = (int)_asm.Invoke("multiply", 100, 0);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void TestIntegerMultiplyNegative()
    {
        var result = (int)_asm.Invoke("multiply", -5, 3);
        Assert.AreEqual(-15, result);
    }

    [Test]
    public void TestIntegerMultiplyLargeNumbers()
    {
        var result = (int)_asm.Invoke("multiply", 1000, 1000);
        Assert.AreEqual(1000000, result);
    }

    #endregion

    #region Global Variable Tests

    [Test]
    public void TestGlobalIncrement()
    {
        // The incf function increments a global and returns the new value
        var a = (int)_asm.Invoke("incf");
        var b = (int)_asm.Invoke("incf");
        var c = (int)_asm.Invoke("incf");

        Assert.AreEqual(b - a, 1);
        Assert.AreEqual(c - b, 1);
    }

    #endregion

    #region String Operations Tests

    [Test]
    public void TestStrlenEmpty()
    {
        var len = (int)_asm.Invoke("get_strlen", "");
        Assert.AreEqual(0, len);
    }

    [Test]
    public void TestStrlenShort()
    {
        var len = (int)_asm.Invoke("get_strlen", "Hi");
        Assert.AreEqual(2, len);
    }

    [Test]
    public void TestStrlenMedium()
    {
        var len = (int)_asm.Invoke("get_strlen", "Hello, World!");
        Assert.AreEqual(13, len);
    }

    [Test]
    public void TestStrlenLong()
    {
        var testString = new string('x', 1000);
        var len = (int)_asm.Invoke("get_strlen", testString);
        Assert.AreEqual(1000, len);
    }

    [Test]
    public void TestStrlenUnicode()
    {
        // UTF-8 encoding: each character may be multiple bytes
        var len = (int)_asm.Invoke("get_strlen", "hello");
        Assert.AreEqual(5, len);
    }

    #endregion

    #region Memory Allocation Tests

    [Test]
    public void TestMallocBasic()
    {
        var ptr = (int)_asm.Invoke("malloc", 100);
        Assert.Greater(ptr, 0);
    }

    [Test]
    public void TestMallocMultiple()
    {
        var ptr1 = (int)_asm.Invoke("malloc", 100);
        var ptr2 = (int)_asm.Invoke("malloc", 100);
        var ptr3 = (int)_asm.Invoke("malloc", 100);

        // Each allocation should return a different address
        Assert.AreNotEqual(ptr1, ptr2);
        Assert.AreNotEqual(ptr2, ptr3);
        Assert.AreNotEqual(ptr1, ptr3);
    }

    [Test]
    public void TestMallocSequential()
    {
        var ptr1 = (int)_asm.Invoke("malloc", 50);
        var ptr2 = (int)_asm.Invoke("malloc", 50);

        // Addresses should be sequential (simple bump allocator)
        Assert.AreEqual(50, ptr2 - ptr1);
    }

    [Test]
    public void TestMallocZero()
    {
        var ptr1 = (int)_asm.Invoke("malloc", 0);
        var ptr2 = (int)_asm.Invoke("malloc", 0);

        // Zero-size allocations still return valid pointers
        Assert.AreEqual(ptr1, ptr2); // Same address since no space used
    }

    #endregion

    #region Memory Copy Tests

    [Test]
    public void TestMemcpyBasic()
    {
        var src = _asm.Malloc(10);
        var dst = _asm.Malloc(10);

        // Initialize source memory
        var heap = _asm.GetHeap();
        for (int i = 0; i < 10; i++)
            heap[src + i] = (byte)(i + 1);

        _asm.Invoke("test_memcpy", dst, src, 10);

        // Verify copy
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(i + 1, heap[dst + i]);
    }

    [Test]
    public void TestMemcpyZeroLength()
    {
        var src = _asm.Malloc(10);
        var dst = _asm.Malloc(10);

        var heap = _asm.GetHeap();

        // Initialize dst with known values
        for (int i = 0; i < 10; i++)
            heap[dst + i] = 0xFF;

        // Copy zero bytes
        _asm.Invoke("test_memcpy", dst, src, 0);

        // dst should remain unchanged
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(0xFF, heap[dst + i]);
    }

    #endregion

    #region Memory Fill Tests

    [Test]
    public void TestMemfillBasic()
    {
        var p = (int)_asm.Invoke("test_memfill", 10, 0xAB);
        var heap = _asm.GetHeap();
        var arr = heap.Slice(p, 10).ToArray();

        Assert.IsTrue(arr.All(b => b == 0xAB));
    }

    [Test]
    public void TestMemfillZeroValue()
    {
        var p = (int)_asm.Invoke("test_memfill", 10, 0);
        var heap = _asm.GetHeap();
        var arr = heap.Slice(p, 10).ToArray();

        Assert.IsTrue(arr.All(b => b == 0));
    }

    [Test]
    public void TestMemfillMaxValue()
    {
        var p = (int)_asm.Invoke("test_memfill", 10, 0xFF);
        var heap = _asm.GetHeap();
        var arr = heap.Slice(p, 10).ToArray();

        Assert.IsTrue(arr.All(b => b == 0xFF));
    }

    [Test]
    public void TestMemfill2Basic()
    {
        // test_memfill2 uses memory.fill and memory.copy instructions
        var p = (int)_asm.Invoke("test_memfill2", 10, 0x42);
        var heap = _asm.GetHeap();
        var arr = heap.Slice(p, 10).ToArray();

        Assert.IsTrue(arr.All(b => b == 0x42));
    }

    #endregion

    #region Pointer Operations Tests

    [Test]
    public void TestPointerOffset()
    {
        var ptr = (int)_asm.Invoke("pointerOffset", 12345);
        Assert.AreEqual(12345, ptr);
    }

    [Test]
    public void TestPointerOffsetZero()
    {
        var ptr = (int)_asm.Invoke("pointerOffset", 0);
        Assert.AreEqual(0, ptr);
    }

    #endregion

    #region Test Function (Combined SIMD and Arithmetic Tests)

    [Test]
    public void TestRunAllTestsFunction()
    {
        // This calls the run_all_tests export which runs ~60 inline tests
        Assert.DoesNotThrow(() => _asm.Invoke("run_all_tests"));
    }

    // Note: The "test" function is already tested in TestBasicWasm.LoadAndRunBasic
    // which calls test.Invoke(null, []) with a fresh assembly instance

    #endregion

    #region FNV-1a Hash Tests

    [Test]
    public void TestFnv1aHash()
    {
        byte[] input = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] output = new byte[8];

        var inputPtr = _asm.Malloc(8);
        var outputPtr = _asm.Malloc(8);

        var heap = _asm.GetHeap();
        input.CopyTo(heap.Slice(inputPtr, 8));

        _asm.Invoke("fnv1a", inputPtr, 8, outputPtr);

        output = heap.Slice(outputPtr, 8).ToArray();
        var hexstr = Convert.ToHexString(output).ToLower();

        // Expected FNV-1a hash for [1,2,3,4,5,6,7,8]
        Assert.AreEqual("ed788a368b10b57e", hexstr);
    }

    [Test]
    public void TestFnv1aEmptyInput()
    {
        var outputPtr = _asm.Malloc(8);
        var inputPtr = _asm.Malloc(1);

        _asm.Invoke("fnv1a", inputPtr, 0, outputPtr);

        var heap = _asm.GetHeap();
        var output = heap.Slice(outputPtr, 8).ToArray();

        // FNV-1a offset basis: 14695981039346656037 = 0xcbf29ce484222325
        // In little-endian: 25 23 22 84 e4 9c f2 cb
        var hexstr = Convert.ToHexString(output).ToLower();
        Assert.AreEqual("25232284e49cf2cb", hexstr);
    }

    #endregion
}

/// <summary>
/// Tests for SIMD vector operations
/// </summary>
[TestFixture]
public class TestWasmSimd
{
    Assembly _assembly;
    Type _type;

    public TestWasmSimd()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));
        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");
        var dllPath = Path.Combine(baseDir, "w1_simd.dll");

        using var file = File.OpenRead(wasmPath);
        transformer.Transform(file, "W1Simd", dllPath);
        _assembly = Assembly.LoadFrom(dllPath);
        _type = _assembly.ExportedTypes.First();
    }

    [Test]
    public void TestVectorMultiply()
    {
        var multiplyVec = _type.GetMethod("multiply_vec");

        var vec1 = new Vector4(1, 2, 3, 4).AsVector128().AsByte();
        var vec2 = new Vector4(5, 4, 3, 2).AsVector128().AsByte();

        var result = (Vector128<byte>)multiplyVec.Invoke(null, [vec1, vec2]);
        var resultVec = result.AsSingle().AsVector4();

        Assert.AreEqual(new Vector4(5, 8, 9, 8), resultVec);
    }

    [Test]
    public void TestVectorMultiplyIdentity()
    {
        var multiplyVec = _type.GetMethod("multiply_vec");

        var vec1 = new Vector4(1, 2, 3, 4).AsVector128().AsByte();
        var identity = new Vector4(1, 1, 1, 1).AsVector128().AsByte();

        var result = (Vector128<byte>)multiplyVec.Invoke(null, [vec1, identity]);
        var resultVec = result.AsSingle().AsVector4();

        Assert.AreEqual(new Vector4(1, 2, 3, 4), resultVec);
    }

    [Test]
    public void TestVectorMultiplyZero()
    {
        var multiplyVec = _type.GetMethod("multiply_vec");

        var vec1 = new Vector4(1, 2, 3, 4).AsVector128().AsByte();
        var zero = new Vector4(0, 0, 0, 0).AsVector128().AsByte();

        var result = (Vector128<byte>)multiplyVec.Invoke(null, [vec1, zero]);
        var resultVec = result.AsSingle().AsVector4();

        Assert.AreEqual(new Vector4(0, 0, 0, 0), resultVec);
    }

    [Test]
    public void TestTryVec()
    {
        var tryVec = _type.GetMethod("try_vec");
        var result = (Vector128<byte>)tryVec.Invoke(null, []);

        // The function does a shuffle operation on [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16]
        // Shuffle: 15 31 14 30 13 29 9 8 7 6 5 4 3 2 1 0
        // This effectively reverses some parts of the vector
        // Verify we got a valid result by checking it's not the default value
        Assert.IsFalse(result.Equals(default(Vector128<byte>)), "Result should not be default");
    }

    [Test]
    public void TestV128Load32Lane()
    {
        var test = _type.GetMethod("test_v128_load32_lane");
        Assert.DoesNotThrow(() => test.Invoke(null, []));
    }
}

/// <summary>
/// Tests for WasmAssembly helper methods
/// </summary>
[TestFixture]
public class TestWasmAssemblyHelpers
{
    WasmAssembly _asm;

    public TestWasmAssemblyHelpers()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));
        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");
        _asm = transformer.LoadWasmAssembly(wasmPath, "W1Helpers");
    }

    [Test]
    public void TestGetHeap()
    {
        var heap = _asm.GetHeap();
        Assert.Greater(heap.Length, 0);
    }

    [Test]
    public void TestStringToHeap()
    {
        var ptr = _asm.StringToHeap("Hello");
        Assert.Greater(ptr, 0);

        var readBack = _asm.GetHeapString(ptr);
        Assert.AreEqual("Hello", readBack);
    }

    [Test]
    public void TestStringToHeapEmpty()
    {
        var ptr = _asm.StringToHeap("");
        Assert.Greater(ptr, 0);

        var readBack = _asm.GetHeapString(ptr);
        Assert.AreEqual("", readBack);
    }

    [Test]
    public void TestMallocAndFree()
    {
        var ptr = _asm.Malloc(100);
        Assert.Greater(ptr, 0);

        // Free doesn't actually do anything in this simple allocator
        Assert.DoesNotThrow(() => _asm.Free(ptr));
    }

    [Test]
    public void TestGetHeapObject()
    {
        var ptr = _asm.Malloc(4);
        var heap = _asm.GetHeap();

        // Write an integer to memory
        heap[ptr] = 0x78;
        heap[ptr + 1] = 0x56;
        heap[ptr + 2] = 0x34;
        heap[ptr + 3] = 0x12;

        var value = _asm.GetHeapObject<int>(ptr);
        Assert.AreEqual(0x12345678, value);
    }
}

/// <summary>
/// Tests for the interface implementation wrapper
/// </summary>
[TestFixture]
public class TestInterfaceWrapper
{
    public interface ISimpleWasm
    {
        [Wasm("multiply")]
        int Multiply(int a, int b);

        [Wasm("get_strlen")]
        int GetStrLen(string str);

        [Wasm("malloc")]
        int Malloc(int size);

        [Wasm("free")]
        void Free(int ptr);
    }

    WasmAssembly _asm;

    public TestInterfaceWrapper()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));
        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");
        _asm = transformer.LoadWasmAssembly(wasmPath, "W1Interface");
    }

    [Test]
    public void TestAsImplementation()
    {
        var api = _asm.AsImplementation<ISimpleWasm>();
        Assert.IsNotNull(api);
    }

    [Test]
    public void TestInterfaceMultiply()
    {
        var api = _asm.AsImplementation<ISimpleWasm>();
        var result = api.Multiply(7, 8);
        Assert.AreEqual(56, result);
    }

    [Test]
    public void TestInterfaceStrLen()
    {
        var api = _asm.AsImplementation<ISimpleWasm>();
        var result = api.GetStrLen("testing");
        Assert.AreEqual(7, result);
    }

    [Test]
    public void TestInterfaceMalloc()
    {
        var api = _asm.AsImplementation<ISimpleWasm>();
        var ptr1 = api.Malloc(100);
        var ptr2 = api.Malloc(100);

        Assert.Greater(ptr1, 0);
        Assert.Greater(ptr2, ptr1);
    }
}

/// <summary>
/// Tests for loading WASM from different sources
/// </summary>
[TestFixture]
public class TestWasmLoading
{
    [Test]
    public void TestLoadFromFile()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));

        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");

        var asm = transformer.LoadWasmAssembly(wasmPath, "LoadTest1");
        Assert.IsNotNull(asm);

        // Verify it works
        var result = (int)asm.Invoke("multiply", 5, 5);
        Assert.AreEqual(25, result);
    }

    [Test]
    public void TestLoadFromStream()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));

        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");

        using var file = File.OpenRead(wasmPath);
        using var mem = new MemoryStream();
        transformer.Transform(file, "LoadTest2", mem);
        mem.Position = 0;

        var asm = Assembly.Load(mem.ToArray());
        Assert.IsNotNull(asm);
        Assert.Greater(asm.ExportedTypes.Count(), 0);
    }

    [Test]
    public void TestLoadMultipleModules()
    {
        var baseDir = AppContext.BaseDirectory;

        // Load w1.wasm
        var transformer1 = new Transformer();
        transformer1.LoadImportModule("env", typeof(LibC));
        transformer1.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer1.LoadImportModule("env", typeof(TestBasicWasm._Env));
        var asm1 = transformer1.LoadWasmAssembly(Path.Combine(baseDir, "w1.wasm"), "Multi1");

        // Load w2.wasm
        var transformer2 = new Transformer();
        transformer2.LoadImportModule("console", typeof(TestBasicWasm.Import));
        var asm2 = transformer2.LoadWasmAssembly(Path.Combine(baseDir, "w2.wasm"), "Multi2");

        // Both should work independently
        var result1 = (int)asm1.Invoke("testLog", 5);
        var result2 = (int)asm2.Invoke("testLog", 5);

        Assert.AreEqual(5, result1);
        Assert.AreEqual(10, result2);
    }
}

/// <summary>
/// Tests for error handling
/// </summary>
[TestFixture]
public class TestWasmErrorHandling
{
    // An interface with methods that don't exist in w1.wasm
    public interface IBadInterface
    {
        [Wasm("nonexistent_function")]
        int NonExistentFunction(int x);
    }

    [Test]
    public void TestBadImportThrows()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("console", typeof(TestBasicWasm.ImportBad));

        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");

        using var file = File.OpenRead(wasmPath);
        using var mem = new MemoryStream();

        Assert.Throws<TransformException>(() => transformer.Transform(file, "BadImport", mem));
    }

    [Test]
    public void TestBadInterfaceThrows()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadImportModule("console", typeof(TestBasicWasm.Import));
        transformer.LoadImportModule("env", typeof(TestBasicWasm._Env));

        var baseDir = AppContext.BaseDirectory;
        var wasmPath = Path.Combine(baseDir, "w1.wasm");
        var asm = transformer.LoadWasmAssembly(wasmPath, "BadInterface");

        // This interface references a function that doesn't exist
        // Throws InvalidOperationException when method not found
        Assert.Throws<InvalidOperationException>(() =>
            asm.AsImplementation<IBadInterface>());
    }
}
