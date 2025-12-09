using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm2IL.Optimization;

namespace Wasm2IL.UnitTests;

[TestFixture]
public class TestAlgebraicSimplifier
{
    private AssemblyDefinition _assembly;
    private TypeDefinition _type;
    private ModuleDefinition _module;

    public TestAlgebraicSimplifier()
    {
        var assemblyName = new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0));
        _assembly = AssemblyDefinition.CreateAssembly(assemblyName, "TestModule", ModuleKind.Dll);
        _module = _assembly.MainModule;
        _type = new TypeDefinition("Test", "TestClass",
            TypeAttributes.Class | TypeAttributes.Public,
            _module.TypeSystem.Object);
        _module.Types.Add(_type);
    }

    private MethodDefinition CreateMethod(string name, TypeReference returnType, params TypeReference[] paramTypes)
    {
        var method = new MethodDefinition(name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);
        foreach (var paramType in paramTypes)
        {
            method.Parameters.Add(new ParameterDefinition(paramType));
        }
        _type.Methods.Add(method);
        return method;
    }

    [Test]
    public void TestAddZeroRight_NotOptimized()
    {
        // a + 0 is NOT optimized because 'add' is used for pointer arithmetic
        // and we can't determine the type of 'a' without type tracking
        var method = CreateMethod("TestAddZeroRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be simplified due to pointer arithmetic concerns
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestAddZeroLeft_NotOptimized()
    {
        // 0 + a is NOT optimized because 'add' is used for pointer arithmetic
        var method = CreateMethod("TestAddZeroLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be simplified due to pointer arithmetic concerns
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestSubZero_NotOptimized()
    {
        // a - 0 is NOT optimized because 'sub' is used for pointer arithmetic
        var method = CreateMethod("TestSubZero", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be simplified due to pointer arithmetic concerns
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestMulOneRight()
    {
        // Test: a * 1 => a
        var method = CreateMethod("TestMulOneRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestMulOneLeft()
    {
        // Test: 1 * a => a
        var method = CreateMethod("TestMulOneLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestMulZeroRight()
    {
        // Test: a * 0 => 0 (when a is a simple load)
        var method = CreateMethod("TestMulZeroRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestMulZeroLeft()
    {
        // Test: 0 * a => 0 (when a is a simple load)
        var method = CreateMethod("TestMulZeroLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestDivOne()
    {
        // Test: a / 1 => a
        var method = CreateMethod("TestDivOne", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Div);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestOrZeroRight()
    {
        // Test: a | 0 => a
        var method = CreateMethod("TestOrZeroRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestOrZeroLeft()
    {
        // Test: 0 | a => a
        var method = CreateMethod("TestOrZeroLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestAndAllOnesRight()
    {
        // Test: a & -1 => a
        var method = CreateMethod("TestAndAllOnesRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestAndAllOnesLeft()
    {
        // Test: -1 & a => a
        var method = CreateMethod("TestAndAllOnesLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestXorZeroRight()
    {
        // Test: a ^ 0 => a
        var method = CreateMethod("TestXorZeroRight", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Xor);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestXorZeroLeft()
    {
        // Test: 0 ^ a => a
        var method = CreateMethod("TestXorZeroLeft", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Xor);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestShiftLeftZero()
    {
        // Test: a << 0 => a
        var method = CreateMethod("TestShiftLeftZero", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Shl);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestShiftRightZero()
    {
        // Test: a >> 0 => a
        var method = CreateMethod("TestShiftRightZero", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Shr);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestI64AddZero_NotOptimized()
    {
        // a + 0L is NOT optimized for i64 (same pointer arithmetic concerns as i32)
        var method = CreateMethod("TestI64AddZero", _module.TypeSystem.Int64, _module.TypeSystem.Int64);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I8, 0L);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be simplified due to pointer arithmetic concerns
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestI64MulOne()
    {
        // Test: a * 1L => a (i64)
        var method = CreateMethod("TestI64MulOne", _module.TypeSystem.Int64, _module.TypeSystem.Int64);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I8, 1L);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestF64AddZero()
    {
        // Test: a + 0.0 => a (f64)
        var method = CreateMethod("TestF64AddZero", _module.TypeSystem.Double, _module.TypeSystem.Double);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_R8, 0.0);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestF64MulOne()
    {
        // Test: a * 1.0 => a (f64)
        var method = CreateMethod("TestF64MulOne", _module.TypeSystem.Double, _module.TypeSystem.Double);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_R8, 1.0);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestF32AddZero()
    {
        // Test: a + 0.0f => a (f32)
        var method = CreateMethod("TestF32AddZero", _module.TypeSystem.Single, _module.TypeSystem.Single);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_R4, 0.0f);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestF32MulOne()
    {
        // Test: a * 1.0f => a (f32)
        var method = CreateMethod("TestF32MulOne", _module.TypeSystem.Single, _module.TypeSystem.Single);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_R4, 1.0f);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestChainedSimplification()
    {
        // Test: (a | 0) * 1 => a
        // Using | instead of + since + is not optimized due to pointer arithmetic
        var method = CreateMethod("TestChained", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(6, method.Body.Instructions.Count);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Both identity operations should be removed
        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestConstantFoldingThenSimplification()
    {
        // Test: a | (1 - 1) => a | 0 => a
        // This tests that constant folding and algebraic simplification work together
        var method = CreateMethod("TestCombined", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(6, method.Body.Instructions.Count);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // After constant folding: a, 0, or, ret
        // After algebraic simplification: a, ret
        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestNonSimplifiableNotChanged()
    {
        // Test: a + 5 should not be simplified
        var method = CreateMethod("TestNoChange", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_5);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }
}
