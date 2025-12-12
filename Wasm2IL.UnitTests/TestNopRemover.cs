using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm2IL.Optimization;

namespace Wasm2IL.UnitTests;

[TestFixture]
public class TestNopRemover
{
    private AssemblyDefinition _assembly;
    private TypeDefinition _type;
    private ModuleDefinition _module;

    public TestNopRemover()
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
    public void TestRemoveSimpleNop()
    {
        // Test: nop, ldarg.0, ret => ldarg.0, ret
        var method = CreateMethod("TestRemoveSimpleNop", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(3, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(OpCodes.Ret, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestRemoveMultipleNops()
    {
        // Test: nop, nop, ldarg.0, nop, ret => ldarg.0, ret
        var method = CreateMethod("TestRemoveMultipleNops", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(5, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(OpCodes.Ret, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestNopBranchTargetPreserved()
    {
        // Test: NOP that is a branch target is preserved
        // br nop_target
        // nop          <- branch target, should be kept
        // ldarg.0
        // ret
        var method = CreateMethod("TestNopBranchTarget", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        var nopInstr = il.Create(OpCodes.Nop);
        var ldargInstr = il.Create(OpCodes.Ldarg_0);

        il.Emit(OpCodes.Br, nopInstr);
        il.Append(nopInstr);
        il.Append(ldargInstr);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(4, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // NOP should be preserved because it's a branch target
        Assert.AreEqual(4, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Br, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(nopInstr, method.Body.Instructions[0].Operand);
        Assert.AreEqual(OpCodes.Nop, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestTrailingNopPreserved()
    {
        // Test: trailing nop (last instruction) is preserved
        var method = CreateMethod("TestTrailingNop", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);
        il.Emit(OpCodes.Nop);

        Assert.AreEqual(3, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // Trailing NOP is preserved
        Assert.AreEqual(3, method.Body.Instructions.Count);
    }

    [Test]
    public void TestNopBeforeRetRemoved()
    {
        // Test: nop before ret should be removed (not a branch target)
        // ldarg.0
        // nop
        // ret
        var method = CreateMethod("TestNopBeforeRet", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(3, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        Assert.AreEqual(2, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(OpCodes.Ret, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestConditionalBranchToNopPreserved()
    {
        // Test: NOP that is a conditional branch target is preserved
        var method = CreateMethod("TestConditionalBranchToNop", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        var nopInstr = il.Create(OpCodes.Nop);
        var ldc0 = il.Create(OpCodes.Ldc_I4_0);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Brfalse, nopInstr);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);
        il.Append(nopInstr);
        il.Append(ldc0);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(7, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // NOP is preserved because it's a branch target
        Assert.AreEqual(7, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Brfalse, method.Body.Instructions[1].OpCode);
        Assert.AreEqual(nopInstr, method.Body.Instructions[1].Operand);
    }

    [Test]
    public void TestNoNopsNoChange()
    {
        // Test: method with no nops is unchanged
        var method = CreateMethod("TestNoNops", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        int originalCount = method.Body.Instructions.Count;

        var pass = new NopRemover();
        bool changed = pass.Run(method.Body);

        Assert.IsFalse(changed);
        Assert.AreEqual(originalCount, method.Body.Instructions.Count);
    }

    [Test]
    public void TestMixedNopsAndBranchTargets()
    {
        // Test: some NOPs are branch targets, others are not
        // Only non-target NOPs should be removed
        var method = CreateMethod("TestMixedNops", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        var nopTarget = il.Create(OpCodes.Nop);  // This one is a branch target
        var nopFree = il.Create(OpCodes.Nop);    // This one is not

        il.Emit(OpCodes.Br, nopTarget);
        il.Append(nopFree);      // Not a target, should be removed
        il.Append(nopTarget);    // Branch target, should be kept
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(5, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // Only nopFree should be removed
        Assert.AreEqual(4, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Br, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(nopTarget, method.Body.Instructions[0].Operand);
        Assert.AreEqual(OpCodes.Nop, method.Body.Instructions[1].OpCode);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[2].OpCode);
    }
}
