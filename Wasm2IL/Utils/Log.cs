namespace Wasm2IL.Utils;

internal static class Log
{
    public static void WriteLine(string format, params object[] args) =>
        Console.WriteLine(format, args);
}
