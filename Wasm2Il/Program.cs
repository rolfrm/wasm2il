using System.Numerics;

namespace Wasm2Il
{
    public class Program
    {
        public static void Main()
        {


            UnitTests.TestReadWrite();
            var fstr = File.OpenRead("test.wasm");
            Transformer.Go(fstr, "Test");
            
            var fstr2 = File.OpenRead("base.wasm");
            Transformer.Go(fstr2, "Test3");

            for (int i = -8; i < 8; i++)
            {
                var r = Test3.Code.testGe(i, 5);
                var r2 = Test3.Code.testLe(i, 5);
                var r3 = Test3.Code.testXor(i, 5);
                var r4 = Test3.Code.testGeu(i, 5);
                var r5 = Test3.Code.testNe(i, 5);
                var r6 = Test3.Code.testRotl(512, i);
                var r7 = Test3.Code.testRotr(512, i);
                var selectOn = (i % 2) == 0 ? 0 : 1;
                var r8 = Test3.Code.testSelect(1, 2, selectOn);
                Assert.AreEqual<bool>(i >= 5, r > 0);
                Assert.AreEqual<bool>(i <= 5, r2 > 0);
                Assert.AreEqual(i ^ 5, r3);
                Assert.AreEqual<bool>(((uint)i) >= 5, r4 > 0);
                Assert.AreEqual<bool>(i != 5, r5 > 0);
                var r6r = BitOperations.RotateLeft(512, i);
                var r7r = BitOperations.RotateRight(512, i);
                Assert.AreEqual((int)r6r, r6);
                Assert.AreEqual((int)r7r, r7);

            }
            // to do: add wat compiling as a pre-step.
            
            megaTest();
        }

        static int TestFib(int x){
            if(x == 0) return 0;
            if(x == 1) return 1;
            return TestFib(x - 1) + TestFib(x - 2);
        }
        

        static int Tformx(int x){
            switch(x){
                case 0: return 5;
                case 1: return 111;
                case 2: return 313;
                case 4: return -1000;
                case 5: return -1000000;
                default: return -1;
            }
        }
        static void megaTest()
        {
            int r = Test.Code.GetX(50);
            int r2 = Test.Code.AddNumbers(60,11);
            float r3 = Test.Code.GetX4(5f);
            float r4 = Test.Code.GetX3(3.1f);
            float r5 = Test.Code.GetX5(3.1f);
            float r6 = Test.Code.GetX6(3.1f);
            for (int i = 0; i < 10; i++)
            {
                int r7 = Test.Code.TestFib(i);
                var gt = TestFib(i);
                Assert.AreEqual(r7, gt);
            }

            float r8 = Test.Code.DivInt(1, 2);
            float r9 = Test.Code.DivInt(10, 2);
            foreach(int x in new int[]{10, 0,1,2,3,4,5})
            {
                var t = Tformx(x);
                var r10 = Test.Code.Tformx(x);
                Assert.AreEqual(t, r10);
            }

            {
                var a = Test.Code.selectTest(0);
                var b = Test.Code.selectTest(3);
                Assert.AreEqual(1, a);
                Assert.AreEqual(2, b);
            }

            {
                var ptr = Test.Code.selectTestPtr();
                
                var callPtr = Test.Code.callPtr(ptr, 5);
                var mulPtr = Test.Code.MulIntPtr();
                var callResult2 = Test.Code.callPtr(mulPtr, 5);
                var xpt = Test.Code.getXPointer();
                //var crc00 = Test2.Code.test3(Test2.Code.getString1());
                var crc0 = Test.Code.test3(Test.Code.getString1());
                var crc01 = Test.Code.test3(Test.Code.getString2());
                var crc02 = Test.Code.test3(Test.Code.getString1());
                var crc03 = Test.Code.test3(Test.Code.getString3());
                //var crc2 = Test.Code.test2();
                var crc = Test.Code.test1();
                
                //var mall2 = Test2.Code.malloc2(1024);

                //var mallo = Test4.Code.malloc(1024);
                //var mallo2 = Test4.Code.malloc(1024);
                for (int j = 0; j < 10; j++)
                {
                    List<int> ptrs = new List<int>();
                    for (int i = 0; i < 100; i++)
                        ptrs.Add(Test.Code.malloc(1024 * i));
                    Assert.IsTrue(ptrs.Distinct().Count() >= 99); // malloc(0) can return anything.
                    foreach (var ptr2 in ptrs)
                        Test.Code.free(ptr2);
                }
                
                
                var fmotr = Test.Code.fmod2(3.5f, 3.0f);
                var fmodt = 3.5 - 3.0;
                Assert.IsTrue(Math.Abs(fmotr - fmodt) < 0.01);
                var fmodr2 = Test.Code.fmod2(1.55f, 1.5f);
                
                var abs = Test.Code.fabs2(-1.0f);
                var abs2 = Test.Code.fabsd(-1.0f);
                Assert.AreEqual(abs2, Math.Abs(-1.0));
                Assert.AreEqual(abs, Math.Abs(-1.0f));
                int combined = 0;
                for (int i = 0; i < 9; i++)
                {
                    combined += i;
                    var tr = Test.Code.testAddingNumbers(i);
                    Assert.AreEqual(combined, tr);
                }
                var rarg = Test.Code.testAddingNumbers(4);
                var mall2 = Test.Code.malloc(32);
                var mall3 = Test.Code.malloc(32);
                var mall4 = Test.Code.malloc(1024);
                Test.Code.free(mall3);
                Test.Code.free(mall4);

                Test.Code.SetValue(xpt, 10);
                Test.Code.SetValue(xpt+4, 20);
                Test.Code.SetValue(xpt + 8, 2000000000);

                //Test.Code.writeData(xpt, 10);
                var a = Test.Code.GetValue(xpt + 8);
                var b = Test.Code.GetValue(xpt + 9);
                Test.Code.writeData(xpt, xpt + 12, 12);
                Test.Code.writeData(xpt, xpt + 24, 12);
                var c = Test.Code.GetValue(xpt + 8 + 12);
                //Test.Code.memset(xpt, 32, 30);

                var l2 = TestAssembly.CodeModule.strlen(1);
                Test.Code.SetValue(xpt, 0x10101000);
                Test.Code.SetValue(xpt + 4, 0x10101010);
                Test.Code.SetValue(xpt + 8, 0x10001010);

                //var l = Test.Code.strlen(xpt + 2);
                
                var c2 = Test.Code.GetValue(xpt + 1);
                var c2str = c2.ToString("X");

                var x = Test.Code.runTest();
                var lencrc = Test.Code.runTest2();
                //Test.Code.testWrite();
                Test.Code.helloWorld();

                Test.Code.openWriteRead();

            }

        }
    }
}