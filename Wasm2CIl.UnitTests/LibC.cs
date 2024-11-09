using System.Runtime.CompilerServices;

namespace Wasm2Cil.UnitTests;

public class LibC
{
    public static int strlen(CString p)
    {
        return p.Length;
    }
    
    public static unsafe int memcpy(void * dst, void * src, int c)
    {
        var d = (byte*) dst;
        var s = (byte*) src;
        for (int i = 0; i < c; i++)
            d[i] = s[i];
        return 0;
    }

    public static unsafe void memfill(void * dst, int value, int c)
    {
        var d = (byte*) dst;
        for (int i = 0; i < c; i++)
            d[i] = (byte)value;
    }
    
    
    public unsafe static void TestThing()
    {
        byte[] memory = { 10, 20, 30, 40, 50, 60, 70, 80 }; // Example array
        fixed (byte* ptr = memory)
        {
            memcpy(ptr + 5, ptr, 5);
        }
    }
}