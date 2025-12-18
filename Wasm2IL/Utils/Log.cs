namespace Wasm2IL.Utils;

internal static class Log
{
    public static bool Enabled = false;

    public static void WriteLine(string format, params object[] args)
    {
        if(Enabled)
            Console.WriteLine(format, args);
    }
}
