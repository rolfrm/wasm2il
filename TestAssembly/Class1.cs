using System.Runtime.InteropServices;
namespace TestAssembly
{

    public class CodeModule
    {
        public static byte[] Heap = new byte[3024];
        public static object[] Functions = new object[1024];

        static CodeModule()
        {
            Functions[0] = new Func<int,int,int>(Add);
        }

        private static byte[] HeapTest = new byte[]
            {0, 0, 0, 0, 0, 0, 0, 0, 00, 0, 0, 0, 0, 0, 0, 0, 1, 2, 1, 3, 1, 0, 0, 0, 0, 0, 0};
        public static int Add(int x, int y)
        {
            return Heap[0] + Heap[1];
        }

        public static void TestType(Type t)
        {
            
        }

        public static void Ref(ref byte x)
        {
            TestType(typeof(CodeModule));
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
            if (x == 0) return 4;
            return 5;
        }
        public static int TestBranch2(int x)
        {
            return x == 2 ? 1 : 2;
        }
        public static int strlen(int param0)
        {
            int num = param0;
            int num2;
            if (((uint)param0 & 3u) != 0)
            {
                num = param0;
                num2 = param0;
                if (Heap[num2 + 0L] == 0)
                {
                    goto IL_01fd;
                }
                if (((uint)(num = param0 + 1) & 3u) != 0)
                {
                    num2 = num;
                    if (Heap[num2 + 0L] == 0)
                    {
                        goto IL_01fd;
                    }
                    if (((uint)(num = param0 + 2) & 3u) != 0)
                    {
                        num2 = num;
                        if (Heap[num2 + 0L] == 0)
                        {
                            goto IL_01fd;
                        }
                        if (((uint)(num = param0 + 3) & 3u) != 0)
                        {
                            num2 = num;
                            if (Heap[num2 + 0L] == 0)
                            {
                                goto IL_01fd;
                            }
                            num = param0 + 4;
                        }
                    }
                }
            }
            num += -4;
            int num3;
            do
            {
                num2 = (num += 4);
            }
            while ((((num3 = System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref Heap[num2 + 0L])) ^ -1) & (num3 + -16843009) & -2139062144) == 0);
            if (((uint)num3 & 0xFFu) != 0)
            {
                do
                {
                    num2 = ++num;
                }
                while (Heap[num2 + 0L] != 0);
            }
            goto IL_01fd;
            IL_01fd:
            return num - param0;
        }

    }

}