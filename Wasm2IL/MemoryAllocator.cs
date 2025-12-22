using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wasm2IL;

public static class MemoryAllocator
{
    const int MEM_RESERVE = 0x2000;
    const int MEM_COMMIT = 0x1000;
    const int MEM_RELEASE = 0x8000;
    const int PAGE_READWRITE = 0x04;

    static unsafe byte* AllocateMemory0(long size)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return VirtualAlloc(IntPtr.Zero, new IntPtr(size),
                MEM_COMMIT | MEM_RESERVE,
                PAGE_READWRITE);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return mmap(IntPtr.Zero, size, MmapProt.PROT_READ | MmapProt.PROT_WRITE,
                MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANONYMOUS_LINUX, -1, IntPtr.Zero);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return mmap(IntPtr.Zero, size, MmapProt.PROT_READ | MmapProt.PROT_WRITE,
                MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANONYMOUS, -1, IntPtr.Zero);

        throw new PlatformNotSupportedException("Unsupported platform for memory allocation");
    }

    public static unsafe byte* AllocateMemory(long size)
    {
        var r = AllocateMemory0(size);
        if (r == null)
            throw new Exception("Unable to allocate memory");

        return r;
    }

    /// <summary>
    /// Allocates memory with an indirection pointer. Returns byte** that points to
    /// a location containing the actual heap pointer. This allows the heap pointer
    /// to be updated (e.g., during growth) without invalidating cached values.
    /// </summary>
    /// <param name="size">Size of the heap to allocate</param>
    /// <returns>Pointer to pointer (byte**) - dereference to get actual heap</returns>
    public static unsafe byte** AllocateMemoryIndirect(long size)
    {
        // Allocate 8 bytes to hold the pointer
        var ptrStorage = (byte**)AllocateMemory(8);

        // Allocate the actual heap
        var heap = AllocateMemory(size);

        // Store heap pointer at the indirection location
        *ptrStorage = heap;

        return ptrStorage;
    }

    /// <summary>
    /// Grows memory from currentSize to newSize. First tries to extend the current region in place,
    /// otherwise allocates new memory and copies the data.
    /// </summary>
    /// <param name="currentPtr">Current memory pointer</param>
    /// <param name="currentSize">Current allocated size in bytes</param>
    /// <param name="newSize">New requested size in bytes</param>
    /// <returns>Pointer to the grown memory region (may be the same or different from currentPtr)</returns>
    public static unsafe byte* GrowMemory(byte* currentPtr, long currentSize, long newSize)
    {
        if (newSize <= currentSize)
            return currentPtr;

        if (currentPtr == null)
            return AllocateMemory(newSize);

        // Try to extend in place first
        var extended = TryExtendMemory(currentPtr, currentSize, newSize);
        if (extended != null)
            return extended;

        // Extension failed - allocate new memory and copy
        var newPtr = AllocateMemory(newSize);
        Unsafe.CopyBlock(newPtr, currentPtr, (uint)currentSize);
        FreeMemory(currentPtr, currentSize);
        return newPtr;
    }

    static unsafe byte* TryExtendMemory(byte* currentPtr, long currentSize, long newSize)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: Use mremap which can efficiently extend or relocate
            var result = mremap(currentPtr, currentSize, newSize, MremapFlags.MREMAP_MAYMOVE);
            if (result != (byte*)-1)
                return result;
            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Try to allocate memory immediately after the current block
            var nextAddr = currentPtr + currentSize;
            var additionalSize = newSize - currentSize;
            var extended = VirtualAlloc(new IntPtr(nextAddr), new IntPtr(additionalSize),
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (extended == nextAddr)
                return currentPtr; // Successfully extended in place

            // If we got memory somewhere else, free it
            if (extended != null)
                VirtualFree(new IntPtr(extended), IntPtr.Zero, MEM_RELEASE);
            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: Try to map memory immediately after the current block
            var nextAddr = currentPtr + currentSize;
            var additionalSize = newSize - currentSize;
            var extended = mmap(new IntPtr(nextAddr), additionalSize,
                MmapProt.PROT_READ | MmapProt.PROT_WRITE,
                MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANONYMOUS | MmapFlags.MAP_FIXED_NOREPLACE,
                -1, IntPtr.Zero);

            // MAP_FIXED_NOREPLACE returns MAP_FAILED if the address is in use
            if (extended == nextAddr)
                return currentPtr; // Successfully extended in place

            // If we got memory somewhere else (shouldn't happen with NOREPLACE), free it
            if (extended != null && extended != (byte*)-1)
                munmap(extended, additionalSize);
            return null;
        }

        return null;
    }

    static unsafe void FreeMemory(byte* ptr, long size)
    {
        if (ptr == null)
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            VirtualFree(new IntPtr(ptr), IntPtr.Zero, MEM_RELEASE);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            munmap(ptr, size);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe byte* VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, int flAllocationType, int flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, int dwFreeType);

    [DllImport("libc", SetLastError = true)]
    static extern unsafe byte* mmap(IntPtr addr, long length, MmapProt prot, MmapFlags flags, int fd, IntPtr offset);

    [DllImport("libc", SetLastError = true)]
    static extern unsafe int munmap(byte* addr, long length);

    [DllImport("libc", SetLastError = true)]
    static extern unsafe byte* mremap(byte* old_address, long old_size, long new_size, MremapFlags flags);

    [Flags]
    enum MmapFlags
    {
        MAP_PRIVATE = 0x02,
        MAP_FIXED = 0x10,
        MAP_ANONYMOUS_LINUX = 0x20,
        MAP_ANONYMOUS = 0x1000,
        MAP_FIXED_NOREPLACE = 0x100000  // Linux 4.17+, macOS uses similar behavior
    }

    [Flags]
    enum MmapProt
    {
        PROT_READ = 0x1,
        PROT_WRITE = 0x2
    }

    [Flags]
    enum MremapFlags
    {
        MREMAP_MAYMOVE = 1
    }
}
