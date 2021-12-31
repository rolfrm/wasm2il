using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wasm2Il;

public class Wasi
{
    public class Context
    {
        public Dictionary<int, FileStream> fds = new Dictionary<int, FileStream>();
        public Dictionary<int, DirectoryInfo> dirs = new Dictionary<int, DirectoryInfo>();
        public byte[] Memory => (byte[])t.GetField("Memory", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private Type t;
        public Context(RuntimeTypeHandle rt)
        {
            t = Type.GetTypeFromHandle(rt);
        }

        public object Call(string method, params object[] args)
        {
            var x = t.GetMethod(method);
            return x.Invoke(null, args);
        }

        private Dictionary<string, int> inodes = new Dictionary<string, int>();
        private int inodesCounter = 5;

        public int InodeForFile(string fileName)
        {
            if(inodes.TryGetValue(fileName, out var inode))
                return inode;
            return inodes[fileName] = inodesCounter++;

        }

        public object LookupFd(int fd)
        {
            if (dirs.TryGetValue(fd, out var dir))
                return dir;
            return GetFdStream(fd);
        }

        Stream? stdout;
        public Stream GetFdStream(int fd)
        {
            if (fd == 1)
            {
                return stdout ??= Console.OpenStandardOutput();
            }

            if (fd == 2)
            {
                return Console.OpenStandardInput();
            }

            if (fds.TryGetValue(fd, out var str))
                return str;
            return null;
        }

        

        public int OpenFileOrDir(string pa)
        {
            var dirinfo = new DirectoryInfo(pa);
            if (dirinfo.Exists)
            {
                for (int i = 1001; i < 2000; i++)
                {
                    if (dirs.ContainsKey(i) == false)
                    {
                        dirs[i] = dirinfo;
                        return i;
                    }
                }
            }
            var fst = File.Open(pa, FileMode.OpenOrCreate);
            for (int i = 10; i < 1000; i++)
            {
                if (fds.ContainsKey(i) == false)
                {
                    fds[i] = fst;
                    return i;
                }
            }

            throw new Exception("Out of handles");

        }

        public void CloseFd(int fd)
        {
            if (fds.TryGetValue(fd, out var ctx))
            {
                ctx.Close();
                fds.Remove(fd);
            }
        }
    }

    private static Dictionary<IntPtr, Context> contexts = new Dictionary<IntPtr, Context>(); 
    public static Context GetContext(RuntimeTypeHandle t)
    {
        if (contexts.TryGetValue(t.Value, out var ctx))
            return ctx;
        return contexts[t.Value] = new Context(t);
    }
    
    public static void abort(Context ctx)
    {
        ctx.Call("fflush", 0);
        
        throw new Exception("Operation aborted");
    }

    public static int testWrap(int x, Context ctx)
    {
        var test = ctx.Call("testWrap_pre", 0);
        Assert.AreEqual(test, 5);
        return 0;
    }
    const int F_GETLK = 5;
    const int F_SETLK = 6;
    public static int fcntl(int fd, int cmd, int args, Context ctx)
    {
        ctx.Call("fflush", 0);
        if (cmd == F_SETLK)
        {
            return 0;
        }

        if (cmd == F_GETLK)
        {
            return 0;
        }
        var test = ctx.Call("fcntl_pre", fd, cmd, args);
        return (int)test;
    }

    public enum __wasi_rights_t : ulong
    {
        
        FD_DATASYNC = 1,
        FD_READ = 2,
        FD_SEEK = 4,
        FDSTAT_SET_FLAGS = 8,
        FD_SYNC = 16,
        FD_TELL = 32,
        FD_WRITE = 64,
        FD_ADIVCE = 128,
        FD_ALLOCATE = 256,
        PATH_CREATE_DIRECTORY = 512,
        CREATE_FILE = 1 << 10,
        LINK_SOURCE = 1 << 11,
        LINK_TARGET = 1 << 12,
        PATH_OPEN = 1 << 13,
        READDIR = 1 << 14,
        READLINK = 1 << 15,
        RENAME_SOURCE = 1 << 16,
        RENAME_TARGET = 1 << 17,
        PATH_FILESTAT_GET = 1 << 18,
        PATH_FILESTAT_SET_SIZE = 1 << 19,
        PATH_FILESTAT_SET_TIMES = 1 << 20,
        FD_FILESTAT_GET = 1 << 21,
        FD_FILESTAT_SET_SIZE = 1 << 22,
        FD_FILESTAT_SET_TIMES = 1 << 23,
        PATH_READ_SYMLINK = 1 << 24,
        PATH_REMOVE_DIRECTORY = 1 << 25,
        PATH_UNLINK_FILE = 1 << 26,
        POLL_FD_READWRITE = 1 << 27,
        SOCK_SHUTDOWN = 1 << 28,
        ALL = 0xFFFFFFFF
        
    }

    public enum __wasi_filetype_t :byte
    {
        Unknown = 0,
        BlockDevice = 1,
        CharacterDevice = 2,
        Directory = 3,
        RegularFile = 4,
        SocketDgram = 5,
        SocketStream = 6,
        SymbolicLink = 7
    }
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    struct __wasi_fdstat_t {
        /**
     * File type.
     */
        public __wasi_filetype_t fs_filetype;

        /**
     * File descriptor flags.
     */
        public FdFlags fs_flags;

        /**
     * Rights that apply to this file descriptor.
     */
        public __wasi_rights_t fs_rights_base;

        /**
     * Maximum set of rights that may be installed on new file descriptors that
     * are created through this file descriptor, e.g., through `path_open`.
     */
        public __wasi_rights_t fs_rights_inheriting;

    }

    /*public static int __wasilibc_find_relpath(int pathptr, )
    {
        
    }*/
    public static int fd_fdstat_get(int fd, int retptr0, Context context)
    {
        var str = context.GetFdStream(fd);
        
        __wasi_fdstat_t stat = new __wasi_fdstat_t()
        {
        };
        stat.fs_rights_base = __wasi_rights_t.ALL;
        if (str is FileStream fstr2)
        {
            stat.fs_filetype = __wasi_filetype_t.RegularFile;
        }
        else if (str is Stream)
        {
            stat.fs_filetype = __wasi_filetype_t.CharacterDevice;
        }else if (fd == 4)
        {
            stat.fs_filetype = __wasi_filetype_t.Directory;
            stat.fs_rights_base = __wasi_rights_t.ALL;
            stat.fs_rights_inheriting = __wasi_rights_t.ALL;
        }
        else
        {
            return -1;
        }
        
        Unsafe.As<byte, __wasi_fdstat_t>(ref context.Memory[retptr0]) = stat;
        return 0;
    }

    public static int args_get(int a, int b, Context context)
    {
        throw new NotImplementedException("");
    }
    
    public static int args_sizes_get(int a, int b, Context context)
    {
        throw new NotImplementedException("");
    }
    public static int environ_get(int a, int b, Context context)
    {
        //throw new NotImplementedException("");
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    struct ciovec_t
    {
        public int bufptr;
        public int size;
    }
    
    public static int fd_write(int fd, int iov, int iov_len, int n_written, Context context)
    {
        var memory = context.Memory;
        int written = 0;
        for (int i = 0; i < iov_len; i++)
        {
            ciovec_t p = Unsafe.Add(ref Unsafe.As<byte, ciovec_t>(ref memory[iov]), i);
            written += p.size;
            var span = memory.AsSpan(p.bufptr, p.size);
            var stream = context.GetFdStream(fd);
            stream.Write(span);
            
        }

        Unsafe.As<byte, uint>(ref memory[n_written]) = (uint) written;
        
        return 0;
    }
    
    public static int environ_sizes_get(int P_0, int P_1, Context context)
    {
        //throw new NotImplementedException("Not Implemented");
        return 0;
    }

    public static int clock_res_get(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int clock_time_get(int P_0, long P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_advise(int P_0, long P_1, long P_2, int P_3, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_allocate(int P_0, long P_1, long P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_close(int fd, Context context)
    {
        context.CloseFd(fd);
        return 0;
    }
    
    public static int fd_datasync(int P_0, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    
    public static int fd_fdstat_set_flags(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_fdstat_set_rights(int P_0, long P_1, long P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    struct __wasi_device_t
    {
        public ulong id;
    }

    struct __wasi_inode_t
    {
        public ulong id;
    }

    struct __wasi_linkcount_t
    {
        public ulong count;
    }
    
    struct __wasi_filesize_t
    {
        public ulong count;
    }

    struct __wasi_timestamp_t
    {
        public ulong time;
    }

    [StructLayout(LayoutKind.Sequential, Pack=0)]
     struct __wasi_filestat_t {
        /**
     * Device ID of device containing the file.
     */
        public __wasi_device_t dev;

        /**
     * File serial number.
     */
        public __wasi_inode_t ino;

        /**
     * File type.
     */
        public __wasi_filetype_t  filetype;

        /**
     * Number of hard links to the file.
     */
        public __wasi_linkcount_t nlink;

        /**
     * For regular files, the file size in bytes. For symbolic links, the length in bytes of the pathname contained in the symbolic link.
     */
        public __wasi_filesize_t size;

        /**
     * Last data access timestamp.
     */
        public __wasi_timestamp_t atim;

        /**
     * Last data modification timestamp.
     */
        public __wasi_timestamp_t mtim;

        /**
     * Last file status change timestamp.
     */
        public __wasi_timestamp_t ctim;

    }

     static __wasi_filestat_t fileStatFromString(Context ctx, string str)
     {
         var x = new __wasi_filestat_t();
         var info = new FileInfo(str);
         if (info.Exists == false)
             return x;
         x.filetype = __wasi_filetype_t.RegularFile;
         
             
         x.size.count = (ulong)info.Length;
         x.nlink.count = 1;
         x.ino.id = (ulong)ctx.InodeForFile(str);
         x.atim.time = (ulong)info.LastAccessTimeUtc.ToFileTime(); 
         x.ctim.time = (ulong)info.CreationTimeUtc.ToFileTime(); 
         x.mtim.time = (ulong)info.LastWriteTimeUtc.ToFileTime();
         return x;
     }
    public static int fd_filestat_get(int fd, int retptr, Context context)
    {
        var fstr = context.GetFdStream(fd) as FileStream;
        if (fstr == null) return -1;
        var x = fileStatFromString(context, fstr.Name);
        x.size.count = (ulong)fstr.Length;
        x.nlink.count = 1;
        Unsafe.As<byte, __wasi_filestat_t>(ref context.Memory[retptr]) = x;
        return 0;
    }


    public static int fd_filestat_set_size(int P_0, long P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_filestat_set_times(int P_0, long P_1, long P_2, int P_3, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_pread(int P_0, int P_1, int P_2, long P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_prestat_get(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_prestat_dir_name(int P_0, int P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_pwrite(int P_0, int P_1, int P_2, long P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_read(int fd, int iov, int iov_len, int retPtrs, Context context)
    {
        var stream = context.GetFdStream(fd);
        var memory = context.Memory;
        int read = 0;
        for (int i = 0; i < iov_len; i++)
        {
            ciovec_t p = Unsafe.Add(ref Unsafe.As<byte, ciovec_t>(ref memory[iov]), i);
            var span = memory.AsSpan(p.bufptr, p.size);
            read += stream.Read(span);
        }

        Unsafe.As<byte, int>(ref memory[retPtrs]) = read;
        return 0;
    }

    public static int fd_readdir(int P_0, int P_1, int P_2, long P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_renumber(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int fd_seek(int fd, long offset, SeekOrigin whence, int retptr, Context context)
    {
        var fptr = context.GetFdStream(fd);
        var o = (ulong)fptr.Seek(offset, whence);
        var mem = context.Memory;
        Unsafe.As<byte, ulong>(ref mem[retptr]) = o;
        return 0;
    }
    
    public static int fd_sync(int fd, Context context)
    {
        var obj = context.LookupFd(fd);
        if (obj is FileStream fs)
            fs.Flush();
        if (obj is DirectoryInfo)
        {
            // cannot sync directory.
            
        }
        return 0;
    }
    public static int fd_tell(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_create_directory(int P_0, int P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int path_filestat_get(int dirFd, LookupFlags flags, int path, int pathlen, int retptr0, Context context)
    {
        string baseDir = "";
        if (dirFd == 4)
        {
            baseDir = "/tmp/";
        }
        var span = context.Memory.AsSpan(path, pathlen);
        var path2 = System.Text.Encoding.UTF8.GetString(span);
        var x = fileStatFromString(context, baseDir + path2);
        Unsafe.As<byte, __wasi_filestat_t>(ref context.Memory[retptr0]) = x;
        return 0;
    }
    public static int path_filestat_set_times(int P_0, int P_1, int P_2, int P_3, long P_4, long P_5, int P_6, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    [Flags]
    public enum LookupFlags : uint
    {
        FollowSymlinks = 1,
    }

    [Flags]
    public enum OFlags : ushort
    {
        CREAT = 1,
        DIRECTORY = 2,
        EXCL = 4,
        TRUNC = 8,
    }
    [Flags]
    public enum FdFlags : ushort
    {
        APPEND = 1,
        DSYNC = 2,
        NONBLOCK = 4,
        RSYNC = 8,
        SYNC = 16
    }
    public static int path_open(int dirFd, LookupFlags dirFlags, int pathPtr, int pathlen,  OFlags o_flags, __wasi_rights_t fs_rights_base, __wasi_rights_t fs_rights_inheriting, FdFlags fdflags, int retptr0, Context context)
    {
        string baseDir = "";
        if (dirFd == 4)
        {
            baseDir = "/tmp/";
        }
        else
        {
            throw new Exception("??");
        }
        var pathMem= context.Memory.AsSpan(pathPtr);
        var end = pathMem.IndexOf((byte)0);
        var pa = System.Text.Encoding.UTF8.GetString(pathMem.Slice(0, end));
         
        {
            int fd = context.OpenFileOrDir(baseDir + pa);
            Unsafe.As<byte, int>(ref context.Memory[retptr0]) = fd;
            
            return 0;
        }

        return 1;

    }
    public static int path_readlink(int P_0, int P_1, int P_2, int P_3, int P_4, int P_5, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_remove_directory(int P_0, int P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_rename(int P_0, int P_1, int P_2, int P_3, int P_4, int P_5, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    
    public static int path_symlink(int P_0, int P_1, int P_2, int P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_unlink_file(int dirFd, int path, int pathlen, Context context)
    {
        var bytes =context.Memory.AsSpan().Slice(path, pathlen);
        var pa = System.Text.Encoding.UTF8.GetString(bytes);
        string baseDir = "";
        if (dirFd == 4)
        {
            baseDir = "/tmp/";
        }
        else
        {
            throw new Exception("??");
        }

        var fullPath = Path.Combine(baseDir, pa);
        File.Delete(fullPath);

        return 0;
    }

    public static void sqlite3_io_error_trap(Context context)
    {   
        
    }
    public static int poll_oneoff(int P_0, int P_1, int P_2, int P_3, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static void proc_exit(int P_0, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    
    
    public static int proc_raise(int P_0, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int sched_yield(Context context)
    {
        Thread.Yield();
        return 0;
    }

    public static int random_get(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int sock_recv(int P_0, int P_1, int P_2, int P_3, int P_4, int P_5, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int sock_send(int P_0, int P_1, int P_2, int P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }

    public static int sock_shutdown(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
}