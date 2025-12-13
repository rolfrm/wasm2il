using System.Diagnostics;
using System.Reflection;

namespace Wasm2IL;

public class Program
{
    public static void Main()
    {
        var args = Environment.GetCommandLineArgs();
        string? run = null;
        string? file = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--run")
            {
                run = args[i + 1];
                i += 1;
            }
            else if (args[i] == "--help")
            {
                Console.WriteLine("Usage: wasm2il <file.wasm> [--run <function>]");
                return;
            }
            else
            {
                file = args[i];
            }
        }

        if (file == null)
            throw new ArgumentException("WASM file not specified");

        string dllName = Path.ChangeExtension(file, ".dll");
        using var fstr = File.OpenRead(file);
        new Transformer().Transform(fstr, Path.GetFileNameWithoutExtension(file), dllName);

        if (run != null)
        {
            var asm = Assembly.LoadFile(Path.GetFullPath(dllName));
            var m = asm.ExportedTypes.First().GetMethod(run)
                ?? throw new InvalidOperationException($"Method '{run}' not found");
            var sw = Stopwatch.StartNew();
            m.Invoke(null, null);
            Console.WriteLine($"Done {sw.ElapsedMilliseconds}ms");
        }
    }
}