namespace Wasm2Il
{
    public class Program
    {
        public static void Main()
        {
            UnitTests.TestReadWrite();
            var fstr = File.OpenRead("test.wasm");
            Transformer.Go(fstr, "Test");
           
            /*
            int r = Test.Code.GetX(50);
            int r2 = Test.Code.AddNumbers(60,11);
            float r3 = Test.Code.GetX4(5f);
            float r4 = Test.Code.GetX3(3.1f);
            */
            
        }
    }
}