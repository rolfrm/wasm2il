using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace Wasm2IL;

public static class Lib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static byte* Alloc(int size)
    {
        var b = (byte*) Marshal.AllocHGlobal(size);
        Unsafe.InitBlockUnaligned(b, 0, (uint) size);
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static byte* Realloc(byte* ptr, int size)
    {
        return (byte*) Marshal.ReAllocHGlobal((IntPtr) ptr, size);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_eq(Vector128<byte> a, Vector128<byte> b)
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_ne(Vector128<byte> a, Vector128<byte> b)
        => Vector128.Equals(a, b) ^ Vector128<byte>.One;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_eq(Vector128<short> a, Vector128<short> b) 
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_ne(Vector128<short> a, Vector128<short> b)
        => Vector128.Equals(a, b ^ Vector128<short>.One);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_lt_s(Vector128<short> a, Vector128<short> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_lt_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_gt_s(Vector128<short> a, Vector128<short> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_gt_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_le_s(Vector128<short> a, Vector128<short> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_le_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_ge_s(Vector128<short> a, Vector128<short> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_ge_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_eq(Vector128<int> a, Vector128<int> b)
        => Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_ne(Vector128<int> a, Vector128<int> b)
        => Vector128.Equals(a, b) ^ Vector128<int>.One;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_lt_s(Vector128<int> a, Vector128<int> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_lt_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.LessThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_gt_s(Vector128<int> a, Vector128<int> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_gt_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.GreaterThan(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_le_s(Vector128<int> a, Vector128<int> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_le_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.LessThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_ge_s(Vector128<int> a, Vector128<int> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_ge_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.GreaterThanOrEqual(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_add(Vector128<float> a, Vector128<float> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_sub(Vector128<float> a, Vector128<float> b)
    {
        return a - b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_mul(Vector128<float> a, Vector128<float> b)
    {
        return a * b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_div(Vector128<float> a, Vector128<float> b)
    {
        return a / b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_min(Vector128<float> a, Vector128<float> b)
    {
        return Vector128.Min(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> f32x4_max(Vector128<float> a, Vector128<float> b)
    {
        return Vector128.Max(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_create(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7
        , byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15, byte b16)
    {
        return Vector128.Create(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16);
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
        if (Ssse3.IsSupported)
        {
            var ctrl = Vector128.GreaterThan(control, Vector128.Create((byte) 15)) + Vector128.Create((byte) 1);


            var nctrl = Vector128.LessThanOrEqual(control, Vector128.Create((byte) 15)) + Vector128.Create((byte) 1);

            var a = Ssse3.Shuffle(vec1, ctrl * control);
            var b = Ssse3.Shuffle(vec2, nctrl * (control - Vector128.Create((byte) 16)));
            var shuffledResult = a * ctrl + b * nctrl;

            return shuffledResult;
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
                    result1 = result1.WithElement(i, vec2.GetElement(idx));
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
    public static Vector128<float> f32x4_replace_lane(Vector128<float> vector, int value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double f64x2_extract_lane(Vector128<double> vector, int lane) => vector.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<double> f64x2_replace_lane(Vector128<double> vector, double value, int lane)
        => vector.WithElement(lane, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<int> v128_load32_zero(int* i1) => Vector128.CreateScalar(*i1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<long> vec128_load64_zero(long* i1) =>
        Vector128.CreateScalar(*i1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_not(Vector128<byte> a) =>  a ^ Vector128<byte>.AllBitsSet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_and(Vector128<byte> a, Vector128<byte> b) =>  a & b;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_andnot(Vector128<byte> a, Vector128<byte> b) => Vector128.AndNot(a, b);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_or(Vector128<byte> a, Vector128<byte> b) => a | b;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_xor(Vector128<byte> a, Vector128<byte> b) =>  a ^ b;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> v128_bitselect(Vector128<byte> a, Vector128<byte> b, Vector128<byte> mask)
    {
        // maybe
        if (AdvSimd.IsSupported)
        {
            // Perform (mask & a) | (~mask & b)
            var maskAndA = AdvSimd.And(mask, a); // mask & a
            var notMaskAndB = AdvSimd.BitwiseClear(b, mask); // ~mask & b
            return AdvSimd.Or(maskAndA, notMaskAndB); // Combine the results
        }
        else if (Sse2.IsSupported)
        {
            var maskAndA = Sse2.And(mask, a); // mask & a
            var notMask = Sse2.AndNot(mask, b); // ~mask & b
            return Sse2.Or(maskAndA, notMask);
        }
        else
        {
            throw new PlatformNotSupportedException("AdvSimd is not supported on this platform.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int v128_any_true(Vector128<byte> vector)
    {
        return vector != Vector128<byte>.Zero ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<sbyte> i8x16_abs(Vector128<sbyte> a)
        => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<sbyte> i8x16_neg(Vector128<sbyte> a)
        => Vector128.Negate(a);


    public static Vector128<byte> i8x16_popcnt(Vector128<byte> a)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool i8x16_all_true(Vector128<byte> a)
    {
        var isAnyZero = Vector128.Equals(a, Vector128<byte>.Zero);
        return Vector128.EqualsAll(isAnyZero, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int i8x16_bitmask(Vector128<byte> a)
    {
        int bitmask = 0;
        for (int i = 0; i < Vector128<byte>.Count; i++)
        {
            if (a.GetElement(i) != 0)
            {
                bitmask |= (1 << i);
            }
        }

        return bitmask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<sbyte> i8x16_narrow_i16x8_s(Vector128<short> a, Vector128<short> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_narrow_i16x8_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.Narrow(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_shl(Vector128<byte> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_shr_u(Vector128<byte> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_shr_s(Vector128<byte> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_add(Vector128<byte> a, Vector128<byte> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_sub(Vector128<byte> a, Vector128<byte> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_mul(Vector128<byte> a, Vector128<byte> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_shl(Vector128<short> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_shr_u(Vector128<ushort> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_shr_s(Vector128<short> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_add(Vector128<short> a, Vector128<short> b) => a + b;


    // Extend lower 8 lanes of signed 8-bit integers to signed 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_extend_low_i8x16_s(Vector128<sbyte> a)
        => Vector128.Create(
            (short) a.GetElement(0), (short) a.GetElement(1), (short) a.GetElement(2), (short) a.GetElement(3),
            (short) a.GetElement(4), (short) a.GetElement(5), (short) a.GetElement(6), (short) a.GetElement(7));

    // Extend upper 8 lanes of signed 8-bit integers to signed 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_extend_high_i8x16_s(Vector128<sbyte> a)
        => Vector128.Create(
            (short) a.GetElement(8), (short) a.GetElement(9), (short) a.GetElement(10), (short) a.GetElement(11),
            (short) a.GetElement(12), (short) a.GetElement(13), (short) a.GetElement(14), (short) a.GetElement(15));

    // Extend lower 8 lanes of unsigned 8-bit integers to unsigned 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_extend_low_i8x16_u(Vector128<byte> a)
        => Vector128.Create(
            (ushort) a.GetElement(0), (ushort) a.GetElement(1), (ushort) a.GetElement(2), (ushort) a.GetElement(3),
            (ushort) a.GetElement(4), (ushort) a.GetElement(5), (ushort) a.GetElement(6), (ushort) a.GetElement(7));

    // Extend upper 8 lanes of unsigned 8-bit integers to unsigned 16-bit integers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ushort> i16x8_extend_high_i8x16_u(Vector128<byte> a)
        => Vector128.Create(
            (ushort) a.GetElement(8), (ushort) a.GetElement(9), (ushort) a.GetElement(10), (ushort) a.GetElement(11),
            (ushort) a.GetElement(12), (ushort) a.GetElement(13), (ushort) a.GetElement(14), (ushort) a.GetElement(15));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i16x8_add_sat_s(Vector128<byte> a, Vector128<byte> b)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i32x4_extadd_pairwise_i16x8_s(Vector128<byte> a)
    {
        return Vector128.Create(
            (byte) ((short) a.GetElement(0) + (short) a.GetElement(1)),
            (byte) ((short) a.GetElement(2) + (short) a.GetElement(3)),
            (byte) ((short) a.GetElement(4) + (short) a.GetElement(5)),
            (byte) ((short) a.GetElement(6) + (short) a.GetElement(7)),
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 // Adjust as necessary for output type
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i32x4_extadd_pairwise_i16x8_u(Vector128<byte> a)
    {
        return Vector128.Create(
            (byte) ((ushort) a.GetElement(0) + (ushort) a.GetElement(1)),
            (byte) ((ushort) a.GetElement(2) + (ushort) a.GetElement(3)),
            (byte) ((ushort) a.GetElement(4) + (ushort) a.GetElement(5)),
            (byte) ((ushort) a.GetElement(6) + (ushort) a.GetElement(7)),
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 // Adjust as necessary for output type
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_abs(Vector128<int> a) => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_neg(Vector128<int> a) => Vector128.Negate(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool i32x4_all_true(Vector128<int> a)
    {
        return Vector128.EqualsAny(a, Vector128<int>.Zero) == false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int i32x4_bitmask(Vector128<int> a)
    {
        int bitmask = 0;
        for (int i = 0; i < 4; i++)
        {
            if (a.GetElement(i) < 0) // Check MSB (negative value)
            {
                bitmask |= (1 << i); // Set corresponding bit
            }
        }

        return bitmask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_extend_low_i16x8_s(Vector128<short> a)
    {
        return Vector128.Create(
            a.GetElement(0),
            a.GetElement(1),
            a.GetElement(2),
            a.GetElement(3)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_extend_high_i16x8_s(Vector128<short> a)
    {
        return Vector128.Create(
            a.GetElement(4),
            a.GetElement(5),
            a.GetElement(6),
            a.GetElement(7)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_extend_low_i16x8_u(Vector128<ushort> a)
    {
        return Vector128.Create(
            a.GetElement(0),
            a.GetElement(1),
            a.GetElement(2),
            a.GetElement(3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_extend_high_i16x8_u(Vector128<ushort> a)
    {
        return Vector128.Create(
            a.GetElement(4),
            a.GetElement(5),
            a.GetElement(6),
            a.GetElement(7));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_shl(Vector128<int> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_shr(Vector128<int> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_add(Vector128<int> a, Vector128<int> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_sub(Vector128<int> a, Vector128<int> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_mul(Vector128<int> a, Vector128<int> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_min_s(Vector128<int> a, Vector128<int> b) => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_min_u(Vector128<uint> a, Vector128<uint> b) =>
        Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_max_s(Vector128<int> a, Vector128<int> b) =>
        Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> i32x4_max_u(Vector128<uint> a, Vector128<uint> b) =>
        Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_abs(Vector128<long> a) => Vector128.Abs(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_neg(Vector128<byte> a)
    {
        return (-a.AsInt64()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_all_true(Vector128<byte> a)
    {
        return Vector128.Create((byte) (a.AsInt64() == (Vector128<long>.Zero) ? 1 : 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int i64x2_bitmask(Vector128<byte> _a)
    {
        var a = _a.AsInt64();
        int bitmask = 0;
        for (int i = 0; i < 8; i++)
        {
            if (a.GetElement(i) < 0) // Check MSB (negative value)
            {
                bitmask |= (1 << i); // Set corresponding bit
            }
        }

        return bitmask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extend_low_i32x4_s(Vector128<byte> a)
    {
        return Vector128.Create(
            (long) a.GetElement(0),
            (long) a.GetElement(1),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extend_high_i32x4_s(Vector128<byte> a)
    {
        return Vector128.Create(
            (long) a.GetElement(2),
            (long) a.GetElement(3),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extend_low_i32x4_u(Vector128<byte> a)
    {
        return Vector128.Create(
            (ulong) a.GetElement(0),
            (ulong) a.GetElement(1),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extend_high_i32x4_u(Vector128<byte> a)
    {
        return Vector128.Create(
            (ulong) a.GetElement(2),
            (ulong) a.GetElement(3),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_shl(Vector128<long> a, int b) => a << b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_shr_s(Vector128<long> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<ulong> i64x2_shr_u(Vector128<ulong> a, int b) => a >> b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_add(Vector128<long> a, Vector128<long> b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_sub(Vector128<long> a, Vector128<long> b) => a - b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_mul(Vector128<long> a, Vector128<long> b) => a * b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_eq(Vector128<long> a, Vector128<long> b) =>  Vector128.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_ne(Vector128<long> a, Vector128<long> b)
    {
        return Vector128.OnesComplement(Vector128.Equals(a, b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_lt_s(Vector128<long> a, Vector128<long> b)
    {
        return Vector128.LessThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_gt_s(Vector128<long> a, Vector128<long> b)
    {
        return Vector128.GreaterThan(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_le_s(Vector128<long> a, Vector128<long> b)
    {
        return Vector128.LessThanOrEqual(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_ge_s(Vector128<long> a, Vector128<long> b)
    {
        return Vector128.GreaterThanOrEqual(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extmul_low_i32x4_s(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Create(
            (long) ((int) a.GetElement(0) * (int) b.GetElement(0)),
            (long) ((int) a.GetElement(1) * (int) b.GetElement(1)),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extmul_high_i32x4_s(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Create(
            (long) ((int) a.GetElement(2) * (int) b.GetElement(2)),
            (long) ((int) a.GetElement(3) * (int) b.GetElement(3)),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extmul_low_i32x4_u(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Create(
            (ulong) ((uint) a.GetElement(0) * (uint) b.GetElement(0)),
            (ulong) ((uint) a.GetElement(1) * (uint) b.GetElement(1)),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i64x2_extmul_high_i32x4_u(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Create(
            (ulong) ((uint) a.GetElement(2) * (uint) b.GetElement(2)),
            (ulong) ((uint) a.GetElement(3) * (uint) b.GetElement(3)),
            0, 0 // Fill higher lanes as necessary
        ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> i8x16_splat(byte a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> i16x8_splat(short a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> i32x4_splat(int a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> i64x2_splat(long a) => Vector128.Create(a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<byte> v128_load8_splat(byte* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<short> v128_load16_splat(short* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<int> v128_load32_splat(int* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<long> v128_load64_splat(long* a) => Vector128.Create(*a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<byte> v128_load8_lane(byte* a, int lane, Vector128<byte> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<short> v128_load16_lane(short* a, int lane, Vector128<short> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<int> v128_load32_lane(int* a, int lane, Vector128<int> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<long> v128_load64_lane(long* a, int lane, Vector128<long> v) =>
        v.WithElement(lane, *a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void v128_store8_lane(byte* a, int lane, Vector128<byte> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void v128_store16_lane(short* a, int lane, Vector128<short> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void v128_store32_lane(int* a, int lane, Vector128<int> v) => *a = v.GetElement(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void v128_store64_lane(long* a, int lane, Vector128<long> v) => *a = v.GetElement(lane);

    public static Vector128<byte> not_implemented_vec128_vec128(Vector128<byte> a)
    {
        throw new NotImplementedException();
    }
}

public ref struct CString
{
    private readonly int offset;
    private readonly Span<byte> heap;

    public CString(Span<byte> heap, int offset)
    {
        this.heap = heap;
        this.offset = offset;
    }

    unsafe public CString(byte* heap, int offset)
    {
        this.heap = new Span<byte>(heap + offset, 1024 * 1024);
        this.offset = 0;
    }

    public static CString New(byte[] heap, int offset)
    {
        return new CString(heap, offset);
    }

    public static unsafe CString New2(byte* heap, int offset)
    {
        return new CString(heap, offset);
    }

    public string ToString()
    {
        var span = heap.Slice(offset, Length);
        return System.Text.Encoding.UTF8.GetString(span);
    }

    public int Length
    {
        get
        {
            int i = offset;
            for (;; i++)
            {
                var b = heap[i];
                if (b == 0)
                    return i - offset;
            }
        }
    }
}