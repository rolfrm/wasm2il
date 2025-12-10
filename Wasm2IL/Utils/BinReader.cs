using System.Runtime.InteropServices;

namespace Wasm2IL
{
    using u64 = UInt64;
    using u32 = UInt32;
    using i64 = Int64;
    using i32 = Int32;
    using i16 = Int16;
    using u8 = Byte;
    using u16 = UInt16;

    internal class BinReader
    {
        static System.Text.Encoding utf8 => System.Text.Encoding.UTF8;
        private int position = 0;
        private readonly int length;
        private readonly byte[] data;

        public BinReader Clone()
        {
            return new BinReader(data, position);

        }

        public long Position
        {
            get => position;
            set => position = (int)value;
        }

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

        public bool ReadToEnd() => position == length;

        public u8 ReadU8()
        {
            return data[position++];
        }

        public byte ReadByte() => ReadU8();
        
        public u32 ReadU32Leb() => (u32)ReadU64Leb();


        public u64 ReadU64Leb()
        {
            // read LEB128
            u8 chunk = 0;
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
            // read LEB128
            unchecked
            {
                i64 value = 0;
                u32 shift = 0;
                u8 chunk;
                do
                {
                    chunk = ReadU8();
                    value |= (((i64)(chunk & 0x7f)) << (i32)shift);
                    shift += 7;
                } while (chunk >= 128);
                if (shift < 64 && (chunk & 0x40) != 0)
                    value |= ((i64)0xFFFFFFFFFFFFFFFFL) << (i32)shift;
                return value;
            }
        }

        public int Read(Span<byte> outData){
            var subSpan = data.AsSpan(position, outData.Length);
            subSpan.CopyTo(outData);
            position += subSpan.Length;
            return subSpan.Length;
        }

        public long ReadI64()
        {
            Span<long> l = stackalloc long[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        public ulong ReadU64()
        {
            Span<ulong> l = stackalloc ulong[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        internal short ReadI16()
        {
            Span<i16> l = stackalloc i16[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        internal ushort ReadU16()
        {
            Span<u16> l = stackalloc u16[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        internal int ReadI32()
        {
            Span<i32> l = stackalloc i32[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }
        
        internal uint ReadU32()
        {
            Span<u32> l = stackalloc u32[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        internal float ReadF32()
        {
            Span<float> l = stackalloc float[1];
            Read(MemoryMarshal.AsBytes(l));
            return l[0];
        }

        public double ReadF64() => ReadT<double>();
        T ReadT<T>() where T: struct {
            T b = default;
            var elems = MemoryMarshal.CreateSpan(ref b, 1);
            var bytes = MemoryMarshal.AsBytes(elems);
            Read(bytes);
            return b;
        }

        public string ReadStr0()
        {
            membuffer.Seek(0,  SeekOrigin.Begin);

            while(true){
                var b = ReadU8();
                if(b == 0) break;
                membuffer.WriteByte(b);
            }
            
            return utf8.GetString(membuffer.GetBuffer(), 0, (int) membuffer.Position);
        }

        MemoryStream membuffer = new();



        public string ReadStrN()
        {
            membuffer.Seek(0,  SeekOrigin.Begin);
            var len = (int)ReadU64Leb();
            Span<byte> buffer = stackalloc byte[100];
            
            while(len > 0){
                var bufferSlice = buffer.Slice(0, Math.Min(len, 100));
                Read(bufferSlice);
                len = len - bufferSlice.Length;
                membuffer.Write(bufferSlice);
            }
            return utf8.GetString(membuffer.GetBuffer(), 0, (int)membuffer.Position);
            
        }

        public string ReadStrl(int len)
        {
            Span<byte> buffer = stackalloc byte[100]; 
            while(len > 0){
                var bufferSlice = buffer.Slice(0, Math.Min(len, 100));
                Read(bufferSlice);
                len = len - bufferSlice.Length;
                membuffer.Write(bufferSlice);
            }
            return utf8.GetString(membuffer.GetBuffer(), 0, (int)membuffer.Position);
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

        public object ReadSLeb64()
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

            // Sign extend if necessary
            if (shift < 64 && (b & 0x40) != 0)
            {
                result |= -(1L << shift);
            }

            return result;
        }
    }
}