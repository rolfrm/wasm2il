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
    public void TestNopBranchTargetRedirected()
    {
        // Test: branch targeting a nop gets redirected to next instruction
        // br nop_target
        // nop          <- branch target
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

        // NOP should be removed, branch should target ldarg.0
        Assert.AreEqual(3, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Br, method.Body.Instructions[0].OpCode);
        Assert.AreEqual(ldargInstr, method.Body.Instructions[0].Operand);
        Assert.AreEqual(OpCodes.Ldarg_0, method.Body.Instructions[1].OpCode);
    }

    [Test]
    public void TestConsecutiveNopBranchTargets()
    {
        // Test: multiple branches to consecutive nops
        // br nop1
        // br nop2
        // nop1         <- branch target 1
        // nop2         <- branch target 2
        // ldarg.0
        // ret
        var method = CreateMethod("TestConsecutiveNopTargets", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        var nop1 = il.Create(OpCodes.Nop);
        var nop2 = il.Create(OpCodes.Nop);
        var ldarg = il.Create(OpCodes.Ldarg_0);

        il.Emit(OpCodes.Br, nop1);
        il.Emit(OpCodes.Br, nop2);
        il.Append(nop1);
        il.Append(nop2);
        il.Append(ldarg);
        il.Emit(OpCodes.Ret);

        Assert.AreEqual(6, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // Both NOPs should be removed, both branches should target ldarg.0
        Assert.AreEqual(4, method.Body.Instructions.Count);
        Assert.AreEqual(ldarg, method.Body.Instructions[0].Operand);
        Assert.AreEqual(ldarg, method.Body.Instructions[1].Operand);
    }

    [Test]
    public void TestTrailingNopPreserved()
    {
        // Test: trailing nop with no instruction after is preserved
        // ldarg.0
        // ret
        // nop          <- no instruction after, should be preserved? Actually no - let's check
        var method = CreateMethod("TestTrailingNop", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);
        il.Emit(OpCodes.Nop);

        Assert.AreEqual(3, method.Body.Instructions.Count);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // Trailing NOP with nothing after is preserved (can't redirect branches to nothing)
        Assert.AreEqual(3, method.Body.Instructions.Count);
    }

    [Test]
    public void TestNopBeforeRetRemoved()
    {
        // Test: nop before ret should be removed
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
    public void TestConditionalBranchToNop()
    {
        // Test: conditional branch to nop gets redirected
        // ldarg.0
        // brfalse nop_target
        // ldc.i4.1
        // ret
        // nop          <- branch target
        // ldc.i4.0
        // ret
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

        // NOP removed, brfalse now targets ldc.i4.0
        Assert.AreEqual(6, method.Body.Instructions.Count);
        Assert.AreEqual(OpCodes.Brfalse, method.Body.Instructions[1].OpCode);
        Assert.AreEqual(ldc0, method.Body.Instructions[1].Operand);
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
    public void TestSwitchInstructionTargetingNop()
    {
        // Test: switch instruction with nop target gets redirected
        var method = CreateMethod("TestSwitchToNop", _module.TypeSystem.Int32, _module.TypeSystem.Int32);
        var il = method.Body.GetILProcessor();

        var nop1 = il.Create(OpCodes.Nop);
        var ldc1 = il.Create(OpCodes.Ldc_I4_1);
        var ldc2 = il.Create(OpCodes.Ldc_I4_2);
        var defaultTarget = il.Create(OpCodes.Ldc_I4_0);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Switch, new[] { nop1, ldc2 });
        il.Append(defaultTarget);
        il.Emit(OpCodes.Ret);
        il.Append(nop1);
        il.Append(ldc1);
        il.Emit(OpCodes.Ret);
        il.Append(ldc2);
        il.Emit(OpCodes.Ret);

        var pass = new NopRemover();
        pass.Run(method.Body);

        // NOP should be removed, switch target redirected to ldc1
        var switchInstr = method.Body.Instructions[1];
        Assert.AreEqual(OpCodes.Switch, switchInstr.OpCode);
        var targets = (Instruction[])switchInstr.Operand;
        Assert.AreEqual(ldc1, targets[0]);
        Assert.AreEqual(ldc2, targets[1]);
    }
}
