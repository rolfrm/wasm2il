namespace Wasm2IL.Utils;

internal class Log
{
    public static void WriteLine(string information, params object[] args)
    {
        
        Console.WriteLine(information, args);
    }
}