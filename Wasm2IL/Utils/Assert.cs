namespace Wasm2IL;

static class Assert
{
    class AssertException(string message) : Exception(message);

    public static void AreEqual<T>(T expected, T actual)
    {
        if (!Equals(expected, actual))
            throw new AssertException($"Expected {expected}, got {actual}");
    }

    public static void AreEqual(float expected, float actual, float tolerance = 0.0001f)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new AssertException($"Expected {expected}, got {actual}");
    }

    public static void AreEqual(double expected, double actual, double tolerance = 0.00001)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new AssertException($"Expected {expected}, got {actual}");
    }

    public static void IsTrue(bool condition, string message = "Assertion failed")
    {
        if (!condition)
            throw new AssertException(message);
    }
}
