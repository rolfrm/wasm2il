using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Wasm;

namespace Wasm2IL;

public static partial class Lib
{
    // -------------------------------
    // I16x8 MIN / MAX
    // -------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_MIN_S)]
    public static Vector128<short> i16X8_min_s(Vector128<short> a, Vector128<short> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_MIN_U)]
    public static Vector128<ushort> i16X8_min_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_MAX_S)]
    public static Vector128<short> i16X8_max_s(Vector128<short> a, Vector128<short> b)
        => Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I16X8_MAX_U)]
    public static Vector128<ushort> i16X8_max_u(Vector128<ushort> a, Vector128<ushort> b)
        => Vector128.Max(a, b);

    // -------------------------------
    // I8x16 MIN / MAX
    // -------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_MIN_S)]
    public static Vector128<sbyte> i8X16_min_s(Vector128<sbyte> a, Vector128<sbyte> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_MIN_U)]
    public static Vector128<byte> i8X16_min_u(Vector128<byte> a, Vector128<byte> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_MAX_S)]
    public static Vector128<sbyte> i8X16_max_s(Vector128<sbyte> a, Vector128<sbyte> b)
        => Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I8X16_MAX_U)]
    public static Vector128<byte> i8X16_max_u(Vector128<byte> a, Vector128<byte> b)
        => Vector128.Max(a, b);

    // -------------------------------
    // I32x4 MIN / MAX
    // -------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_MIN_S)]
    public static Vector128<int> i32X4_min_s(Vector128<int> a, Vector128<int> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_MIN_U)]
    public static Vector128<uint> i32X4_min_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_MAX_S)]
    public static Vector128<int> i32X4_max_s(Vector128<int> a, Vector128<int> b)
        => Vector128.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(VectorInstructions.I32X4_MAX_U)]
    public static Vector128<uint> i32X4_max_u(Vector128<uint> a, Vector128<uint> b)
        => Vector128.Max(a, b);
}