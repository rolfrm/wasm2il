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

}