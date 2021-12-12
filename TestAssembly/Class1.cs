using System.Runtime.InteropServices;
namespace TestAssembly
{

    class CodeModule
    {
        static byte[] Heap = new byte[1024];
        public static int Add(int x, int y)
        {
            return Heap[0] + Heap[1];
        }

        public static void Ref(ref byte x)
        {
            x += 1;
        }

        public static void Ref2()
        {
            var x = new Span<byte>(new byte[1]);
            Ref(ref x[0]);
        }

        public static void Test()
        {   
            Ref(ref Heap[0]);
        }
        
        static void test1()
        {
            var x = Heap.AsSpan(1);
            var y = MemoryMarshal.Cast<byte, int>(x);
            y[0] = 5;
        }

        private static int[] Globals = new int[1024];
        public static int AddNumbers(int param0, int param1)
        {
            int num = (int)(nint)Globals[0];
            int num2 = 16;
            int num3 = num - num2;
            int num4 = param0;
            int num5 = num3;
            System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref Heap[num4 + 12L]) = num5;
            num4 = param1;
            num5 = num3;
            System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref Heap[num4 + 8L]) = num5;
            num4 = num3;
            int num6 = System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref Heap[num4 + 12L]);
            num4 = num3;
            int num7 = System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref Heap[num4 + 8L]);
            return num6 + num7;
        }

        public static int TestBranch(int x)
        {
            if (x > 0) return 4;
            return 5;
        }
    }

}