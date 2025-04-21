using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Wasm2CIl.Utils;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace Wasm2Cil.OperatingSystem;

public struct ApiDefinition
{
    public enum ArgumentType
    {
        IMMEDIATE,
        BUFFER,
        STRING,
        POINTER
    }

    public struct Argument
    {
        public Argument(int type, int fixedSize)
        {
            Type = (ArgumentType) type;
            FixedSize = fixedSize;
        }

        public ArgumentType Type { get; }
        public int FixedSize { get; }
    }

    public MethodInfo Callback { get; }
    public string Name { get; }
    public string Module { get; }
    public Argument[] Arguments { get; }

    public ApiDefinition(MethodInfo callback, string name, string module, Argument[] arguments)
    {
        Callback = callback;
        Name = name;
        Module = module;
        Arguments = arguments;
    }
}

class Process : IProcess
{
    public Thread thread;
    public int ExitCode;
    public readonly IProcess Parent;
    private readonly WasmAssembly assembly;
    private readonly MethodInfo overrides2;
    public WasmAssembly Assembly => assembly;

    private ApiDefinition[]? api;

    public IEnumerable<ApiDefinition> Api
    {
        get
        {
            if (api == null)
            {
                api = GetApiDefinition();
            }

            return api;
        }
    }

    ApiDefinition[] GetApiDefinition()
    {
        var list = new List<ApiDefinition>();
        if (overrides2 != null)
        {
            var or = (int) overrides2.Invoke(null, []);
            
            var heap = assembly.GetHeap();
            
            var unpacker = new Unpacker(heap, or);
            var apis = unpacker.ReadInt32();
            unpacker.Dereference();
            for (int i = 0; i < apis; i++)
            {
                var p = unpacker.DereferenceNew();
                var func = p.ReadInt32();
                var name = p.ReadString();
                var module = p.ReadString();
                var argCount = p.ReadInt32();
                p.Dereference();
                var args = new List<ApiDefinition.Argument>();
                for (int j = 0; j < argCount; j++)
                {
                    var type = p.ReadInt32();
                    var fixedSize = p.ReadInt32();

                    args.Add(new ApiDefinition.Argument(type, fixedSize));
                }

                var f = assembly.LookupFunction(func);
                var m = (f as MulticastDelegate)?.Method;

                list.Add(new ApiDefinition(m, name, module, args.ToArray()));
            }
        }

        return list.ToArray();
    }

    public Process(IProcess parent, Thread thread, WasmAssembly asm)
    {
        this.thread = thread;
        this.Parent = parent;
        this.assembly = asm; 
        this.overrides2 = assembly.GetMethod("__provide_overrides2__");
    }

    public int WaitForExit()
    {
        while (thread.IsAlive)
            Thread.Sleep(100);
        return ExitCode;
    }

    unsafe ref struct Unpacker
    {
        private readonly ReadOnlySpan<byte> heap;
        private int offset0;

        public Unpacker(ReadOnlySpan<byte> heap, int offset0)
        {
            this.heap = heap;
            this.offset0 = offset0;
        }

        T Read<T>() where T : struct
        {
            T r = MemoryMarshal.Cast<byte, T>(heap.Slice(offset0, Marshal.SizeOf<T>()))[0];
            offset0 += 4;
            return r;
        }

        public int ReadInt32() => Read<int>();

        public string ReadString()
        {
            var ptr = Read<int>();
            var x = heap.Slice(ptr);
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] == 0)
                {
                    return Encoding.UTF8.GetString(x.Slice(0, i));
                }
            }

            return "";
        }

        public void Dereference()
        {
            var ptr = ReadInt32();
            offset0 = ptr;
        }

        public Unpacker DereferenceNew()
        {
            var ptr = ReadInt32();
            return new Unpacker(heap, ptr);
        }
    }

            unsafe MethodReference WrapMethod(TypeDefinition cls, MethodReference m, ApiDefinition apiDefinition)
        {
            
            if (cls.Methods.FirstOrDefault(x => x.Name == m.Name + "__wrap2") is { } ext)
            {
                return ext;
            }

            var mod = cls.Module;

            var m2 = new MethodDefinition(m.Name + "__wrap2",
                Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Public,
                m.ReturnType);
            ConstructorInfo methodImplConstructor =
                typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]);
            // Create a CustomAttributeBuilder with the MethodImplOptions value
            var attr = new CustomAttribute(mod.ImportReference(methodImplConstructor));
            attr.ConstructorArguments.Add(new CustomAttributeArgument(
                mod.ImportReference(typeof(MethodImplOptions)), MethodImplOptions.AggressiveInlining));
            m2.CustomAttributes.Add(attr);

            cls.Methods.Add(m2);
            var il2 = m2.Body.GetILProcessor();
            int argidx = 0;
            foreach (var (p, arg) in m.Parameters.Zip(apiDefinition.Arguments))
            {
                var p2 = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                m2.Parameters.Add(p2);
               
                il2.Emit(OpCodes.Ldarg, argidx);
                argidx += 1;
            }


            il2.Emit(OpCodes.Call, m);
            il2.Emit(OpCodes.Ret);
            return m2;
        }

    
    public void ResolveImport(ResolveImportEventArgs args)
    {
        foreach (var a in Api)
        {
            if (a.Module == args.ModuleName && a.Name == args.Name)
            {
                
                var m = a.Callback;
                /*if (args.ModuleName == "fs" && args.Name == "write")
                {
                    fakeWrite = m;
                    m = typeof(Redirects).GetMethod(nameof(Redirects.write));
                }

                args.Result = m;
                args.Handled = true;*/
                var mref = args.ModuleDefinition.ImportReference(m);
                
                args.Result = WrapMethod(args.TypeBuilder, mref, a);
                args.Handled = true;
                return;
            }
        }

        Parent?.ResolveImport(args);
    }

    public MethodInfo fakeWrite;
    public readonly FieldInfo callerHeap;

    IProcess IProcess.Parent => this.Parent;
}

public class Redirects
{
    public static unsafe int write(int fd, void* buffer, int length)
    {
        // its a callback
        var caller = (Process) OS.Current.GetCurrentProcess();
        var callee = (Process) caller.Parent;

        var a = callee.Assembly.Malloc(length);
        var s = callee.Assembly.GetHeapSpan(a, length);
        new Span<byte>(buffer, length).CopyTo(s);
        var r = (int) callee.fakeWrite.Invoke(null, [fd, a, length]);
        callee.Assembly.Free(a);
        return r;
    }
}