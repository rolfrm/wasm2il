namespace Wasm2IL.UnitTests
{
    internal static class Assert
    {

        class AssertException : Exception
        {
            public AssertException() : base($"Assertion failed")
            {

            }
            public AssertException(string s) : base($"Assertion failed: {s}")
            {

            }

        }


        public static void AreEqual<T>(T a, T b)
        {
            if (Equals(a, b) == false)
                throw new AssertException($"Expected {a} == {b}");
        }

        public static void AreNotEqual<T>(T a, T b)
        {
            if (Equals(a, b))
                throw new AssertException($"Expected {a} != {b}");
        }

        public static void AreEqual(float a, float b)
        {
            float d = 0.0001f;
            if (Math.Abs(a - b) > d)
                throw new AssertException($"Expected {a} == {b} (within epsilon {d})");
        }

        public static void AreEqual(double a, double b)
        {
            double d = 0.00001;
            if (Math.Abs(a - b) > d)
                throw new AssertException($"Expected {a} == {b} (within epsilon {d})");
        }

        public static void AreEqual(string a, string b)
        {
            if (Equals(a, b) == false)
                throw new AssertException($"Expected \"{a}\" == \"{b}\"");
        }

        public static void IsTrue(bool v, string message = null)
        {
            if (v == false)
                throw new AssertException(message ?? "Expected true but was false");
        }

        public static void IsFalse(bool v, string message = null)
        {
            if (v)
                throw new AssertException(message ?? "Expected false but was true");
        }

        public static void IsNull<T>(T value) where T : class
        {
            if (value != null)
                throw new AssertException($"Expected null but got {value}");
        }

        public static void IsNotNull<T>(T value) where T : class
        {
            if (value == null)
                throw new AssertException("Expected non-null value but got null");
        }

        public static void Greater(int a, int b)
        {
            if (a <= b)
                throw new AssertException($"Expected {a} > {b}");
        }

        public static void GreaterOrEqual(int a, int b)
        {
            if (a < b)
                throw new AssertException($"Expected {a} >= {b}");
        }

        public static void Less(int a, int b)
        {
            if (a >= b)
                throw new AssertException($"Expected {a} < {b}");
        }

        public static void LessOrEqual(int a, int b)
        {
            if (a > b)
                throw new AssertException($"Expected {a} <= {b}");
        }

        public static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                throw new AssertException($"Expected exception of type {typeof(TException).Name} but no exception was thrown");
            }
            catch (TException)
            {
                // Expected
            }
        }

        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new AssertException($"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
            }
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            if (!expected.SequenceEqual(actual))
                throw new AssertException($"Sequences are not equal");
        }

        public static void Contains(string substring, string actual)
        {
            if (!actual.Contains(substring))
                throw new AssertException($"Expected \"{actual}\" to contain \"{substring}\"");
        }
    }
}