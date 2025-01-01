using System.Runtime.InteropServices;

namespace Wasm2Cil.OperatingSystem;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;

public class Fs
{
    private static ImmutableDictionary<int, FileStream> _fileDescriptors = ImmutableDictionary<int, FileStream>.Empty;
    private static int _nextFd = 3; // Starting from 3 to mimic typical Unix behavior

    [DllImport("libc", EntryPoint = "write", SetLastError = true)]
    private unsafe static extern IntPtr Write(int fileDescriptor, byte * buffer, int count);
    
    public static int open(CString _path)
    {
        var path = _path.ToString();
        if (string.IsNullOrEmpty(path))
            return -1;

        try
        {
            FileStream fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            int fd = Interlocked.Increment(ref _nextFd); // Atomically increment and get the next FD
            ImmutableInterlocked.Update(ref _fileDescriptors, (dict) => dict.Add(fd, fileStream));
            return fd;
        }
        catch (Exception)
        {
            return -1; // Return -1 to indicate failure
        }
    }

    public static int close(int fd)
    {
        FileStream? fileStream;
        if (ImmutableInterlocked.TryRemove(ref _fileDescriptors, fd, out fileStream))
        {
            try
            {
                fileStream?.Close();
                return 0; // Success
            }
            catch (Exception)
            {
                return -1; // Error during close
            }
        }
        return -1; // Invalid file descriptor
    }

    public static unsafe int write(int fd, void* buffer, int length)
    {
        var b2 = new ReadOnlySpan<byte>(buffer, length);
        if (_fileDescriptors.TryGetValue(fd, out FileStream? fileStream))
        {
            try
            {
                fileStream.Write(b2);
                return length; // Return number of bytes written
            }
            catch (Exception)
            {
                return -1; // Write error
            }
        }

        if (fd == 1)
        { // stdout
            Write(1, (byte *)buffer, length);
            return length;

            
        }
        return -1; // Invalid file descriptor
    }

    public static unsafe int read(int fd, void* buffer, int length)
    {
        var b2 = new Span<byte>(buffer, length);
        if (_fileDescriptors.TryGetValue(fd, out FileStream? fileStream))
        {
            try
            {
                
                int bytesRead = fileStream.Read(b2);
                return bytesRead; // Return number of bytes read
            }
            catch (Exception)
            {
                return -1; // Read error
            }
        }
        return -1; // Invalid file descriptor
    }
}

public class Sys
{
    public static int system2(CString c, CString arg)
    {
        var code = c.ToString();
        var args = arg.ToString();
        var path = Path.ChangeExtension(code, ".wasm");
        var bytes = File.ReadAllBytes(path);
        
        var p = OS.Current.StartProcess(WasmCode.FromBytes(bytes), code, [args]);
        return p.WaitForExit();
    }
    
}