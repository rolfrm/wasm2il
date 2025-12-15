using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Wasm;

namespace Wasm2IL;

public static partial class Lib
{
    static Type InstrType(Instruction instruction, bool unsigned = false)
    {
        var s = instruction.ToString();
        if (s.Contains("F32")) return typeof(float);
        if (s.Contains("F64")) return typeof(double);
        if (s.Contains("I32")) return unsigned ? typeof(uint) : typeof(int);
        if (s.Contains("I64")) return unsigned ? typeof(ulong) : typeof(long);
        return typeof(void);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void MemoryFill(void* loc, int value, int N)
    {
        Unsafe.InitBlock(loc, (byte) value, (uint) N);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void MemoryCopy(void* dst, void* src, int N)
    {
        Unsafe.CopyBlockUnaligned(dst, src, (uint) N);
    }

    [WasmOpcode(ExtendedInstruction.I32_TRUNC_SAT_F32_S)]
    public static int I32_TRUNC_SAT_F32_S(float f)
    {
        if (float.IsNaN(f)) return 0;
        if (f >= int.MaxValue) return int.MaxValue;
        if (f <= int.MinValue) return int.MinValue;
        return (int)f;
    }

    [WasmOpcode(ExtendedInstruction.I32_TRUNC_SAT_F32_U)]
    public static uint I32_TRUNC_SAT_F32_U(float f)
    {
        if (float.IsNaN(f)) return 0;
        if (f >= uint.MaxValue) return uint.MaxValue;
        if (f <= uint.MinValue) return uint.MinValue;
        return (uint)f;
    }

    [WasmOpcode(ExtendedInstruction.I32_TRUNC_SAT_F64_S)]
    public static int I32_TRUNC_SAT_F64_S(double f)
    {
        if (double.IsNaN(f)) return 0;
        if (f >= int.MaxValue) return int.MaxValue;
        if (f <= int.MinValue) return int.MinValue;
        return (int)f;
    }

    [WasmOpcode(ExtendedInstruction.I32_TRUNC_SAT_F64_U)]
    public static uint I32_TRUNC_SAT_F64_U(double f)
    {
        if (double.IsNaN(f)) return 0;
        if (f >= uint.MaxValue) return uint.MaxValue;
        if (f <= uint.MinValue) return uint.MinValue;
        return (uint)f;
    }

    [WasmOpcode(ExtendedInstruction.I64_TRUNC_SAT_F32_S)]
    public static long I64_TRUNC_SAT_F32_S(float f)
    {
        if (float.IsNaN(f)) return 0;
        if (f >= long.MaxValue) return long.MaxValue;
        if (f <= long.MinValue) return long.MinValue;
        return (long)f;
    }

    [WasmOpcode(ExtendedInstruction.I64_TRUNC_SAT_F32_U)]
    public static ulong I64_TRUNC_SAT_F32_U(float f)
    {
        if (float.IsNaN(f)) return 0;
        if (f >= ulong.MaxValue) return ulong.MaxValue;
        if (f <= ulong.MinValue) return ulong.MinValue;
        return (ulong)f;
    }

    [WasmOpcode(ExtendedInstruction.I64_TRUNC_SAT_F64_S)]
    public static long I64_TRUNC_SAT_F64_S(double f)
    {
        if (double.IsNaN(f)) return 0;
        if (f >= long.MaxValue) return long.MaxValue;
        if (f <= long.MinValue) return long.MinValue;
        return (long)f;
    }

    [WasmOpcode(ExtendedInstruction.I64_TRUNC_SAT_F64_U)]
    public static ulong I64_TRUNC_SAT_F64_U(double f)
    {
        if (double.IsNaN(f)) return 0;
        if (f >= ulong.MaxValue) return ulong.MaxValue;
        if (f <= ulong.MinValue) return ulong.MinValue;
        return (ulong)f;
    }

    [WasmOpcode(Instruction.I64_TRUNC_F32_S)]
    public static long I64_TRUNC_F32_S(float f)
    {
        if (f >= long.MaxValue || f <= long.MinValue || float.IsNaN(f))
            throw new OverflowException("i64.trunc_f32_s overflow");
        return (long)f;
    }

    [WasmOpcode(Instruction.I64_TRUNC_F32_U)]
    public static ulong I64_TRUNC_F32_U(float f)
    {
        if (f >= ulong.MaxValue || f < 0 || float.IsNaN(f))
            throw new OverflowException("i64.trunc_f32_u overflow");
        return (ulong)f;
    }
    [WasmOpcode(Instruction.I32_CTZ)]
    public static MethodInfo ctz()
    {
        return typeof(BitOperations).GetMethod(nameof(BitOperations.TrailingZeroCount),
            [typeof(uint)]);
    }

    [WasmOpcode(Instruction.I64_CTZ)]
    public static long ctz64(ulong arg)
    {
        return BitOperations.TrailingZeroCount(arg);
    }
    
    [WasmOpcode(Instruction.I32_CLZ)]
    public static MethodInfo clz()
    {
        return typeof(BitOperations).GetMethod(nameof(BitOperations.LeadingZeroCount),
            [typeof(uint)]);
    } 
    
    [WasmOpcode(Instruction.I64_CLZ)]
    public static long clz64(ulong arg)
    {
        return BitOperations.LeadingZeroCount(arg);
    }

    
    public static Expression getOthers(Instruction instr)
    {
        switch (instr)
        {
            case Instruction.I64_ROTL:
                return () => BitOperations.RotateLeft(0L, 0);
            case Instruction.I32_ROTL:
                return () => BitOperations.RotateLeft(0, 0);
            case Instruction.I64_ROTR:
                return () => BitOperations.RotateRight(0L, 0);
            case Instruction.I32_ROTR:
                return () => BitOperations.RotateRight(0, 0);
            case Instruction.I64_POPCNT:
                return () => BitOperations.PopCount(0L);
            case Instruction.I32_POPCNT:
                return () => BitOperations.PopCount(0);
            case Instruction.I64_REINTERPRET_F64:
                return () => BitConverter.DoubleToInt64Bits(0.0);
            case Instruction.I32_REINTERPRET_F32:
                return () => BitConverter.SingleToInt32Bits(0.0f);
            case Instruction.F32_REINTERPRET_I32:
                return () => BitConverter.Int32BitsToSingle(0);
            case Instruction.F64_REINTERPRET_I64:
                return () => BitConverter.Int64BitsToDouble(0L);
            case Instruction.F64_NEAREST:
                return () => Math.Round(0.0);
            case Instruction.F32_NEAREST:
                return () => MathF.Round(0.0f);
            case Instruction.F64_TRUNC:
                return () => Math.Truncate(0.0);
            case Instruction.F32_TRUNC:
                return () => MathF.Truncate(0.0f);
            case Instruction.F64_FLOOR:
                return () => Math.Floor(0.0);
            case Instruction.F32_FLOOR:
                return () => MathF.Floor(0.0f);
            case Instruction.F64_CEIL:
                return () => Math.Ceiling(0.0);
            case Instruction.F32_CEIL:
                return () => MathF.Ceiling(0.0f);
            case Instruction.F64_COPYSIGN:
                return () => Math.CopySign(0.0, 0.0);
            case Instruction.F32_COPYSIGN:
                return () => MathF.CopySign(0.0f, 0.0f);
            case Instruction.F64_SQRT:
                return () => Math.Sqrt(0.0);
            case Instruction.F32_SQRT:
                return () => MathF.Sqrt(0.0f);
            case Instruction.F64_MAX:
                return () => Math.Max(0.0, 0.0);
            case Instruction.F32_MAX:
                return () => MathF.Max(0.0f, 0.0f);
            case Instruction.F64_MIN:
                return () => Math.Min(0.0, 0.0);
            case Instruction.F32_MIN:
                return () => MathF.Min(0.0f, 0.0f);
            case Instruction.F64_ABS:
                return () => Math.Abs(0.0);
            case Instruction.F32_ABS:
                return () => MathF.Abs(0.0f);
        }

        return null;
    }

    public static MethodInfo GetOthers(Instruction instr)
    {
        var thing = getOthers(instr);
        if (thing is LambdaExpression le && le.Body is MethodCallExpression mc)
        {
            return mc.Method;
        }

        return null;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class WasmOpcodeAttribute : Attribute
{
    public object Key { get; }

    public WasmOpcodeAttribute(VectorInstructions vectorInstruction)
    {
        Key = vectorInstruction;
    }

    public WasmOpcodeAttribute(Instruction instruction)
    {
        Key = instruction;
    }

    public WasmOpcodeAttribute(ExtendedInstruction instruction)
    {
        Key = instruction;
    }
}