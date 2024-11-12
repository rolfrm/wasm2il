using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Wasm2Cil;

public class WasmAssembly
{
    private readonly Assembly asm;
    private readonly Type code;
    private MethodInfo malloc;
    private MethodInfo free;
    public WasmAssembly(Assembly asm)
    {
        this.asm = asm;
        this.code = asm.ExportedTypes.FirstOrDefault();
        malloc = code.GetMethod("malloc");
        free = code.GetMethod("free");
    }

    public int Malloc(int len)
    {
        int ptr = (int)malloc.Invoke(null, new object[]{len});
        return ptr;
    }

    void Free(int ptr)
    {
        free.Invoke(null, new object[]{ptr});
    }

    public byte[] GetHeap()
    {
        return (byte[]) code.GetField("Memory").GetValue(null);
    }

    public int StringToHeap(string str)
    {
        var bc = System.Text.Encoding.UTF8.GetByteCount(str);
        int s = Malloc(bc + 1);
        var span = GetHeap().AsSpan(s, bc + 1);
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

    public Span<byte> GetHeapSpan(int i, int len)
    {
        return GetHeap().AsSpan(i, len);
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
}