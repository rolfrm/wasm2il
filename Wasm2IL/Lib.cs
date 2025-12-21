using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Wasm;

namespace Wasm2IL;

public static partial class Lib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(ExtendedInstruction.MEMORY_FILL)]
    public static unsafe void MemoryFill(void* loc, int value, int N, [WasmConst] byte memory)
    {
        Unsafe.InitBlock(loc, (byte) value, (uint) N);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(ExtendedInstruction.MEMORY_COPY)]
    public static unsafe void MemoryCopy(void* dst, void* src, int N, [WasmConst] byte memory1, [WasmConst] byte memory2)
    {
        Unsafe.CopyBlockUnaligned(dst, src, (uint) N);
    }

    public static unsafe void CheckMemory(void * loc, int size, void * memory)
    {
        var loc2 = new IntPtr(loc).ToInt64();
        var mem2 = new IntPtr(memory).ToInt64();
        if (loc2 > (mem2 + size) || loc2 < mem2)
            throw new Exception("Memory access out of range: {loc2 - mem2} out of {size}.");
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
        if (float.IsNaN(f)) return 0;
        if (f >= long.MaxValue) return long.MaxValue;
        if (f < long.MinValue) return long.MinValue;
        return (long)f;
    }

    [WasmOpcode(Instruction.I64_TRUNC_F32_U)]
    public static ulong I64_TRUNC_F32_U(float f)
    {
        if (float.IsNaN(f)) return 0;
        if (f >= ulong.MaxValue) return ulong.MaxValue;
        if (f < ulong.MinValue) return ulong.MinValue;
        return (ulong)f;
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
            case Instruction.I64_CLZ:
                return () => BitOperations.LeadingZeroCount(0L);
            case Instruction.I32_CLZ:
                return () => BitOperations.LeadingZeroCount(0);
            case Instruction.I64_CTZ:
                return () => BitOperations.TrailingZeroCount(0L);
            case Instruction.I32_CTZ:
                return () => BitOperations.TrailingZeroCount(0);
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

public class WasmConstAttribute : Attribute
{
    
}