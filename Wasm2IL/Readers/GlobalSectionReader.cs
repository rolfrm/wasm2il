using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm;
using IlInstr = Mono.Cecil.Cil.OpCodes;
using instr = Wasm.Instruction;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the GLOBAL section of a WASM module.
    /// </summary>
    public class GlobalSectionReader : WasmSectionReaderBase
    {
        public GlobalSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            uint globalCount = reader.ReadU32Leb();
            for (uint i = 0; i < globalCount; i++)
            {
                var valType = reader.ReadU8();
                var mut = reader.ReadU8();
                var instruction = (instr)reader.ReadU8();
                var glob = new Global();
                glob.Const = mut == 0;
                glob.Type = valType;

                switch (instruction)
                {
                    case instr.I32_CONST:
                        glob.Value = (int)reader.ReadI64Leb();
                        break;
                    case instr.I64_CONST:
                        glob.Value = reader.ReadI64Leb();
                        break;
                    case instr.F64_CONST:
                        glob.Value = reader.ReadF64();
                        break;
                    case instr.F32_CONST:
                        glob.Value = reader.ReadF32();
                        break;
                    default:
                        throw new Exception("Unsupported constant " + instruction);
                }

                var end = (instr)reader.ReadU8();
                Assert.AreEqual(instr.END, end);
                Context.Globals[i] = glob;
            }

            var ctor = Context.MainClass.GetStaticConstructor();
            var il = ctor.Body.GetILProcessor();
            ctor.Body.Instructions.RemoveAt(ctor.Body.Instructions.Count - 1);

            foreach (var global in Context.Globals)
            {
                var type = global.Value.Type;
                var fld = new FieldDefinition("global" + global.Key,
                    FieldAttributes.Static, Context.ByteToTypeReference(type));

                Context.MainClass.Fields.Add(fld);
                Context.Globals[global.Key].Field = fld;

                switch (global.Value.Value)
                {
                    case int i4:
                        il.Emit(IlInstr.Ldc_I4, i4);
                        break;
                    case long i8:
                        il.Emit(IlInstr.Ldc_I8, i8);
                        break;
                    case float r4:
                        il.Emit(IlInstr.Ldc_R4, r4);
                        break;
                    case double r8:
                        il.Emit(IlInstr.Ldc_R8, r8);
                        break;
                    default:
                        throw new Exception("Unsupported type");
                }

                il.Emit(IlInstr.Stsfld, fld);
            }

            il.Emit(IlInstr.Ret);
            Log.WriteLine("Globals: {0}", Context.Globals.Count);
        }
    }
}
