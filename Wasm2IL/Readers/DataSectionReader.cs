using Mono.Cecil.Cil;
using Wasm;
using IlInstr = Mono.Cecil.Cil.OpCodes;
using instr = Wasm.Instruction;

namespace Wasm2IL.Readers
{
    /// <summary>
    /// Reads the DATA section of a WASM module.
    /// </summary>
    public class DataSectionReader : WasmSectionReaderBase
    {
        public DataSectionReader(TransformerContext context) : base(context)
        {
        }

        public override void Read(BinReader reader)
        {
            uint dataCount = reader.ReadU32Leb();
            for (int i = 0; i < dataCount; i++)
            {
                uint memidx = reader.ReadU32Leb();
                if (memidx != 0)
                    throw new Exception("Multiple memories are not supported");

                int offset = 0;
                while (true)
                {
                    var instruction = (instr)reader.ReadU8();
                    switch (instruction)
                    {
                        case instr.I32_CONST:
                            var _offset = (int)reader.ReadI64Leb();
                            offset = _offset;
                            break;
                        case instr.GLOBAL_GET:
                            throw new Exception("Check this!");
                        case instr.END:
                            goto read_end;
                        default:
                            throw new Exception("Unknown instruction");
                    }
                }

            read_end:
                uint byteCount = reader.ReadU32Leb();
                byte[] bc = new byte[byteCount];
                reader.Read(bc);

                var cctor = Context.MainClass.GetStaticConstructor();
                var il = cctor.Body.GetILProcessor();
                il.RemoveAt(cctor.Body.Instructions.Count - 1); // remove RET

                il.Emit(IlInstr.Ldsfld, Context.MemoryField);
                il.Emit(IlInstr.Ldc_I4, offset);
                il.Emit(IlInstr.Add);

                for (int i2 = 0; i2 < byteCount; i2++)
                {
                    if (byteCount - i2 >= 8)
                    {
                        var v = BitConverter.ToInt64(bc.AsSpan(i2, 8));
                        if (v != 0)
                        {
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Ldc_I8, v);
                            il.Emit(IlInstr.Stind_I8);
                        }

                        il.Emit(IlInstr.Ldc_I4_8);
                        il.Emit(IlInstr.Add);

                        i2 += 7;
                    }
                    else if (bc[i2] != 0)
                    {
                        il.Emit(IlInstr.Dup);
                        il.Emit(IlInstr.Ldc_I4, (int)bc[i2]);
                        il.Emit(IlInstr.Stind_I1);
                        il.Emit(IlInstr.Ldc_I4_1);
                        il.Emit(IlInstr.Add);
                    }
                }

                il.Emit(IlInstr.Pop);
                il.Emit(IlInstr.Ret);
            }
        }
    }
}
