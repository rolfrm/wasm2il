using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Wasm2Il.UnitTests")]
namespace Wasm2Il
{
    public static class Assert
    {

        class AssertException : Exception
        {

        }

        public static void AreEqual<T>(T a, T b)
        {
            if (Equals(a, b) == false)
                throw new AssertException();
        }
        public static void AreEqual(float a, float b)
        {
            float d = 0.0001f;
            if (Math.Abs(a - b) > d)
                throw new AssertException();
        }
        
        public static void AreEqual(double a, double b)
        {
            double d = 0.00001;
            if (Math.Abs(a - b) > d)
                throw new AssertException();
        }

        public static void AreEqual(string a, string b)
        {
            if (Equals(a, b) == false)
                throw new AssertException();
        }

        public static void IsTrue(bool v)
        {
            if (v == false)
                throw new AssertException();
        }
    }
}