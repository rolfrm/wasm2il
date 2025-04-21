using System.Collections.Immutable;
using System.Reflection;

namespace Wasm2Cil.OperatingSystem;

public interface IProcess
{
    int WaitForExit();
    void ResolveImport(ResolveImportEventArgs args); 
    IProcess Parent { get; }
}

public class OS : IProcess 
{
    [ThreadStatic] public static OS Current;
    ImmutableList<Process> processes = ImmutableList<Process>.Empty;

    IProcess ProcessByThread(Thread trd) => (IProcess)processes.FirstOrDefault(p => p.thread == trd) ?? this;

    public void Load()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
    }

    private Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        foreach (var proc in processes)
        {
            var name = args.Name;
            var name2 = proc.Assembly.Assembly.FullName;
            if (name == name2)
                return proc.Assembly.Assembly;
        }
        

        return null;
    }

    public IProcess StartProcess(IWasmCode wasm, string name, string[] arguments)
    {
        var parentProcess = ProcessByThread(Thread.CurrentThread);
        Current = this;
        var tform = new Transformer();
        tform.OnResolveImport += TformOnOnResolveImport;
        
        using var fstr = wasm.GetCodeStream();

        var vCounter = processes.Count(p => (p.Parent as Process)?.Assembly.Name == name);
        
        var asm = tform.LoadWasmAssembly(fstr, name, $"{name}.dll", $"{vCounter + 1}.0.0");

        int argc = arguments.Length + 1;
        int argvp = asm.Malloc(argc * 4);
        for (int i = 0; i < arguments.Length; i++)
        {
            var span = asm.GetHeapSpan<int>(argvp + (i + 1) * 4, 1);
            span[0] = asm.StringToHeap(arguments[i]);
        }

        Process thisProcess = null;
        var trd = new Thread(() =>
        {
            Current = this;
            int exitCode = (int)asm.Invoke("main", [argc, argvp]);
            thisProcess.ExitCode = exitCode;
            ImmutableInterlocked.Update(ref processes, p=> p.Remove(thisProcess));
        });

        
        thisProcess = new Process(parentProcess, trd, asm);
        ImmutableInterlocked.Update(ref processes, p => p.Add(thisProcess));
        trd.Start();
        return thisProcess;
    }

    private void TformOnOnResolveImport(object? sender, ResolveImportEventArgs e)
    {
        var parentProcess = ProcessByThread(Thread.CurrentThread);
        parentProcess.ResolveImport(e);
        if (!e.Handled)
        {
            parentProcess.Parent?.ResolveImport(e);
        }

    }

    /*
    public IProcess StartProcess(Type app, string name, string[] arguments)
    {
        var tform = new Transformer();
        tform.LoadImportModule("fs", typeof(Fs));
        
        
        Process thisProcess = null;
        var trd = new Thread(() =>
        {
            app.Invoke("main", arguments);
            ImmutableInterlocked.Update(ref processes, p=> p.Remove(thisProcess));
        });
        
        thisProcess = new Process(trd);
        ImmutableInterlocked.Update(ref processes, p => p.Add(thisProcess));
        trd.Start();
        return thisProcess;
    }*/
    public int WaitForExit()
    {
        throw new Exception("Invalid operation");
    }

    public void ResolveImport(ResolveImportEventArgs args)
    {
        MethodInfo? method;
        switch (args.ModuleName)
        {
            case "fs": method = typeof(Fs).GetMethod(args.Name); break;
            case "sys": method = typeof(Sys).GetMethod(args.Name); break;
            case "log": method = typeof(Log).GetMethod(args.Name); break;
            case "console": method = typeof(Console2).GetMethod(args.Name); break;
            default: return;
        }

        if (method != null)
        {
            args.Result = method;
            args.Handled = true;
        }
    }

    public class Log
    {
        public static void debug(CString msg)
        {
            Console.WriteLine(msg.ToString());
        }
    }

    public IProcess Parent => null;

    public IProcess GetCurrentProcess()
    {
        return ProcessByThread(Thread.CurrentThread);
    }
}

public class Console2
{
    public static unsafe void write(CString buffer)
    {
        Console.Write(buffer.ToString());
        
    }

    public static int width()
    {
        return Console.WindowWidth;
    }
    
    public static int height()
    {
        return Console.WindowHeight;
    }

    public static void cursor(int x, int y)
    {
        Console.SetCursorPosition(x, y);
    }
    
}