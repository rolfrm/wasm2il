using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using Wasm;

namespace Wasm2IL;

public static partial class Lib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* Alloc(int size) =>
        throw new NotImplementedException("Alloc not available - WASM module should provide malloc");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* Realloc(byte* ptr, int size) =>
        throw new NotImplementedException("Realloc not available - WASM module should provide realloc");

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_EQ)]
    public static Vector128<byte> i8x16_eq(Vector128<byte> a, Vector128<byte> b)
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_NE)]
    public static Vector128<byte> i8x16_ne(Vector128<byte> a, Vector128<byte> b)
        => Vector128.OnesComplement(Vector128.Equals(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_LT_S)]
    public static Vector128<sbyte> i8x16_lt_s(Vector128<sbyte> a, Vector128<sbyte> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_LT_U)]
    public static Vector128<byte> i8x16_lt_u(Vector128<byte> a, Vector128<byte> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_EQ)]
    public static Vector128<short> i16x8_eq(Vector128<short> a, Vector128<short> b)
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_NE)]
    public static Vector128<short> i16x8_ne(Vector128<short> a, Vector128<short> b)
        => Vector128.OnesComplement(Vector128.Equals(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_LT_S)]
    public static Vector128<short> i16x8_lt_s(Vector128<short> a, Vector128<short> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_LT_U)]
    public static Vector128<ushort> i16x8_lt_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_GT_S)]
    public static Vector128<short> i16x8_gt_s(Vector128<short> a, Vector128<short> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_GT_U)]
    public static Vector128<ushort> i16x8_gt_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_LE_S)]
    public static Vector128<short> i16x8_le_s(Vector128<short> a, Vector128<short> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_LE_U)]
    public static Vector128<ushort> i16x8_le_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_GE_S)]
    public static Vector128<short> i16x8_ge_s(Vector128<short> a, Vector128<short> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_GE_U)]
    public static Vector128<ushort> i16x8_ge_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EQ)]
    public static Vector128<int> i32x4_eq(Vector128<int> a, Vector128<int> b)
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_NE)]
    public static Vector128<int> i32x4_ne(Vector128<int> a, Vector128<int> b)
        => Vector128.OnesComplement(Vector128.Equals(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_LT_S)]
    public static Vector128<int> i32x4_lt_s(Vector128<int> a, Vector128<int> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_LT_U)]
    public static Vector128<uint> i32x4_lt_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_GT_S)]
    public static Vector128<int> i32x4_gt_s(Vector128<int> a, Vector128<int> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_GT_U)]
    public static Vector128<uint> i32x4_gt_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_LE_S)]
    public static Vector128<int> i32x4_le_s(Vector128<int> a, Vector128<int> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_LE_U)]
    public static Vector128<uint> i32x4_le_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_GE_S)]
    public static Vector128<int> i32x4_ge_s(Vector128<int> a, Vector128<int> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_GE_U)]
    public static Vector128<uint> i32x4_ge_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_ADD)]
    public static Vector128<float> f32x4_add(Vector128<float> a, Vector128<float> b)
        => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_SUB)]
    public static Vector128<float> f32x4_sub(Vector128<float> a, Vector128<float> b)
        => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_MUL)]
    public static Vector128<float> f32x4_mul(Vector128<float> a, Vector128<float> b)
        => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_DIV)]
    public static Vector128<float> f32x4_div(Vector128<float> a, Vector128<float> b)
        => a / b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_MIN)]
    public static Vector128<float> f32x4_min(Vector128<float> a, Vector128<float> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_MAX)]
    public static Vector128<float> f32x4_max(Vector128<float> a, Vector128<float> b)
        => Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_create(long v1, long v2)
    {
        return Vector128.Create(v1, v2).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> v128_shuffle_unsafe(Vector128<byte> vector, Vector128<byte> indices)
    {
        return Ssse3.IsSupported ? Ssse3.Shuffle(vector, indices) :
            AdvSimd.Arm64.IsSupported ? AdvSimd.Arm64.VectorTableLookup(vector, indices) :
            PackedSimd.IsSupported ? PackedSimd.Swizzle(vector, indices) :
            Vector128.Shuffle(vector, indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_shuffle_vectors(Vector128<byte> vec1, Vector128<byte> vec2,
        Vector128<byte> control)
    {
        // control > 15 means select from vec2. < 15 means select from vec1.
        if (Sse41.IsSupported)
        {
            var fromVec2 = Vector128.GreaterThan(control, Vector128.Create((byte)15));
            control &= Vector128.Create((byte)15);
            vec1 = Ssse3.Shuffle(vec1, control);
            vec2 = Ssse3.Shuffle(vec2, control);
            return Sse41.BlendVariable(vec1, vec2, fromVec2);
        }

        if (AdvSimd.Arm64.IsSupported)
        {
            return AdvSimd.Arm64.VectorTableLookup((vec1, vec2), control);
        }

        // the slow but equal implementation
        {
            var result1 = new Vector128<byte>();
            for (var i = 0; i < 16; i++)
            {
                int idx = control.GetElement(i);
                if (idx > 15)
                {
                    idx -= 16;
                    result1 = result1.WithElement(i, vec2.GetElement(idx));
                }
                else
                {
                    result1 = result1.WithElement(i, vec1.GetElement(idx));
                }
            }

            return result1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte i8x16_extract_lane_s(Vector128<sbyte> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte i8x16_extract_lane_u(Vector128<byte> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_replace_lane(Vector128<byte> vector, byte value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short i16x8_extract_lane_s(Vector128<short> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort i16x8_extract_lane_u(Vector128<ushort> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_replace_lane(Vector128<short> vector, short value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int i32x4_extract_lane(Vector128<int> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_replace_lane(Vector128<int> vector, int value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long i64x2_extract_lane(Vector128<long> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_replace_lane(Vector128<long> vector, long value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float f32x4_extract_lane(Vector128<float> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_replace_lane(Vector128<float> vector, float value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double f64x2_extract_lane(Vector128<double> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<double> f64x2_replace_lane(Vector128<double> vector, double value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<int> v128_load32_zero(int* i1) => Vector128.CreateScalar(*i1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<long> vec128_load64_zero(long* i1) =>
        Vector128.CreateScalar(*i1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_NOT)]
    public static Vector128<byte> v128_not(Vector128<byte> a) => a ^ Vector128<byte>.AllBitsSet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_AND)]
    public static Vector128<byte> v128_and(Vector128<byte> a, Vector128<byte> b) => a & b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_ANDNOT)]
    public static Vector128<byte> v128_andnot(Vector128<byte> a, Vector128<byte> b) => Vector128.AndNot(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_OR)]
    public static Vector128<byte> v128_or(Vector128<byte> a, Vector128<byte> b) => a | b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_XOR)]
    public static Vector128<byte> v128_xor(Vector128<byte> a, Vector128<byte> b) => a ^ b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_BITSELECT)]
    public static Vector128<byte> v128_bitselect(Vector128<byte> a, Vector128<byte> b, Vector128<byte> mask)
        => Vector128.ConditionalSelect(mask, a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.V128_ANY_TRUE)]
    public static int v128_any_true(Vector128<byte> vector)
        => vector != Vector128<byte>.Zero ? 1 : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_ABS)]
    public static Vector128<sbyte> i8x16_abs(Vector128<sbyte> a)
        => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_NEG)]
    public static Vector128<sbyte> i8x16_neg(Vector128<sbyte> a)
        => Vector128.Negate(a);

    [WasmOpcode(VectorInstructions.I8X16_POPCNT)]
    public static Vector128<byte> i8x16_popcnt(Vector128<byte> a)
    {
        var count4Bit = Vector128.Create(0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, (byte)4);

        var count_lower = Vector128.Shuffle(count4Bit, a & Vector128.Create((byte)0xF)); // TODO: Use ShuffleNative on .NET 10
        var count_upper = Vector128.Shuffle(count4Bit, a >> 4);

        return count_lower + count_upper;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_ALL_TRUE)]
    public static bool i8x16_all_true(Vector128<byte> a)
    {
        var isAnyZero = Vector128.Equals(a, Vector128<byte>.Zero);
        return Vector128.EqualsAll(isAnyZero, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_BITMASK)]
    public static int i8x16_bitmask(Vector128<byte> a) => (int)Vector128.ExtractMostSignificantBits(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_NARROW_I16X8_S)]
    public static Vector128<sbyte> i8x16_narrow_i16x8_s(Vector128<short> a, Vector128<short> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_NARROW_I16X8_U)]
    public static Vector128<byte> i8x16_narrow_i16x8_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_NARROW_I32X4_U)]
    public static Vector128<ushort> i16x8_narrow_i32x4_U(Vector128<uint> a, Vector128<uint> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_NARROW_I32X4_S)]
    public static Vector128<short> i16x8_narrow_i32x4_s(Vector128<int> a, Vector128<int> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_SHL)]
    public static Vector128<byte> i8x16_shl(Vector128<byte> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_SHR_S)]
    public static Vector128<sbyte> i8x16_shr_s(Vector128<sbyte> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_SHR_U)]
    public static Vector128<byte> i8x16_shr_u(Vector128<byte> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_ADD)]
    public static Vector128<byte> i8x16_add(Vector128<byte> a, Vector128<byte> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_SUB)]
    public static Vector128<byte> i8x16_sub(Vector128<byte> a, Vector128<byte> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_MUL)]
    public static Vector128<byte> i8x16_mul(Vector128<byte> a, Vector128<byte> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_SHL)]
    public static Vector128<short> i16x8_shl(Vector128<short> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_SHR_U)]
    public static Vector128<ushort> i16x8_shr_u(Vector128<ushort> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_SHR_S)]
    public static Vector128<short> i16x8_shr_s(Vector128<short> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_ADD)]
    public static Vector128<short> i16x8_add(Vector128<short> a, Vector128<short> b) => a + b;

    // Extend lower 8 lanes of signed 8-bit integers to signed 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_EXTEND_LOW_I8x16_S)]
    public static Vector128<short> i16x8_extend_low_i8x16_s(Vector128<sbyte> a) => Vector128.WidenLower(a);

    // Extend upper 8 lanes of signed 8-bit integers to signed 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_EXTEND_HIGH_I8x16_S)]
    public static Vector128<short> i16x8_extend_high_i8x16_s(Vector128<sbyte> a) => Vector128.WidenUpper(a);

    // Extend lower 8 lanes of unsigned 8-bit integers to unsigned 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_EXTEND_LOW_I8x16_U)]
    public static Vector128<short> i16x8_extend_low_i8x16_u(Vector128<byte> a) => Vector128.WidenLower(a).AsInt16();

    // Extend upper 8 lanes of unsigned 8-bit integers to unsigned 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_EXTEND_HIGH_I8x16_U)]
    public static Vector128<short> i16x8_extend_high_i8x16_u(Vector128<byte> a) => Vector128.WidenUpper(a).AsInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<sbyte> i16x8_add_sat_s(Vector128<sbyte> a, Vector128<sbyte> b)
    {
        var (a0, a1) = Vector128.Widen(a);
        var (b0, b1) = Vector128.Widen(b);

        a0 += b0;
        a1 += b1;

        var min = Vector128.Create((short)sbyte.MinValue);
        var max = Vector128.Create((short)sbyte.MaxValue);

        return Vector128.Narrow(
            Vector128.Clamp(a0, min, max),
            Vector128.Clamp(a1, min, max)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTADD_PAIRWISE_I16X8_S)]
    public static Vector128<int> i32x4_extadd_pairwise_i16x8_s(Vector128<short> a)
    {
        a = Vector128.Shuffle(a, Vector128.Create(0,2,4,6, 1,3,5,7));
        var (a0, a1) = Vector128.Widen(a);
        return a0+a1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTADD_PAIRWISE_I16X8_U)]
    public static Vector128<int> i32x4_extadd_pairwise_i16x8_u(Vector128<ushort> a)
    {
        a = Vector128.Shuffle(a, Vector128.Create(0,2,4,6, 1,3,5,(ushort)7));
        var (a0, a1) = Vector128.Widen(a);
        return (a0+a1).AsInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_ABS)]
    public static Vector128<int> i32x4_abs(Vector128<int> a) => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_NEG)]
    public static Vector128<int> i32x4_neg(Vector128<int> a) => Vector128.Negate(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_ALL_TRUE)]
    public static bool i32x4_all_true(Vector128<int> a)
        => Vector128.EqualsAny(a, Vector128<int>.Zero) == false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_BITMASK)]
    public static int i32x4_bitmask(Vector128<int> a) => (int)Vector128.ExtractMostSignificantBits(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTEND_LOW_I16X8_S)]
    public static Vector128<int> i32x4_extend_low_i16x8_s(Vector128<short> a) => Vector128.WidenLower(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTEND_HIGH_I16X8_S)]
    public static Vector128<int> i32x4_extend_high_i16x8_s(Vector128<short> a) => Vector128.WidenUpper(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTEND_LOW_I16X8_U)]
    public static Vector128<int> i32x4_extend_low_i16x8_u(Vector128<ushort> a) => Vector128.WidenLower(a).AsInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_EXTEND_HIGH_I16X8_U)]
    public static Vector128<int> i32x4_extend_high_i16x8_u(Vector128<ushort> a)=> Vector128.WidenUpper(a).AsInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_SHL)]
    public static Vector128<int> i32x4_shl(Vector128<int> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_SHR_S)]
    public static Vector128<int> i32x4_shr_s(Vector128<int> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_SHR_U)]
    public static Vector128<uint> i32x4_shr_u(Vector128<uint> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_ADD)]
    public static Vector128<int> i32x4_add(Vector128<int> a, Vector128<int> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_SUB)]
    public static Vector128<int> i32x4_sub(Vector128<int> a, Vector128<int> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_MUL)]
    public static Vector128<int> i32x4_mul(Vector128<int> a, Vector128<int> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_ABS)]
    public static Vector128<long> i64x2_abs(Vector128<long> a) => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_NEG)]
    public static Vector128<long> i64x2_neg(Vector128<long> a) => -a.AsInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_ALL_TRUE)]
    public static bool i64x2_all_true(Vector128<long> a) => a.GetElement(0) != 0 && a.GetElement(1) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_BITMASK)]
    public static int i64x2_bitmask(Vector128<long> a) => (int)Vector128.ExtractMostSignificantBits(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTEND_LOW_I32X4_S)]
    public static Vector128<long> i64x2_extend_low_i32x4_s(Vector128<int> a) => Vector128.WidenLower(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTEND_HIGH_I32X4_S)]
    public static Vector128<long> i64x2_extend_high_i32x4_s(Vector128<int> a) => Vector128.WidenUpper(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTEND_LOW_I32X4_U)]
    public static Vector128<long> i64x2_extend_low_i32x4_u(Vector128<uint> a) => Vector128.WidenLower(a).AsInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTEND_HIGH_I32X4_U)]
    public static Vector128<long> i64x2_extend_high_i32x4_u(Vector128<uint> a) => Vector128.WidenUpper(a).AsInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_SHL)]
    public static Vector128<long> i64x2_shl(Vector128<long> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_SHR_S)]
    public static Vector128<long> i64x2_shr_s(Vector128<long> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_SHR_U)]
    public static Vector128<ulong> i64x2_shr_u(Vector128<ulong> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_ADD)]
    public static Vector128<long> i64x2_add(Vector128<long> a, Vector128<long> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_SUB)]
    public static Vector128<long> i64x2_sub(Vector128<long> a, Vector128<long> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_MUL)]
    public static Vector128<long> i64x2_mul(Vector128<long> a, Vector128<long> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EQ)]
    public static Vector128<long> i64x2_eq(Vector128<long> a, Vector128<long> b) => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_NE)]
    public static Vector128<long> i64x2_ne(Vector128<long> a, Vector128<long> b)
        => Vector128.OnesComplement(Vector128.Equals(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_LT_S)]
    public static Vector128<long> i64x2_lt_s(Vector128<long> a, Vector128<long> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_GT_S)]
    public static Vector128<long> i64x2_gt_s(Vector128<long> a, Vector128<long> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_LE_S)]
    public static Vector128<long> i64x2_le_s(Vector128<long> a, Vector128<long> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_GE_S)]
    public static Vector128<long> i64x2_ge_s(Vector128<long> a, Vector128<long> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTMUL_LOW_I32X4_S)]
    public static Vector128<long> i64x2_extmul_low_i32x4_s(Vector128<int> a, Vector128<int> b)
    {
        return Vector128.Create(
            a.GetElement(0) * b.GetElement(0),
            a.GetElement(1) * b.GetElement(1)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTMUL_HIGH_I32X4_S)]
    public static Vector128<long> i64x2_extmul_high_i32x4_s(Vector128<int> a, Vector128<int> b)
    {
        return Vector128.Create(
            a.GetElement(2) * b.GetElement(2),
            a.GetElement(3) * b.GetElement(3)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTMUL_LOW_I32X4_U)]
    public static Vector128<long> i64x2_extmul_low_i32x4_u(Vector128<uint> a, Vector128<uint> b)
    {
        return Vector128.Create(
            a.GetElement(0) * b.GetElement(0),
            a.GetElement(1) * b.GetElement(1)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_EXTMUL_HIGH_I32X4_U)]
    public static Vector128<long> i64x2_extmul_high_i32x4_u(Vector128<uint> a, Vector128<uint> b)
    {
        return Vector128.Create(
            a.GetElement(2) * b.GetElement(2),
            a.GetElement(3) * b.GetElement(3)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_SPLAT)]
    public static Vector128<byte> i8x16_splat(byte a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_SPLAT)]
    public static Vector128<short> i16x8_splat(short a) => Vector128.Create(a);

    [WasmOpcode(VectorInstructions.I32X4_SPLAT)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_splat(int a) => Vector128.Create(a);

    [WasmOpcode(VectorInstructions.I64X2_SPLAT)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_splat(long a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<byte> v128_load8_splat(byte* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<short> v128_load16_splat(short* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<int> v128_load32_splat(int* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<long> v128_load64_splat(long* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<byte> v128_load8_lane(byte* a, int lane, Vector128<byte> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<short> v128_load16_lane(short* a, int lane, Vector128<short> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<int> v128_load32_lane(int* a, int lane, Vector128<int> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<long> v128_load64_lane(long* a, int lane, Vector128<long> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void v128_store8_lane(byte* a, int lane, Vector128<byte> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void v128_store16_lane(short* a, int lane, Vector128<short> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void v128_store32_lane(int* a, int lane, Vector128<int> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void v128_store64_lane(long* a, int lane, Vector128<long> v) => *a = v.GetElement(lane);

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

    // Non-saturating truncation (throws on overflow)
    public static long I64_TRUNC_F32_S(float f)
    {
        if (f >= long.MaxValue || f <= long.MinValue || float.IsNaN(f))
            throw new OverflowException("i64.trunc_f32_s overflow");
        return (long)f;
    }

    public static ulong I64_TRUNC_F32_U(float f)
    {
        if (f >= ulong.MaxValue || f < 0 || float.IsNaN(f))
            throw new OverflowException("i64.trunc_f32_u overflow");
        return (ulong)f;
    }
}

public class WasmOpcodeAttribute(object key) : Attribute
{
    public object Key { get; } = key;
}