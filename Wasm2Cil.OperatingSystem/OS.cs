using System.Collections.Immutable;
using System.Reflection;

namespace Wasm2Cil.OperatingSystem;

public interface IProcess
{
    int WaitForExit();
}

class Process : IProcess
{
    public Thread thread;
    public int ExitCode;
    public readonly IProcess Parent;

    public Process(IProcess parent, Thread thread)
    {
        this.thread = thread;
        this.Parent = parent;
    }

    public int WaitForExit()
    {
        while(thread.IsAlive)
            Thread.Sleep(100);
        return ExitCode;
    }
    
    
}

public class OS : IProcess
{
    [ThreadStatic] public static OS Current;
    ImmutableList<Process> processes = ImmutableList<Process>.Empty;

    IProcess ProcessByThread(Thread trd) => (IProcess)processes.FirstOrDefault(p => p.thread == trd) ?? this;
    
    public IProcess StartProcess(IWasmCode wasm, string name, string[] arguments)
    {
        var parentProcess = ProcessByThread(Thread.CurrentThread);
        Current = this;
        var tform = new Transformer();
        tform.LoadImportModule("fs", typeof(Fs));
        tform.LoadImportModule("sys", typeof(Sys));
        using var fstr = wasm.GetCodeStream();
        var asm = tform.LoadWasmAssembly(fstr, name, "os-tmp.dll");

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
        
        thisProcess = new Process(parentProcess, trd);
        ImmutableInterlocked.Update(ref processes, p => p.Add(thisProcess));
        trd.Start();
        return thisProcess;
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
}