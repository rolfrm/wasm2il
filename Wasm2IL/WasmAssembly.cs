using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Wasm2Cil;

public class WasmAssembly
{
    public string Name => code.Name;
    private readonly Assembly asm;
    private readonly Type code;
    private MethodInfo malloc;
    private MethodInfo free;

    public Assembly Assembly => asm;
    
    public WasmAssembly(Assembly asm)
    {
        this.asm = asm;
        this.code = asm.ExportedTypes.FirstOrDefault();
        malloc = code.GetMethod("malloc");
        free = code.GetMethod("free");
    }

    public int Malloc(int len)
    {
        if (malloc == null)
        {
            return FakeMalloc(len);
        }
        int ptr = (int)malloc.Invoke(null, [len]);
        return ptr;
    }

    public void Free(int ptr)
    {
        if (malloc == null)
            return;
            // leak
        free.Invoke(null, [ptr]);
    }

    public unsafe int FakeMalloc(int len)
    {
        var p = new IntPtr(Pointer.Unbox(code.GetField("Memory").GetValue(null))); 
        int memSize = (int)code.GetField("MemorySize").GetValue(null);
        
        p = Marshal.ReAllocHGlobal(p, memSize + len);
        code.GetField("Memory").SetValue(null, Pointer.Box(p.ToPointer(), typeof(byte*)));
        code.GetField("MemorySize").SetValue(null, memSize + len);
        return memSize;
    }

    public unsafe Span<byte> GetHeap()
    {
        return  new Span<byte>((System.Reflection.Pointer.Unbox(code.GetField("Memory").GetValue(null))), 
            (int)code.GetField("MemorySize").GetValue(null));
    }

    public int StringToHeap(string str)
    {
        var bc = System.Text.Encoding.UTF8.GetByteCount(str);
        int s = Malloc(bc + 1);
        var span = GetHeap().Slice(s, bc + 1);
        span[bc] = 0;
        System.Text.Encoding.UTF8.GetBytes(str, span);
        return s;
    }

    public object Invoke(string methodName, params object[] args)
    {
        
        var toFree = ImmutableList<int>.Empty; 
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is string str)
            {
                var ptr = StringToHeap(str);
                args[i] = ptr; 
                toFree = toFree.Add(ptr);
            }
        }
        var m = code.GetMethod(methodName);
        var result = m
            .Invoke(null, args);
        foreach (var ptr in toFree)
            Free(ptr);
        return result;
    }

    public MethodInfo GetMethod(string name)
    {
        return this.code.GetMethod(name);
    }
    
    public FieldInfo GetField(string name)
    {
        return this.code.GetField(name);
    }

    public Span<byte> GetHeapSpan(int i, int len)
    {
        return GetHeap().Slice(i, len);
    }
    public Span<T> GetHeapSpan<T>(int i, int len) where T: struct
    {
        return MemoryMarshal.Cast<byte, T>(GetHeap().Slice(i, len * Marshal.SizeOf<T>()));
    }

    public T GetHeapObject<T>(int ptr) where T: struct
    {
        var size = Marshal.SizeOf<T>();
        Span<byte> bytes = GetHeapSpan(ptr, size);
        Span<T> typedSpan = MemoryMarshal.Cast<byte, T>(bytes);
        return typedSpan[0];
    }

    public string GetHeapString(int ptr)
    {
        
        return new CString(GetHeap(), ptr).ToString();
    }

    public object LookupFunction(int i)
    {
        var ftable = (Array)code.GetField("FunctionTable", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        return ftable.GetValue(i);
    }
}