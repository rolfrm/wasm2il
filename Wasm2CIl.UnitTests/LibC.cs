using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Wasm2Cil.UnitTests;

public class LibC
{
    public class LibCOverride
    {
        //FILE *f, const char *fmt, va_list *ap, union arg *nl_arg, int *nl_type)
        //public unsafe static int printf_core(int f, CString fmt, int param2, int param3, int param4)
        //{
        //    return 0;
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void * memcpy(void* dst, void* src, int c)
        {
            Buffer.MemoryCopy(src, dst, c, c);
            return dst;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void memfill(void * dst, int value, int c)
        {
            Span<byte> span = new Span<byte>((byte*)dst, c);

            // Fill the span with the byte value
            span.Fill((byte)(value & 0xFF));  // Clamp to byte range 0-255
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* memmove(void* dst, void* src, int c)
        {
            // Use Buffer.MemoryCopy, which safely handles overlapping memory regions
            Buffer.MemoryCopy(src, dst, c, c);

            // Return the number of bytes moved, similar to C-style memmove.
            return dst;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* memset(void* dst, byte value, uint count)
        {
            Unsafe.InitBlockUnaligned(dst, value, count);
            
            return dst;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int memcmp(void* ptr1, void* ptr2, int count)
        {
            byte* b1 = (byte*)ptr1;
            byte* b2 = (byte*)ptr2;

            int vectorSize = Vector<byte>.Count;
            int i = 0;

            // Compare using SIMD for larger blocks
            while (i <= count - vectorSize)
            {
                var v1 = Unsafe.Read<Vector<byte>>(b1 + i);
                var v2 = Unsafe.Read<Vector<byte>>(b2 + i);

                if (v1 != v2)
                {
                    // If the vectors differ, fall back to element-wise comparison
                    for (int j = 0; j < vectorSize; j++)
                    {
                        int diff = b1[i + j] - b2[i + j];
                        if (diff != 0) return diff;
                    }
                }

                i += vectorSize;
            }

            // Compare remaining bytes
            for (; i < count; i++)
            {
                int diff = b1[i] - b2[i];
                if (diff != 0) return diff;
            }

            return 0;
        }
        
    }
    private static byte[] x;
    public unsafe static void Test(Vector128<byte> vec)
    {
        fixed(byte * b = &x[4])
        {
            ((Vector128<byte>*)b)[1] = vec;    
        }
        fixed(byte * b = &x[4])
        {
            b[10] = 5;    
        }
        
    }
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

    class ModuleContext 
    {
        private readonly Type _ctx;
        
        private readonly MethodInfo malloc;
        private readonly MethodInfo free;
        private readonly FieldInfo memory;
        public Type Ctx => _ctx;
        public int ErrnoLocation { get; }
        public Span<int> ErrorNo => MemoryMarshal.Cast<byte, int>(GetSpan(ErrnoLocation, 4));

        public  ModuleContext(Type ctx)
        {
            _ctx = ctx;
            
            malloc = ctx.GetMethod("malloc");
            free = ctx.GetMethod("free");
            memory = ctx.GetField("Memory");
            memorySize = ctx.GetField("MemorySize");
            ErrnoLocation = Malloc(4);
        }
        
        private Dictionary<string, int> interned = new();
        private readonly FieldInfo memorySize;

        public int InternString(string str)
        {
            if (interned.TryGetValue(str, out var p))
                return p;
            var c = System.Text.Encoding.UTF8.GetByteCount(str);
            p = Malloc(c + 1);
                
            var writeSpan = GetSpan(p, c + 1);
            System.Text.Encoding.UTF8.GetBytes(str, writeSpan);
            writeSpan[c] = 0;
            interned[str] = p;
            return p;
        }

        public int Malloc(int count) => (int)malloc.Invoke(null, new object[]{count});

        public Span<byte> GetSpan(int p, int length) => GetHeap().Slice(p, length);
        public Span<T> GetSpan<T>(int p) where T: struct => MemoryMarshal.Cast<byte, T>(GetHeap().Slice(p, Marshal.SizeOf<T>()));
        public unsafe byte* GetHeapRaw() => (byte*) Pointer.Unbox(memory.GetValue(null));
        public unsafe int GetHeapSize() => (int)memorySize.GetValue(null);
        public unsafe Span<byte> GetHeap() => new Span<byte>((byte*)Pointer.Unbox(memory.GetValue(null)), (int)memorySize.GetValue(null));

        public unsafe void SetHeap(byte* setHeap, int size)
        {
            memory.SetValue(null, Pointer.Box(setHeap, typeof(byte*)));
            memorySize.SetValue(null, size);
        }


    }

    private static Dictionary<Type, ModuleContext> modCtx = new(); 

    static ModuleContext GetModuleContext(Type module)
    {
        if (!modCtx.TryGetValue(module, out var ctx))
        {
            ctx = new ModuleContext(module);
            modCtx[module] = ctx;
        }

        return ctx;
    }

    private static Dictionary<Type, Dictionary<string, int>>
        envLookup = new Dictionary<Type, Dictionary<string, int>>();
    public static int getenv(HeapContext heapCtx, CString name)
    {
        var ctx = GetModuleContext(heapCtx.Module);
        var name2 = name.ToString();
        if (!envLookup.TryGetValue(heapCtx.Module, out var lookup))
        {
            envLookup[heapCtx.Module] = lookup = new();
        }

        if (!lookup.TryGetValue(name2, out var p))
        {
            
            var env = Environment.GetEnvironmentVariable(name2);

            if (env != null)
            {
                var c = System.Text.Encoding.UTF8.GetByteCount(env);
                p = ctx.Malloc(c + 1);
                
                var writeSpan = ctx.GetSpan(p, c + 1);
                System.Text.Encoding.UTF8.GetBytes(env, writeSpan);
                writeSpan[c] = 0;
            }
            else
            {
                p = 0;
            }
            lookup[name2] = p;
            
        }
        
        return p;
    }

    public static long sysconf(SysConf name)
    {
        switch (name)
        {
            case SysConf.SC_PAGE_SIZE:
                return 1 << 16;
            default:
                throw new NotImplementedException("");
        }
        return 0;
    }

    
    public static unsafe int lstat(HeapContext heapCtx, CString pathname, Stat* statbuf)
    {
        return stat(heapCtx, pathname, statbuf);
    }

    public static int __errno_location(HeapContext heapCtx)
    {
        return GetModuleContext(heapCtx.Module).ErrnoLocation;
    }

    public static int strerror(HeapContext heapCtx, int err)
    {
        return GetModuleContext(heapCtx.Module).InternString("no error");
    } 


    public static int getcwd(HeapContext ctx, int buf, uint size)
    {
        var strBuf = GetModuleContext(ctx.Module).GetSpan(buf, (int)size);
        var e = System.Text.Encoding.UTF8.GetBytes(Directory.GetCurrentDirectory(), strBuf);
        strBuf[e] = 0;
        return buf;
    }

    public static int getpid()
    {
        return Process.GetCurrentProcess().Id;
    }

    private static  int _fd = 990;
    public static Dictionary<int, FileStream> files = new ();
    private static Dictionary<int, string> directories = new();
    public static int open(HeapContext ctx, CString path, OpenFlags flags, OpenMode mode)
    {
        var p = path.ToString();
        if (Directory.Exists(p))
        {
            var fd = _fd++;
            directories[fd] = p;
            return fd;    
        }
        else
        {
            var f = new FileStream(path.ToString(), FileMode.OpenOrCreate);
            var fd = _fd++;
            files[fd] = f;
            return fd;
        }
    }

    public static unsafe int fstat(HeapContext ctx, int fd, Stat* stat)
    {
        GetModuleContext(ctx.Module).ErrorNo[0] = 0;
        var finfo = new FileInfo(files[fd].Name);
        var r = _stat(finfo, stat, (ulong)files[fd].Length);
            
        return r;
    }

    static unsafe int _stat(FileInfo finfo, Stat* stat, ulong? length)
    {
        stat[0].st_size = length ?? ((ulong)finfo.Length);
        stat[0].st_blksize = 4096;
        stat[0].st_blocks = (int)Math.Ceiling((double)stat[0].st_size / 4096);
        stat[0].st_ino = (uint)(files.FirstOrDefault(f => f.Value.Name == finfo.Name).Key) + 1000;
        // 0x8000 regular file
        // 0x1a4: -rw-r--r--
        stat[0].st_mode = 0x8000 | 0x1a4; 
        stat[0].st_dev = 1;
        stat[0].st_rdev = 1;
        stat[0].st_uid = 1;
        stat[0].st_gid = 2;
        stat[0].st_nlink = 0;
        
        stat[0].st_atim = Timespec.ConvertDateTimeToTimespec(finfo.LastAccessTime);
        stat[0].st_mtim = Timespec.ConvertDateTimeToTimespec(finfo.LastWriteTime);
        stat[0].st_ctim = Timespec.ConvertDateTimeToTimespec(finfo.LastWriteTime);
        return 0;
    }
    
    
    public static unsafe int stat(HeapContext ctx, CString path, Stat* stat)
    {
        var finfo = new FileInfo(path.ToString());
        var x = GetModuleContext(ctx.Module);
        x.ErrorNo[0] = 0;
        if (!finfo.Exists)
        {
            x.ErrorNo[0] = (int)Errno.ENOENT;
            return -1;
        }

        return _stat(finfo, stat, null);
    }

    public static int lseek(int fd, int offset, SeekOrigin whence)
    {
        var f = files[fd];
        var newoffset = (int)f.Seek(offset, whence switch 
        {
            SeekOrigin.SeekCur => System.IO.SeekOrigin.Current,
            SeekOrigin.SeekEnd => System.IO.SeekOrigin.End,
            SeekOrigin.SeekSet => System.IO.SeekOrigin.Begin,

            _ => throw new ArgumentOutOfRangeException(nameof(whence), whence, null)
        });
        return newoffset;
    }

    public static unsafe int read(int fd, byte * buffer, int count)
    {
        var str = files[fd];
        var bufferSpan = new Span<byte>(buffer, count);
        int readBytes = str.Read(bufferSpan);
        return readBytes;
    } 
    
    public static unsafe int write(int fd, byte * buffer, int count)
    {
        var str = files[fd];
        var bufferSpan = new Span<byte>(buffer, count);
        str.Write(bufferSpan);
        return count;
    } 
    
    public unsafe static int sbrk(HeapContext _ctx, int increment)
    {
        var x = GetModuleContext(_ctx.Module);
        if (increment == 0)
        {
            // Return the current heap size (simulated program break)
            return x.GetHeap().Length;
        }

        if (increment > 0)
        {
            var r = x.GetHeapRaw();
            
            var lp = x.GetHeapSize();
            int newSize = lp + increment;

            r = Lib.Realloc(r, newSize);
            x.SetHeap(r, newSize);
            return lp;
        }
        return x.GetHeap().Length;
    }

    public static unsafe int fcntl(int fd, FcntlCommand cmd2, int * commandsPtr)
    {
        if (cmd2 == FcntlCommand.F_SETLK)
        {
            // Just ignore set lock.
        }else if (cmd2 == FcntlCommand.F_GETLK)
        {
            
        }
        else
        {
            throw new NotImplementedException("");
        }
        return 0;
    }

    public static int fsync(int fd)
    {
        if(files.TryGetValue(fd, out var f))
            f.Flush();
        return 0;
    }

    public static int close(int fd)
    {
        if (directories.TryGetValue(fd, out var _))
        {
            directories.Remove(fd);
            return 0;
        }
        files[fd].Close();
        files.Remove(fd);
        return 0;
    }

    public static int unlink(CString path)
    {
        File.Delete(path.ToString());
        return 0;
    }

    public static int fchmod(int fd, FilePermissions mode)
    {
        return 0;
    }

    public static int geteuid()
    {
        return 5;
    }

    public static int getuid()
    {
        return 996;
    }

    // int atexit(void (*func)(void));
    public static int atexit(int func)
    {

        return 0;
    }


    public static unsafe int fopen(HeapContext ctx, CString path, CString mode)
    {
        var x = GetModuleContext(ctx.Module);
        var p = x.Malloc(4);
        var idspan = x.GetSpan<int>(p);
        return 0;
    }

    public static int __lockfile(int filePtr)
    {
        return 1; // ok
    }

    public static void __unlockfile(int fileptr)
    {
        
    }

    public static int __towrite(int fptr)
    {
        return 0;
    }

    public static int putchar(int v)
    {
        return v;
    }

    public static void __errr(CString cstr)
    {
        Console.WriteLine(cstr.ToString());
    }

    public unsafe static int __fwritex(byte * data, int len, int file)
    {
        var span = new ReadOnlySpan<byte>(data, len);
        var str = System.Text.Encoding.UTF8.GetString(span);
        Console.Write(str);

        return 0;
    }
    public unsafe static int strnlen(byte * data, int len)
    {
        for (int i = 0; i < len; i++)
        {
            if (data[i] == 0)
                return i;
        }

        return -1;
    }
    
    
}

[Flags]
public enum FilePermissions
{
    S_ISUID = 0b_100_000_000_000, 
    S_ISGID = 0b_010_000_000_000,
    S_ISVTX = 0b_001_000_000_000,

    S_IRUSR = 0b_000_100_000_000,
    S_IWUSR = 0b_000_010_000_000,
    S_IXUSR = 0b_000_001_000_000,
    S_IRWXU = 0b_000_111_000_000, 

    S_IRGRP = 0b_000_000_100_000,
    S_IWGRP = 0b_000_000_010_000,
    S_IXGRP = 0b_000_000_001_000,
    S_IRWXG = 0b_000_000_111_000,

    S_IROTH = 0b_000_000_000_100,
    S_IWOTH = 0b_000_000_000_010,
    S_IXOTH = 0b_000_000_000_001,
    S_IRWXO = 0b_000_000_000_111
}

public enum SysConf : int
{
    SC_ARG_MAX = 0,
    SC_CHILD_MAX = 1,
    SC_CLK_TCK = 2,
    SC_NGROUPS_MAX = 3,
    SC_OPEN_MAX = 4,
    SC_STREAM_MAX = 5,
    SC_TZNAME_MAX = 6,
    SC_JOB_CONTROL = 7,
    SC_SAVED_IDS = 8,
    SC_REALTIME_SIGNALS = 9,
    SC_PRIORITY_SCHEDULING = 10,
    SC_TIMERS = 11,
    SC_ASYNCHRONOUS_IO = 12,
    SC_PRIORITIZED_IO = 13,
    SC_SYNCHRONIZED_IO = 14,
    SC_FSYNC = 15,
    SC_MAPPED_FILES = 16,
    SC_MEMLOCK = 17,
    SC_MEMLOCK_RANGE = 18,
    SC_MEMORY_PROTECTION = 19,
    SC_MESSAGE_PASSING = 20,
    SC_SEMAPHORES = 21,
    SC_SHARED_MEMORY_OBJECTS = 22,
    SC_AIO_LISTIO_MAX = 23,
    SC_AIO_MAX = 24,
    SC_AIO_PRIO_DELTA_MAX = 25,
    SC_DELAYTIMER_MAX = 26,
    SC_MQ_OPEN_MAX = 27,
    SC_MQ_PRIO_MAX = 28,
    SC_VERSION = 29,
    SC_PAGE_SIZE = 30,
    SC_PAGESIZE = 30, // Duplicate entry for aliasing
    SC_RTSIG_MAX = 31,
    SC_SEM_NSEMS_MAX = 32,
    SC_SEM_VALUE_MAX = 33,
    SC_SIGQUEUE_MAX = 34,
    SC_TIMER_MAX = 35,
    SC_BC_BASE_MAX = 36,
    SC_BC_DIM_MAX = 37,
    SC_BC_SCALE_MAX = 38,
    SC_BC_STRING_MAX = 39,
    SC_COLL_WEIGHTS_MAX = 40,
    SC_EXPR_NEST_MAX = 42,
    SC_LINE_MAX = 43,
    SC_RE_DUP_MAX = 44,
    SC_2_VERSION = 46,
    SC_2_C_BIND = 47,
    SC_2_C_DEV = 48,
    SC_2_FORT_DEV = 49,
    SC_2_FORT_RUN = 50,
    SC_2_SW_DEV = 51,
    SC_2_LOCALEDEF = 52,
    SC_UIO_MAXIOV = 60,
    SC_IOV_MAX = 60, // Duplicate entry for aliasing
    SC_THREADS = 67,
    SC_THREAD_SAFE_FUNCTIONS = 68,
    SC_GETGR_R_SIZE_MAX = 69,
    SC_GETPW_R_SIZE_MAX = 70,
    SC_LOGIN_NAME_MAX = 71,
    SC_TTY_NAME_MAX = 72,
    SC_THREAD_DESTRUCTOR_ITERATIONS = 73,
    SC_THREAD_KEYS_MAX = 74,
    SC_THREAD_STACK_MIN = 75,
    SC_THREAD_THREADS_MAX = 76,
    SC_THREAD_ATTR_STACKADDR = 77,
    SC_THREAD_ATTR_STACKSIZE = 78,
    SC_THREAD_PRIORITY_SCHEDULING = 79,
    SC_THREAD_PRIO_INHERIT = 80,
    SC_THREAD_PRIO_PROTECT = 81,
    SC_THREAD_PROCESS_SHARED = 82,
    SC_NPROCESSORS_CONF = 83,
    SC_NPROCESSORS_ONLN = 84,
    SC_PHYS_PAGES = 85,
    SC_AVPHYS_PAGES = 86,
    SC_ATEXIT_MAX = 87,
    SC_PASS_MAX = 88,
    SC_XOPEN_VERSION = 89,
    SC_XOPEN_XCU_VERSION = 90,
}

public enum Errno :int
{
    EPERM = 1,
    ENOENT = 2,
    ESRCH = 3,
    EINTR = 4,
    EIO = 5,
    ENXIO = 6,
    E2BIG = 7,
    ENOEXEC = 8,
    EBADF = 9,
    ECHILD = 10,
    EAGAIN = 11,
    ENOMEM = 12,
    EACCES = 13,
    EFAULT = 14,
    ENOTBLK = 15,
    EBUSY = 16,
    EEXIST = 17,
    EXDEV = 18,
    ENODEV = 19,
    ENOTDIR = 20,
    EISDIR = 21,
    EINVAL = 22,
    ENFILE = 23,
    EMFILE = 24,
    ENOTTY = 25,
    ETXTBSY = 26,
    EFBIG = 27,
    ENOSPC = 28,
    ESPIPE = 29,
    EROFS = 30,
    EMLINK = 31,
    EPIPE = 32,
    EDOM = 33,
    ERANGE = 34,
    EDEADLK = 35,
    ENAMETOOLONG = 36,
    ENOLCK = 37,
    ENOSYS = 38,
    ENOTEMPTY = 39,
    ELOOP = 40,
    EWOULDBLOCK = 11, // Same as EAGAIN
    ENOMSG = 42,
    EIDRM = 43,
    ECHRNG = 44,
    EL2NSYNC = 45,
    EL3HLT = 46,
    EL3RST = 47,
    ELNRNG = 48,
    EUNATCH = 49,
    ENOCSI = 50,
    EL2HLT = 51,
    EBADE = 52,
    EBADR = 53,
    EXFULL = 54,
    ENOANO = 55,
    EBADRQC = 56,
    EBADSLT = 57,
    EDEADLOCK = 35, // Same as EDEADLK
    EBFONT = 59,
    ENOSTR = 60,
    ENODATA = 61,
    ETIME = 62,
    ENOSR = 63,
    ENONET = 64,
    ENOPKG = 65,
    EREMOTE = 66,
    ENOLINK = 67,
    EADV = 68,
    ESRMNT = 69,
    ECOMM = 70,
    EPROTO = 71,
    EMULTIHOP = 72,
    EDOTDOT = 73,
    EBADMSG = 74,
    EOVERFLOW = 75,
    ENOTUNIQ = 76,
    EBADFD = 77,
    EREMCHG = 78,
    ELIBACC = 79,
    ELIBBAD = 80,
    ELIBSCN = 81,
    ELIBMAX = 82,
    ELIBEXEC = 83,
    EILSEQ = 84,
    ERESTART = 85,
    ESTRPIPE = 86,
    EUSERS = 87,
    ENOTSOCK = 88,
    EDESTADDRREQ = 89,
    EMSGSIZE = 90,
    EPROTOTYPE = 91,
    ENOPROTOOPT = 92,
    EPROTONOSUPPORT = 93,
    ESOCKTNOSUPPORT = 94,
    EOPNOTSUPP = 95,
    ENOTSUP = 95, // Same as EOPNOTSUPP
    EPFNOSUPPORT = 96,
    EAFNOSUPPORT = 97,
    EADDRINUSE = 98,
    EADDRNOTAVAIL = 99,
    ENETDOWN = 100,
    ENETUNREACH = 101,
    ENETRESET = 102,
    ECONNABORTED = 103,
    ECONNRESET = 104,
    ENOBUFS = 105,
    EISCONN = 106,
    ENOTCONN = 107,
    ESHUTDOWN = 108,
    ETOOMANYREFS = 109,
    ETIMEDOUT = 110,
    ECONNREFUSED = 111,
    EHOSTDOWN = 112,
    EHOSTUNREACH = 113,
    EALREADY = 114,
    EINPROGRESS = 115,
    ESTALE = 116,
    EUCLEAN = 117,
    ENOTNAM = 118,
    ENAVAIL = 119,
    EISNAM = 120,
    EREMOTEIO = 121,
    EDQUOT = 122,
    ENOMEDIUM = 123,
    EMEDIUMTYPE = 124,
    ECANCELED = 125,
    ENOKEY = 126,
    EKEYEXPIRED = 127,
    EKEYREVOKED = 128,
    EKEYREJECTED = 129,
    EOWNERDEAD = 130,
    ENOTRECOVERABLE = 131,
    ERFKILL = 132,
    EHWPOISON = 133
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Timespec
{
    public int tv_sec;   // long (32-bit)
    public int tv_nsec;  // long (32-bit)
    
    public static Timespec ConvertDateTimeToTimespec(DateTime dateTime)
    {
        // Get seconds since Unix epoch (January 1, 1970)
        int seconds = (int)(dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        // Get nanoseconds
        int nanoseconds = dateTime.Millisecond * 1_000_000;

        return new Timespec
        {
            tv_sec = seconds,
            tv_nsec = nanoseconds
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Stat
{
    public uint st_dev;           // dev_t (32-bit)
    public uint st_ino;           // ino_t (32-bit)
    public uint st_nlink;         // nlink_t (32-bit)

    public uint st_mode;          // mode_t (32-bit)
    public uint st_uid;           // uid_t (32-bit)
    public uint st_gid;           // gid_t (32-bit)
    public uint __pad0;           // unsigned int padding (32-bit)

    public uint st_rdev;          // dev_t (32-bit)
    public ulong st_size;           // off_t (32-bit)

    public int st_blksize;        // blksize_t (32-bit)
    public int st_blocks;         // blkcnt_t (32-bit)

    // struct timespec (32-bit system: 8 bytes each)
    public Timespec st_atim;      // Access time
    public Timespec st_mtim;      // Modification time
    public Timespec st_ctim;      // Change time

    
    public long __unused1;
    public long __unused2;
    public long __unused3;
}
[Flags]
public enum OpenFlags : int
{
    O_RDONLY = 0,
    O_WRONLY=1,
    O_RDWR= 2,
    O_CREAT = 64,
    O_EXCL = 128,
    O_NOCTTY = 256,
    O_TRUNC = 512,
    O_APPEND = 1024,
    O_NONBLOCK = 2048,
    O_DSYNC = 4096,
    O_SYNC = 1052672,
    O_RSYNC = 1052672,
    O_DIRECTORY = 65536,
    O_NOFOLLOW = 131072,
    O_CLOEXEC = 0x1000000,
    O_ASYNC = 0x10000,
    O_DIRECT = 0x20000,
    O_LARGEFILE = 303240,
    O_NOATIME = 0x200000,
    O_PATH = 0x4000000,
    //O_TMPFILE = 0x80200000,
    O_NDELAY = O_NONBLOCK
}
/*
[Flags]
public enum OpenFlags : int
{
    // File access modes
    O_RDONLY = 0x0000,  // Open for reading only
    O_WRONLY = 0x0001,  // Open for writing only
    O_RDWR   = 0x0002,  // Open for reading and writing

    // File creation flags
    O_CREAT  = 0x0040,  // Create file if it does not exist
    O_EXCL   = 0x0080,  // Ensure that file is created (fails if exists)
    O_TRUNC  = 0x0100,  // Truncate file to zero length if it exists
    O_APPEND = 0x0200,  // Open file in append mode

    // File behavior flags
    O_NONBLOCK = 0x0400, // Non-blocking mode
    O_SYNC     = 0x0800, // Synchronized I/O
    O_DIRECTORY = 0x1000, // File must be a directory (otherwise error)

    // Mask to isolate file access modes
    O_ACCMODE = O_RDONLY | O_WRONLY | O_RDWR
}*/

[Flags]
public enum OpenMode : int
{
    // User permissions
    S_IRUSR = 0x0100,  // Read permission for owner
    S_IWUSR = 0x0080,  // Write permission for owner
    S_IXUSR = 0x0040,  // Execute/search permission for owner

    // Group permissions
    S_IRGRP = 0x0020,  // Read permission for group
    S_IWGRP = 0x0010,  // Write permission for group
    S_IXGRP = 0x0008,  // Execute/search permission for group

    // Other (world) permissions
    S_IROTH = 0x0004,  // Read permission for others
    S_IWOTH = 0x0002,  // Write permission for others
    S_IXOTH = 0x0001,  // Execute/search permission for others

    // Special modes
    S_ISUID = 0x0800,  // Set-user-ID on execution
    S_ISGID = 0x0400,  // Set-group-ID on execution
    S_ISVTX = 0x0200,  // Save swapped text after use (sticky bit)

    // Full permissions (read, write, execute for all)
    S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,  // User permissions
    S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,  // Group permissions
    S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH   // Others permissions
}

public enum FcntlCommand
{
    F_DUPFD = 0,         // Duplicate file descriptor
    F_GETFD = 1,         // Get file descriptor flags
    F_SETFD = 2,         // Set file descriptor flags
    F_GETFL = 3,         // Get file status flags
    F_SETFL = 4,         // Set file status flags

    F_GETLK = 5,         // Get record locking information
    F_SETLK = 6,         // Set record locking (non-blocking)
    F_SETLKW = 7,        // Set record locking and wait

    F_SETOWN = 8,        // Set the owner of a socket
    F_GETOWN = 9,        // Get the owner of a socket
    F_SETSIG = 10,       // Set the signal to be sent
    F_GETSIG = 11,       // Get the signal to be sent

    F_SETOWN_EX = 15,    // Set extended owner information
    F_GETOWN_EX = 16,    // Get extended owner information

    F_GETOWNER_UIDS = 17 // Get owner UIDs (user IDs)
}

public enum SeekOrigin : int
{
    // Set the offset to an absolute position from the beginning of the file
    SeekSet = 0,  // SEEK_SET

    // Set the offset relative to the current position in the file
    SeekCur = 1,  // SEEK_CUR

    // Set the offset relative to the end of the file
    SeekEnd = 2   // SEEK_END
}