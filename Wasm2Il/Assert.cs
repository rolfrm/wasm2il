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