using System.Runtime.InteropServices;
namespace Wasm
{
    using u64 = UInt64;
    using i64 = Int64;
    using u8 = Byte;

    class BinWriter
    {
        static System.Text.Encoding Utf8 => System.Text.Encoding.UTF8;
        readonly Stream str;
        public BinWriter(Stream stream) => str = stream;

        public void WriteLeb(u64 value)
        {
            while (true)
            {
                ulong toWrite = value & 0b01111111L;
                value >>= 7;
                if (value > 0)
                {
                    toWrite |= 0b10000000L;
                    Write((u8)toWrite);
                }
                else
                {
                    Write((u8)toWrite);
                    break;
                }
            }
        }
        
        public void Write(ReadOnlySpan<byte> bytes) => str.Write(bytes);

        public void Write(u8 b) => str.WriteByte(b);
        public void Write<T>(T b) where T: struct{
            var elems = MemoryMarshal.CreateReadOnlySpan(ref b, 1);
            var bytes = MemoryMarshal.AsBytes(elems);
            Write(bytes);
        }
        
        public void WriteStr0(string value)
        {
            var bytes = Utf8.GetBytes(value);
            Write(bytes.AsSpan());
            Write((u8)0);
        }

        public void WriteLeb(i64 value)
        {
            while (true)
            {
                u8 bits = (u8)(value & 0b01111111);
                u8 sign = (u8)(value & 0b01000000);
                i64 next = value >> 7;
                if ((next == 0 && sign == 0) || (sign > 0 && next == -1))
                {
                    Write(bits);
                    break;
                }
                
                Write((u8)(bits | 0b10000000));
                value = next;
                
            }

        }

        public void WriteStrN(string v)
        {
            var bytes = Utf8.GetBytes(v);
            WriteLeb((ulong)bytes.Length);
            Write(bytes.AsSpan());
        }
    }
}