using System.Diagnostics;
using System.Reflection;

namespace Wasm2Il
{
    public class Program
    {
        public static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            string run = null;
            string file = null;
            bool help = false;
            for(int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--run")
                {
                    run = args[i + 1];
                    i += 1;
                }else if (args[i] == "--help")
                    help = true;
                else
                {
                    file = args[i];
                }
            }

            if (file == null)
                throw new ArgumentException("File not specified", "--file");
            
            string dllName = Path.ChangeExtension(file, ".dll");
            if (file != null)
            {
                var fstr = File.OpenRead(file);
                new Transformer().Go(fstr, Path.GetFileNameWithoutExtension(file), dllName);
            }

            if (run != null)
            {  
                var asm = Assembly.LoadFile(Path.GetFullPath(dllName));
                var m = asm.ExportedTypes.First().GetMethod(run);
                var sw = Stopwatch.StartNew();
                m.Invoke(null, null);
                Console.WriteLine("Done " + sw.ElapsedMilliseconds + "ms");
            }
        }
    }
}