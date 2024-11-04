using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Wasm2Cil;

public static class Lib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public static Vector128<byte> AddF32(Vector128<byte> a, Vector128<byte> b) => (a.AsSingle() + b.AsSingle()).AsByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> SubF32(Vector128<byte> a, Vector128<byte> b) => (a.AsSingle() - b.AsSingle()).AsByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MulF32(Vector128<byte> a, Vector128<byte> b) => (a.AsSingle() * b.AsSingle()).AsByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> DivF32(Vector128<byte> a, Vector128<byte> b) => (a.AsSingle() / b.AsSingle()).AsByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MinF32(Vector128<byte> a, Vector128<byte> b) => (Vector128.Min(a.AsSingle(), b.AsSingle()).AsByte());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MaxF32(Vector128<byte> a, Vector128<byte> b) => (Vector128.Max(a.AsSingle(), b.AsSingle()).AsByte());
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> CreateVector128(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7
        , byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15, byte b16) =>
        Vector128.Create(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16);
}