using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using Wasm;

namespace Wasm2IL;

// All the vector instructions are implemented here.
public partial class Lib
{
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
    [WasmOpcode(VectorInstructions.I8X16_REPLACE_LANE)]
    public static Vector128<byte> i8x16_replace_lane(Vector128<byte> vector, byte value, [WasmConst] byte lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short i16x8_extract_lane_s(Vector128<short> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort i16x8_extract_lane_u(Vector128<ushort> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_REPLACE_LANE)]
    public static Vector128<short> i16x8_replace_lane(Vector128<short> vector, short value, [WasmConst] byte lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int i32x4_extract_lane(Vector128<int> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_REPLACE_LANE)]
    public static Vector128<int> i32x4_replace_lane(Vector128<int> vector, int value, [WasmConst] byte lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long i64x2_extract_lane(Vector128<long> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_REPLACE_LANE)]
    public static Vector128<long> i64x2_replace_lane(Vector128<long> vector, long value, [WasmConst] byte lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float f32x4_extract_lane(Vector128<float> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_REPLACE_LANE)]
    public static Vector128<float> f32x4_replace_lane(Vector128<float> vector, float value, [WasmConst] byte lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double f64x2_extract_lane(Vector128<double> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F64X2_REPLACE_LANE)]
    public static Vector128<double> f64x2_replace_lane(Vector128<double> vector, double value, [WasmConst] byte lane)
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

    // ==================== Relaxed SIMD Instructions ====================

    /// <summary>
    /// Relaxed swizzle: selects lanes from 'a' using indices in 's'.
    /// For out-of-range indices (16-255), the result is implementation-defined (0 or the wrapped value).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_RELAXED_SWIZZLE)]
    public static Vector128<byte> i8x16_relaxed_swizzle(Vector128<byte> a, Vector128<byte> s)
        => v128_shuffle_unsafe(a, s);

    /// <summary>
    /// Relaxed truncation of f32x4 to i32x4 signed.
    /// NaN behavior is implementation-defined (0 or INT32_MAX).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_TRUNC_F32X4_S)]
    public static Vector128<int> i32x4_relaxed_trunc_f32x4_s(Vector128<float> a)
    {
        // Use hardware intrinsics when available for relaxed behavior
        if (Sse2.IsSupported)
            return Sse2.ConvertToVector128Int32WithTruncation(a);
        if (AdvSimd.IsSupported)
            return AdvSimd.ConvertToInt32RoundToZero(a);
        // Fallback: element-wise conversion
        return Vector128.Create(
            (int)a.GetElement(0),
            (int)a.GetElement(1),
            (int)a.GetElement(2),
            (int)a.GetElement(3)
        );
    }

    /// <summary>
    /// Relaxed truncation of f32x4 to i32x4 unsigned.
    /// NaN behavior is implementation-defined (0 or UINT32_MAX).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_TRUNC_F32X4_U)]
    public static Vector128<int> i32x4_relaxed_trunc_f32x4_u(Vector128<float> a)
    {
        // Use hardware intrinsics when available for relaxed behavior
        if (AdvSimd.IsSupported)
            return AdvSimd.ConvertToUInt32RoundToZero(a).AsInt32();
        // Fallback: element-wise conversion
        return Vector128.Create(
            (int)(uint)a.GetElement(0),
            (int)(uint)a.GetElement(1),
            (int)(uint)a.GetElement(2),
            (int)(uint)a.GetElement(3)
        );
    }

    /// <summary>
    /// Relaxed truncation of f64x2 to i32x4 signed with zero extension.
    /// The upper two lanes are set to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_TRUNC_F64X2_S_ZERO)]
    public static Vector128<int> i32x4_relaxed_trunc_f64x2_s_zero(Vector128<double> a)
    {
        if (Sse2.IsSupported)
        {
            var truncated = Sse2.ConvertToVector128Int32WithTruncation(a);
            return Sse2.UnpackLow(truncated, Vector128<int>.Zero);
        }
        return Vector128.Create(
            (int)a.GetElement(0),
            (int)a.GetElement(1),
            0,
            0
        );
    }

    /// <summary>
    /// Relaxed truncation of f64x2 to i32x4 unsigned with zero extension.
    /// The upper two lanes are set to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_TRUNC_F64X2_U_ZERO)]
    public static Vector128<int> i32x4_relaxed_trunc_f64x2_u_zero(Vector128<double> a)
    {
        return Vector128.Create(
            (int)(uint)a.GetElement(0),
            (int)(uint)a.GetElement(1),
            0,
            0
        );
    }

    /// <summary>
    /// Relaxed fused multiply-add for f32x4: a * b + c
    /// May be implemented as fused (single rounding) or unfused (two roundings).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_RELAXED_MADD)]
    public static Vector128<float> f32x4_relaxed_madd(Vector128<float> a, Vector128<float> b, Vector128<float> c)
    {
        if (Fma.IsSupported)
            return Fma.MultiplyAdd(a, b, c);
        if (AdvSimd.IsSupported)
            return AdvSimd.FusedMultiplyAdd(c, a, b);
        // Fallback: unfused multiply-add
        return a * b + c;
    }

    /// <summary>
    /// Relaxed negated fused multiply-add for f32x4: -a * b + c
    /// May be implemented as fused (single rounding) or unfused (two roundings).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_RELAXED_NMADD)]
    public static Vector128<float> f32x4_relaxed_nmadd(Vector128<float> a, Vector128<float> b, Vector128<float> c)
    {
        if (Fma.IsSupported)
            return Fma.MultiplyAddNegated(a, b, c);
        if (AdvSimd.IsSupported)
            return AdvSimd.FusedMultiplySubtract(c, a, b);
        // Fallback: unfused negated multiply-add
        return -a * b + c;
    }

    /// <summary>
    /// Relaxed fused multiply-add for f64x2: a * b + c
    /// May be implemented as fused (single rounding) or unfused (two roundings).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F64X2_RELAXED_MADD)]
    public static Vector128<double> f64x2_relaxed_madd(Vector128<double> a, Vector128<double> b, Vector128<double> c)
    {
        if (Fma.IsSupported)
            return Fma.MultiplyAdd(a, b, c);
        if (AdvSimd.Arm64.IsSupported)
            return AdvSimd.Arm64.FusedMultiplyAdd(c, a, b);
        // Fallback: unfused multiply-add
        return a * b + c;
    }

    /// <summary>
    /// Relaxed negated fused multiply-add for f64x2: -a * b + c
    /// May be implemented as fused (single rounding) or unfused (two roundings).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F64X2_RELAXED_NMADD)]
    public static Vector128<double> f64x2_relaxed_nmadd(Vector128<double> a, Vector128<double> b, Vector128<double> c)
    {
        if (Fma.IsSupported)
            return Fma.MultiplyAddNegated(a, b, c);
        if (AdvSimd.Arm64.IsSupported)
            return AdvSimd.Arm64.FusedMultiplySubtract(c, a, b);
        // Fallback: unfused negated multiply-add
        return -a * b + c;
    }

    /// <summary>
    /// Relaxed lane select for i8x16: bitwise select based on mask.
    /// Equivalent to: (a &amp; c) | (b &amp; ~c) or (a &amp; ~c) | (b &amp; c) depending on implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_RELAXED_LANESELECT)]
    public static Vector128<byte> i8x16_relaxed_laneselect(Vector128<byte> a, Vector128<byte> b, Vector128<byte> c)
        => Vector128.ConditionalSelect(c, a, b);

    /// <summary>
    /// Relaxed lane select for i16x8: bitwise select based on mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_RELAXED_LANESELECT)]
    public static Vector128<short> i16x8_relaxed_laneselect(Vector128<short> a, Vector128<short> b, Vector128<short> c)
        => Vector128.ConditionalSelect(c, a, b);

    /// <summary>
    /// Relaxed lane select for i32x4: bitwise select based on mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_LANESELECT)]
    public static Vector128<int> i32x4_relaxed_laneselect(Vector128<int> a, Vector128<int> b, Vector128<int> c)
        => Vector128.ConditionalSelect(c, a, b);

    /// <summary>
    /// Relaxed lane select for i64x2: bitwise select based on mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I64X2_RELAXED_LANESELECT)]
    public static Vector128<long> i64x2_relaxed_laneselect(Vector128<long> a, Vector128<long> b, Vector128<long> c)
        => Vector128.ConditionalSelect(c, a, b);

    /// <summary>
    /// Relaxed minimum for f32x4.
    /// NaN handling is implementation-defined (may return NaN or the other operand).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_RELAXED_MIN)]
    public static Vector128<float> f32x4_relaxed_min(Vector128<float> a, Vector128<float> b)
    {
        if (Sse.IsSupported)
            return Sse.Min(a, b);
        if (AdvSimd.IsSupported)
            return AdvSimd.Min(a, b);
        return Vector128.Min(a, b);
    }

    /// <summary>
    /// Relaxed maximum for f32x4.
    /// NaN handling is implementation-defined (may return NaN or the other operand).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F32X4_RELAXED_MAX)]
    public static Vector128<float> f32x4_relaxed_max(Vector128<float> a, Vector128<float> b)
    {
        if (Sse.IsSupported)
            return Sse.Max(a, b);
        if (AdvSimd.IsSupported)
            return AdvSimd.Max(a, b);
        return Vector128.Max(a, b);
    }

    /// <summary>
    /// Relaxed minimum for f64x2.
    /// NaN handling is implementation-defined (may return NaN or the other operand).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F64X2_RELAXED_MIN)]
    public static Vector128<double> f64x2_relaxed_min(Vector128<double> a, Vector128<double> b)
    {
        if (Sse2.IsSupported)
            return Sse2.Min(a, b);
        if (AdvSimd.Arm64.IsSupported)
            return AdvSimd.Arm64.Min(a, b);
        return Vector128.Min(a, b);
    }

    /// <summary>
    /// Relaxed maximum for f64x2.
    /// NaN handling is implementation-defined (may return NaN or the other operand).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.F64X2_RELAXED_MAX)]
    public static Vector128<double> f64x2_relaxed_max(Vector128<double> a, Vector128<double> b)
    {
        if (Sse2.IsSupported)
            return Sse2.Max(a, b);
        if (AdvSimd.Arm64.IsSupported)
            return AdvSimd.Arm64.Max(a, b);
        return Vector128.Max(a, b);
    }

    /// <summary>
    /// Relaxed Q15 fixed-point multiplication with rounding for i16x8.
    /// Computes (a[i] * b[i] + 0x4000) >> 15 for each lane.
    /// Overflow behavior for INT16_MIN * INT16_MIN is implementation-defined.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_RELAXED_Q15MULR_S)]
    public static Vector128<short> i16x8_relaxed_q15mulr_s(Vector128<short> a, Vector128<short> b)
    {
        if (Ssse3.IsSupported)
            return Ssse3.MultiplyHighRoundScale(a, b);
        // Fallback implementation
        return Vector128.Create(
            (short)((a.GetElement(0) * b.GetElement(0) + 0x4000) >> 15),
            (short)((a.GetElement(1) * b.GetElement(1) + 0x4000) >> 15),
            (short)((a.GetElement(2) * b.GetElement(2) + 0x4000) >> 15),
            (short)((a.GetElement(3) * b.GetElement(3) + 0x4000) >> 15),
            (short)((a.GetElement(4) * b.GetElement(4) + 0x4000) >> 15),
            (short)((a.GetElement(5) * b.GetElement(5) + 0x4000) >> 15),
            (short)((a.GetElement(6) * b.GetElement(6) + 0x4000) >> 15),
            (short)((a.GetElement(7) * b.GetElement(7) + 0x4000) >> 15)
        );
    }

    /// <summary>
    /// Relaxed dot product of i8x16 and i7x16 (signed * "unsigned treated as signed") to i16x8.
    /// Computes pairwise: a[2i] * b[2i] + a[2i+1] * b[2i+1] for each of 8 output lanes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_RELAXED_DOT_I8X16_I7X16_S)]
    public static Vector128<short> i16x8_relaxed_dot_i8x16_i7x16_s(Vector128<sbyte> a, Vector128<sbyte> b)
    {
        if (Ssse3.IsSupported)
            return Ssse3.MultiplyAddAdjacent(a.AsByte(), b);
        // Fallback: manual pairwise dot product
        return Vector128.Create(
            (short)(a.GetElement(0) * b.GetElement(0) + a.GetElement(1) * b.GetElement(1)),
            (short)(a.GetElement(2) * b.GetElement(2) + a.GetElement(3) * b.GetElement(3)),
            (short)(a.GetElement(4) * b.GetElement(4) + a.GetElement(5) * b.GetElement(5)),
            (short)(a.GetElement(6) * b.GetElement(6) + a.GetElement(7) * b.GetElement(7)),
            (short)(a.GetElement(8) * b.GetElement(8) + a.GetElement(9) * b.GetElement(9)),
            (short)(a.GetElement(10) * b.GetElement(10) + a.GetElement(11) * b.GetElement(11)),
            (short)(a.GetElement(12) * b.GetElement(12) + a.GetElement(13) * b.GetElement(13)),
            (short)(a.GetElement(14) * b.GetElement(14) + a.GetElement(15) * b.GetElement(15))
        );
    }

    /// <summary>
    /// Relaxed dot product with accumulation: i8x16 and i7x16 to i32x4 with addition.
    /// Computes quad-wise dot product and adds to accumulator c.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_RELAXED_DOT_I8X16_I7X16_ADD_S)]
    public static Vector128<int> i32x4_relaxed_dot_i8x16_i7x16_add_s(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<int> c)
    {
        // First compute the pairwise dot product to i16x8
        var dot16 = i16x8_relaxed_dot_i8x16_i7x16_s(a, b);

        if (Sse2.IsSupported)
        {
            // Use PMADDWD to sum pairs of i16 into i32
            var dot32 = Sse2.MultiplyAddAdjacent(dot16, Vector128.Create((short)1));
            return Sse2.Add(dot32, c);
        }

        // Fallback: manual pairwise sum
        var result = Vector128.Create(
            dot16.GetElement(0) + dot16.GetElement(1),
            dot16.GetElement(2) + dot16.GetElement(3),
            dot16.GetElement(4) + dot16.GetElement(5),
            dot16.GetElement(6) + dot16.GetElement(7)
        );
        return result + c;
    }
}