using System.Runtime.InteropServices;

namespace Wasm2IL;

public class MemoryAllocator
{
    // Define constants for allocation
    private const int MEM_COMMIT = 0x1000;
    private const int MEM_RESERVE = 0x2000;
    private const int PAGE_READWRITE = 0x04;

    
    public static unsafe byte * AllocateMemory(long size)
    {
        if (IsWindows())
        {
            return VirtualAlloc(IntPtr.Zero, new IntPtr(size), MEM_RESERVE, PAGE_READWRITE);
        }
        else if (IsLinux() || IsMac())
        {
            var r= mmap(IntPtr.Zero, size, MmapProt.PROT_READ | MmapProt.PROT_WRITE, 
                MmapFlags.MAP_PRIVATE | (IsLinux() ? MmapFlags.MAP_ANONYMOUS_LINUX : MmapFlags.MAP_ANONYMOUS),
                -1, IntPtr.Zero);
            return r;
        }

        throw new Exception("Unreachable");
    }

    // Windows VirtualAlloc P/Invoke
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe byte * VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, int flAllocationType, int flProtect);

    // Linux and macOS mmap P/Invoke
    [DllImport("libc", SetLastError = true)]
    private static extern unsafe byte * mmap(IntPtr addr, long length, MmapProt prot, MmapFlags flags, int fd, IntPtr offset);

    // Check if the current platform is Windows
    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    // Check if the current platform is Linux
    private static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    // Check if the current platform is macOS
    private static bool IsMac()
    {
        
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    // Mmap flags for Linux/macOS
    [Flags]
    public enum MmapFlags : int
    {
        MAP_PRIVATE = 0x02,
        MAP_ANONYMOUS_LINUX = 0x20,
        MAP_ANONYMOUS = 0x1000
    }

    // Mmap protection flags for Linux/macOS
    [Flags]
    public enum MmapProt : int
    {
        PROT_READ = 0x1,
        PROT_WRITE = 0x2,
        PROT_EXEC = 0x4
    }
}