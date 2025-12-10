using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Wasm2IL.CodeGen
{
    /// <summary>
    /// Helper methods for emitting IL instructions.
    /// </summary>
    public static class ILEmitterHelper
    {
        /// <summary>
        /// Emits an optimized Ldc_I4 instruction based on the constant value.
        /// </summary>
        public static void EmitLdcI4(this ILProcessor il, int value)
        {
            var opcode = value switch
            {
                -1 => OpCodes.Ldc_I4_M1,
                0 => OpCodes.Ldc_I4_0,
                1 => OpCodes.Ldc_I4_1,
                2 => OpCodes.Ldc_I4_2,
                3 => OpCodes.Ldc_I4_3,
                4 => OpCodes.Ldc_I4_4,
                5 => OpCodes.Ldc_I4_5,
                6 => OpCodes.Ldc_I4_6,
                7 => OpCodes.Ldc_I4_7,
                8 => OpCodes.Ldc_I4_8,
                _ => OpCodes.Ldc_I4
            };

            if (opcode != OpCodes.Ldc_I4)
            {
                il.Emit(opcode);
            }
            else
            {
                if (value is < 126 and > -126)
                    il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                else
                    il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        /// <summary>
        /// Loads memory field into the heap variable if not already loaded.
        /// </summary>
        public static void LoadMemory(this MethodGenerationContext ctx)
        {
            if (!ctx.HeapInited)
            {
                ctx.HeapInited = true;
                ctx.IL.InsertAfter(0, ctx.IL.Create(OpCodes.Ldsfld, ctx.TransformerContext.MemoryField));
                ctx.IL.InsertAfter(1, ctx.IL.Create(OpCodes.Stloc, ctx.HeapVar));
            }

            ctx.IL.Emit(OpCodes.Ldloc, ctx.HeapVar);
        }
    }
}
