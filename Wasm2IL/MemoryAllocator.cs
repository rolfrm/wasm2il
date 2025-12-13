using System.Runtime.InteropServices;

namespace Wasm2IL;

public static class MemoryAllocator
{
    const int MEM_RESERVE = 0x2000;
    const int PAGE_READWRITE = 0x04;

    public static unsafe byte* AllocateMemory(long size)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return VirtualAlloc(IntPtr.Zero, new IntPtr(size), MEM_RESERVE, PAGE_READWRITE);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return mmap(IntPtr.Zero, size, MmapProt.PROT_READ | MmapProt.PROT_WRITE,
                MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANONYMOUS_LINUX, -1, IntPtr.Zero);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return mmap(IntPtr.Zero, size, MmapProt.PROT_READ | MmapProt.PROT_WRITE,
                MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANONYMOUS, -1, IntPtr.Zero);

        throw new PlatformNotSupportedException("Unsupported platform for memory allocation");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe byte* VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, int flAllocationType, int flProtect);

    [DllImport("libc", SetLastError = true)]
    static extern unsafe byte* mmap(IntPtr addr, long length, MmapProt prot, MmapFlags flags, int fd, IntPtr offset);

    [Flags]
    enum MmapFlags
    {
        MAP_PRIVATE = 0x02,
        MAP_ANONYMOUS_LINUX = 0x20,
        MAP_ANONYMOUS = 0x1000
    }

    [Flags]
    enum MmapProt
    {
        PROT_READ = 0x1,
        PROT_WRITE = 0x2
    }
}
