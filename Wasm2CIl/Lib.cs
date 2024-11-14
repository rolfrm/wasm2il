using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace Wasm2Cil;

public static class Lib
{
    public unsafe static byte* Alloc(int size)
    {
        var b = (byte*) Marshal.AllocHGlobal(size);
        Unsafe.InitBlockUnaligned(b, 0, (uint)size);
        return b;
    } 
    
    public unsafe static byte* Realloc(byte* ptr, int size)
    {
        return (byte*) Marshal.ReAllocHGlobal((IntPtr)ptr, size);
    } 
    
    public static unsafe void MemoryFill(void* loc, int value, int N)
    {
        Unsafe.InitBlock(loc, (byte) value, (uint)N);

    }

    public static unsafe void MemoryCopy(void* dst, void* src, int N)
    {
        Buffer.MemoryCopy(src, dst, N, N);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> AddF32(Vector128<byte> a, Vector128<byte> b)
    {
        return (a.AsSingle() + b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> SubF32(Vector128<byte> a, Vector128<byte> b)
    {
        return (a.AsSingle() - b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MulF32(Vector128<byte> a, Vector128<byte> b)
    {
        return (a.AsSingle() * b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> DivF32(Vector128<byte> a, Vector128<byte> b)
    {
        return (a.AsSingle() / b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MinF32(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Min(a.AsSingle(), b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> MaxF32(Vector128<byte> a, Vector128<byte> b)
    {
        return Vector128.Max(a.AsSingle(), b.AsSingle()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> CreateVector128(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7
        , byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15, byte b16)
    {
        return Vector128.Create(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> ShuffleUnsafe(Vector128<byte> vector, Vector128<byte> indices)
    {
        return Ssse3.IsSupported ? Ssse3.Shuffle(vector, indices) :
            AdvSimd.Arm64.IsSupported ? AdvSimd.Arm64.VectorTableLookup(vector, indices) :
            PackedSimd.IsSupported ? PackedSimd.Swizzle(vector, indices) :
            Vector128.Shuffle(vector, indices);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> ShuffleVectors(Vector128<byte> vec1, Vector128<byte> vec2, Vector128<byte> control)
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
    
    unsafe public CString(byte * heap, int offset)
    {
        this.heap = new Span<byte>(heap + offset,1024 * 1024);
        this.offset = 0;
    }

    public static CString New(byte[] heap, int offset)
    {
        return new CString(heap, offset);
    }
    
    public static unsafe CString New2(byte * heap, int offset)
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