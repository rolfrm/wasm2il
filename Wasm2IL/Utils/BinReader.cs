using System.Runtime.InteropServices;
using Wasm;

namespace Wasm2IL;

using u64 = UInt64;
using u32 = UInt32;
using i64 = Int64;
using i32 = Int32;
using i16 = Int16;
using u8 = Byte;
using u16 = UInt16;

internal class BinReader
{
    static System.Text.Encoding Utf8 => System.Text.Encoding.UTF8;
    readonly MemoryStream membuffer = new();
    readonly byte[] data;
    readonly int length;
    int position;

    public BinReader(Stream stream)
    {
        length = (int)stream.Length;
        data = new byte[length];
        stream.ReadExactly(data);
    }

    public BinReader(byte[] data, int position)
    {
        this.data = data;
        this.position = position;
        length = data.Length;
    }

    public BinReader Clone() => new(data, position);

    public long Position
    {
        get => position;
        set => position = (int)value;
    }

    public bool IsAtEnd => position == length;

    public u8 ReadU8() => data[position++];

    public byte ReadByte() => ReadU8();

    public Instruction ReadInstruction() => (Instruction) ReadU8();
    public Instruction PeekInstruction() => (Instruction)data[position];

    public u32 ReadU32Leb() => (u32)ReadU64Leb();

    public u64 ReadU64Leb()
    {
        u8 chunk;
        u64 value = 0;
        u32 offset = 0;
        while ((chunk = ReadU8()) > 0)
        {
            value |= (u64)((0b01111111L & chunk) << (i32)offset);
            offset += 7;
            if ((0b10000000L & chunk) == 0)
                break;
        }
        return value;
    }

    public i64 ReadI64Leb()
    {
        unchecked
        {
            i64 value = 0;
            u32 shift = 0;
            u8 chunk;
            do
            {
                chunk = ReadU8();
                value |= ((i64)(chunk & 0x7f)) << (i32)shift;
                shift += 7;
            } while (chunk >= 128);
            if (shift < 64 && (chunk & 0x40) != 0)
                value |= (i64)0xFFFFFFFFFFFFFFFFL << (i32)shift;
            return value;
        }
    }

    public int Read(Span<byte> outData)
    {
        var subSpan = data.AsSpan(position, outData.Length);
        subSpan.CopyTo(outData);
        position += subSpan.Length;
        return subSpan.Length;
    }

    public long ReadI64() => ReadT<long>();
    public ulong ReadU64() => ReadT<ulong>();
    internal short ReadI16() => ReadT<i16>();
    internal ushort ReadU16() => ReadT<u16>();
    internal int ReadI32() => ReadT<i32>();
    internal uint ReadU32() => ReadT<u32>();
    internal float ReadF32() => ReadT<float>();
    public double ReadF64() => ReadT<double>();

    T ReadT<T>() where T : struct
    {
        T b = default;
        var elems = MemoryMarshal.CreateSpan(ref b, 1);
        Read(MemoryMarshal.AsBytes(elems));
        return b;
    }

    public string ReadStr0()
    {
        membuffer.Seek(0, SeekOrigin.Begin);
        while (true)
        {
            var b = ReadU8();
            if (b == 0) break;
            membuffer.WriteByte(b);
        }
        return Utf8.GetString(membuffer.GetBuffer(), 0, (int)membuffer.Position);
    }

    public string ReadStrN()
    {
        membuffer.Seek(0, SeekOrigin.Begin);
        var len = (int)ReadU64Leb();
        Span<byte> buffer = stackalloc byte[100];
        while (len > 0)
        {
            var bufferSlice = buffer[..Math.Min(len, 100)];
            Read(bufferSlice);
            len -= bufferSlice.Length;
            membuffer.Write(bufferSlice);
        }
        return Utf8.GetString(membuffer.GetBuffer(), 0, (int)membuffer.Position);
    }

    public string ReadStrl(int len)
    {
        membuffer.Seek(0, SeekOrigin.Begin);
        Span<byte> buffer = stackalloc byte[100];
        while (len > 0)
        {
            var bufferSlice = buffer[..Math.Min(len, 100)];
            Read(bufferSlice);
            len -= bufferSlice.Length;
            membuffer.Write(bufferSlice);
        }
        return Utf8.GetString(membuffer.GetBuffer(), 0, (int)membuffer.Position);
    }

    public byte[] ReadAllBytes()
    {
        var buf = new byte[length - position];
        Read(buf);
        return buf;
    }

    public byte[] ReadBytes(int n)
    {
        var buffer = new byte[n];
        Read(buffer);
        return buffer;
    }

    public long ReadSLeb64()
    {
        long result = 0;
        int shift = 0;
        byte b;

        while (true)
        {
            b = ReadByte();
            result |= (long)(b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                break;
        }

        if (shift < 64 && (b & 0x40) != 0)
            result |= -(1L << shift);

        return result;
    }
}