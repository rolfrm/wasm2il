using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;
using Wasm;
using Wasm2IL.Utils;
using instr = Wasm.Instruction;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the ELEMENT section of a WASM module.
    /// </summary>
    public class ElementSectionReader : WasmSectionReaderBase
    {
        private readonly IImportResolver _importResolver;
        private readonly Func<MethodReference, MethodReference> _methodWrapper;

        public ElementSectionReader(TransformerContext context,
            IImportResolver importResolver,
            Func<MethodReference, MethodReference> methodWrapper)
            : base(context)
        {
            _importResolver = importResolver;
            _methodWrapper = methodWrapper;
        }

        public override void Read(BinReader reader)
        {
            var cnt = reader.ReadU32Leb();
            for (int i = 0; i < cnt; i++)
            {
                var tableIndex = reader.ReadU32Leb();
                if (tableIndex != 0)
                    throw new Exception("Multiple tables are not supported");

                var instr2 = (instr)reader.ReadU8();
                if (instr2 == instr.VECTOR_INSTRUCTION)
                {
                    var ext2 = reader.ReadU8();
                    instr2 = (instr)(0xFD00 | ext2);
                }

                Assert.AreEqual(instr.I32_CONST, instr2);
                var offset = reader.ReadU32Leb();
                var end = (instr)reader.ReadU8();
                if (end != instr.END)
                    throw new Exception("Expected END opcode");

                var fncCnt = reader.ReadU32Leb();

                var ctor = Context.MainClass.GetStaticConstructor();
                ctor.Body.Instructions.RemoveAt(ctor.Body.Instructions.Count - 1);
                var il = ctor.Body.GetILProcessor();

                il.Emit(OpCodes.Ldc_I4, (int)fncCnt + 1);
                il.Emit(OpCodes.Newarr, Context.Assembly.MainModule.TypeSystem.Object);
                il.Emit(OpCodes.Stsfld, Context.FunctionTable);

                for (var i2 = 0; i2 < fncCnt; i2++)
                {
                    il.Emit(OpCodes.Ldsfld, Context.FunctionTable);
                    il.Emit(OpCodes.Ldc_I4, (int)i2 + 1);
                    il.Emit(OpCodes.Ldnull);

                    var funcId = reader.ReadU32Leb();
                    if (funcId < Context.ImportFuncs.Count)
                    {
                        var imp = Context.ImportFuncs[funcId];
                        var t = Context.Types[(uint)imp.TypeId];

                        if (imp.Method == null)
                        {
                            var method = _importResolver.ResolveImportedMethod(imp.Module, imp.Name);
                            if (method != null)
                            {
                                var reference = method is MethodReference mr
                                    ? mr
                                    : Context.Assembly.MainModule.ImportReference((MethodInfo)method);
                                reference = _methodWrapper(reference);
                                imp.Method = reference;
                            }
                        }

                        if (imp.Method == null)
                        {
                            throw new InvalidOperationException("!");
                        }

                        il.Emit(OpCodes.Ldftn, imp.Method);
                        var ftype = TypeMapper.TypeIdToFuncType(t, Context);
                        var constr = ftype.GetConstructors().First();
                        var cref = Context.Assembly.MainModule.ImportReference(constr);
                        il.Emit(OpCodes.Newobj, cref);
                        il.Emit(OpCodes.Stelem_Any, Context.Assembly.MainModule.TypeSystem.Object);
                    }
                    else
                    {
                        var importFunc = Context.FuncDecls[(uint)(funcId - Context.ImportFuncs.Count)];
                        var t = Context.Types[importFunc.TypeId];
                        il.Emit(OpCodes.Ldftn, Context.FuncDecls[(uint)(funcId - Context.ImportFuncs.Count)].Method);
                        var ftype = TypeMapper.TypeIdToFuncType(t, Context);
                        var constr = ftype.GetConstructors().First();
                        var cref = Context.Assembly.MainModule.ImportReference(constr);
                        il.Emit(OpCodes.Newobj, cref);
                        il.Emit(OpCodes.Stelem_Any, Context.Assembly.MainModule.TypeSystem.Object);
                    }
                }

                il.Emit(OpCodes.Ret);
            }
        }
    }
}
