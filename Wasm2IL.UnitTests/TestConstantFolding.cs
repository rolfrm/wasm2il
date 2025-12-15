using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm2IL.Optimization;

namespace Wasm2IL.UnitTests;

[TestFixture]
public class TestConstantFolding
{
    AssemblyDefinition _assembly;
    TypeDefinition _type;
    ModuleDefinition _module;

    public TestConstantFolding()
    {
        var assemblyName = new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0));
        _assembly = AssemblyDefinition.CreateAssembly(assemblyName, "TestModule", ModuleKind.Dll);
        _module = _assembly.MainModule;
        _type = new TypeDefinition("Test", "TestClass",
            TypeAttributes.Class | TypeAttributes.Public,
            _module.TypeSystem.Object);
        _module.Types.Add(_type);
    }

    MethodDefinition CreateMethod(string name, TypeReference returnType)
    {
        var method = new MethodDefinition(name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);
        _type.Methods.Add(method);
        return method;
    }

    [Test]
    public void TestI32AddFolding()
    {
        // Test: ldc 1, ldc 2, add => ldc 3
        var method = CreateMethod("TestI32Add", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(4, method.Body.Instructions.Count);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should be folded to: ldc 3, ret
        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_3, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(OpCodes.Ret, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestI32SubFolding()
    {
        // Test: ldc 10, ldc 3, sub => ldc 7
        var method = CreateMethod("TestI32Sub", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 10);
        il.Emit(OpCodes.Ldc_I4_3);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_7, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestI32MulFolding()
    {
        // Test: ldc 6, ldc 7, mul => ldc 42
        var method = CreateMethod("TestI32Mul", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_6);
        il.Emit(OpCodes.Ldc_I4_7);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)42, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestI32DivFolding()
    {
        // Test: ldc 100, ldc 10, div => ldc 10
        var method = CreateMethod("TestI32Div", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 100);
        il.Emit(OpCodes.Ldc_I4, 10);
        il.Emit(OpCodes.Div);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        // 10 should use Ldc_I4_S since it's in -128..127 range
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)10, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestI32DivByZeroNotFolded()
    {
        // Division by zero should not be folded (would cause runtime behavior change)
        var method = CreateMethod("TestI32DivByZero", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 100);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Div);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be folded
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestI32BitwiseAndFolding()
    {
        // Test: ldc 0xFF, ldc 0x0F, and => ldc 0x0F
        var method = CreateMethod("TestI32And", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 0xFF);
        il.Emit(OpCodes.Ldc_I4, 0x0F);
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)0x0F, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestI32ShiftLeftFolding()
    {
        // Test: ldc 1, ldc 4, shl => ldc 16
        var method = CreateMethod("TestI32Shl", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldc_I4_4);
        il.Emit(OpCodes.Shl);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)16, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestI64AddFolding()
    {
        // Test: ldc.i8 100, ldc.i8 200, add => ldc.i8 300
        var method = CreateMethod("TestI64Add", _module.TypeSystem.Int64);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I8, 100L);
        il.Emit(OpCodes.Ldc_I8, 200L);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I8, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(300L, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestF64AddFolding()
    {
        // Test: ldc.r8 1.5, ldc.r8 2.5, add => ldc.r8 4.0
        var method = CreateMethod("TestF64Add", _module.TypeSystem.Double);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_R8, 1.5);
        il.Emit(OpCodes.Ldc_R8, 2.5);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_R8, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(4.0, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestF32MulFolding()
    {
        // Test: ldc.r4 2.0f, ldc.r4 3.0f, mul => ldc.r4 6.0f
        var method = CreateMethod("TestF32Mul", _module.TypeSystem.Single);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_R4, 2.0f);
        il.Emit(OpCodes.Ldc_R4, 3.0f);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_R4, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(6.0f, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestChainedFolding()
    {
        // Test: ldc 1, ldc 2, add, ldc 3, mul => ldc 9
        // (1 + 2) * 3 = 9
        var method = CreateMethod("TestChained", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldc_I4_3);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(6, method.Body.Instructions.Count);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // First pass: ldc 1, ldc 2, add => ldc 3
        // Result: ldc 3, ldc 3, mul, ret
        // Second pass: ldc 3, ldc 3, mul => ldc 9
        // Result: ldc 9, ret
        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)9, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestConversionFolding()
    {
        // Test: ldc.i4 42, conv.i8 => ldc.i8 42
        var method = CreateMethod("TestConv", _module.TypeSystem.Int64);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(3, method.Body.Instructions.Count);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I8, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(42L, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestComparisonFolding()
    {
        // Test: ldc 5, ldc 10, clt => ldc 1 (true)
        var method = CreateMethod("TestClt", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4_5);
        il.Emit(OpCodes.Ldc_I4, 10);
        il.Emit(OpCodes.Clt);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_1, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestEqualityFolding()
    {
        // Test: ldc 42, ldc 42, ceq => ldc 1 (true)
        var method = CreateMethod("TestCeq", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_1, method.Body.Instructions[0].OpCode);
    }

    [Test]
    public void TestNonConstantNotFolded()
    {
        // Test that non-constant operations are not folded
        var method = CreateMethod("TestNonConstant", _module.TypeSystem.Int32);
        method.Parameters.Add(new ParameterDefinition("x", ParameterAttributes.None, _module.TypeSystem.Int32));
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // Should NOT be folded because first operand is not a constant
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestBranchTargetPreserved()
    {
        // Test that branch targets are updated when instructions are removed
        var method = CreateMethod("TestBranch", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        // Create: if (true) return 3; else return 0;
        // Which involves a branch to the first ldc instruction
        var ldcInstr = il.Create(OpCodes.Ldc_I4_1);
        var ldc2Instr = il.Create(OpCodes.Ldc_I4_2);

        il.Emit(OpCodes.Br, ldcInstr);  // Jump to ldc 1
        il.Append(ldcInstr);             // ldc 1
        il.Append(ldc2Instr);            // ldc 2
        il.Emit(OpCodes.Add);            // add
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        // The branch should now point to the folded ldc 3 instruction
        Assert.AreEqual(3, method.Body.Instructions.Count);
        var branchTarget = method.Body.Instructions[0].Operand as Instruction;
        Assert.IsNotNull(branchTarget);
        Assert.AreEqual(OpCodes.Ldc_I4_3, branchTarget.OpCode);
    }

    [Test]
    public void TestNegationFolding()
    {
        // Test: ldc 42, neg => ldc -42
        var method = CreateMethod("TestNeg", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Neg);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4_S, method.Body.Instructions[0].OpCode);
        Assert.AreEqual((sbyte)-42, method.Body.Instructions[0].Operand);
    }

    [Test]
    public void TestLargeConstantFolding()
    {
        // Test folding that results in a value requiring Ldc_I4
        var method = CreateMethod("TestLargeConst", _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldc_I4, 100000);
        il.Emit(OpCodes.Ldc_I4, 200000);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldc_I4, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(300000, method.Body.Instructions[0].Operand);
    }
}
