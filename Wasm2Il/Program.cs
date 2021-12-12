namespace Wasm2Il
{
    public class Program
    {
        public static void Main()
        {


            UnitTests.TestReadWrite();
            var fstr = File.OpenRead("test.wasm");
            Transformer.Go(fstr, "Test");
            megaTest();
        }

        static int TestFib(int x){
            if(x == 0) return 0;
            if(x == 1) return 1;
            return TestFib(x - 1) + TestFib(x - 2);
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
        }
    }
}