using System.Runtime.InteropServices;

namespace Wasm2IL;

class BinReader
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

    public byte ReadU8() => data[position++];

    public uint ReadU32Leb() => (uint)ReadU64Leb();

    public ulong ReadU64Leb()
    {
        byte chunk;
        ulong value = 0;
        int offset = 0;
        while ((chunk = ReadU8()) > 0)
        {
            value |= (ulong)(0x7F & chunk) << offset;
            offset += 7;
            if ((0x80 & chunk) == 0)
                break;
        }
        return value;
    }

    public long ReadI64Leb()
    {
        unchecked
        {
            long value = 0;
            int shift = 0;
            byte chunk;
            do
            {
                chunk = ReadU8();
                value |= (long)(chunk & 0x7F) << shift;
                shift += 7;
            } while (chunk >= 128);

            if (shift < 64 && (chunk & 0x40) != 0)
                value |= -1L << shift;
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
    public short ReadI16() => ReadT<short>();
    public ushort ReadU16() => ReadT<ushort>();
    public int ReadI32() => ReadT<int>();
    public uint ReadU32() => ReadT<uint>();
    public float ReadF32() => ReadT<float>();
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
        var len = (int)ReadU64Leb();
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
}
