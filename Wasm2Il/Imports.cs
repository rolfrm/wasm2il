using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wasm2Il;

public class Imports
{
    public class Context
    {
        public Dictionary<int, FileStream> fds = new Dictionary<int, FileStream>();
        public byte[] Memory => (byte[])t.GetField("Memory", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private Type t;
        public Context(RuntimeTypeHandle rt)
        {
            t = Type.GetTypeFromHandle(rt);
        }

        public Stream GetFdStream(int fd)
        {
            if (fd == 1)
            {
                return Console.OpenStandardOutput();
            }

            if (fd == 2)
            {
                return Console.OpenStandardInput();
            }

            return fds[fd];
        }

        public int OpenFile(string pa)
        {
            var fst = File.Open(pa, FileMode.OpenOrCreate);
            for (int i = 3; i < 1000; i++)
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
    public static int fd_fdstat_get(int fd, int b, Context context)
    {
        return fd == 1 ? 1 : 0;
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
        throw new NotImplementedException("");
    }
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

        return written;
    }
    
    public static int environ_sizes_get(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
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

    public static int fd_filestat_get(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
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
    
    public static int fd_sync(int P_0, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int fd_tell(int P_0, int P_1, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_create_directory(int P_0, int P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_filestat_get(int P_0, int P_1, int P_2, int P_3, int P_4, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_filestat_set_times(int P_0, int P_1, int P_2, int P_3, long P_4, long P_5, int P_6, Context context)
    {
        throw new NotImplementedException("Not Implemented");
    }
    public static int path_open(int fd2, int dirFlags, int pathPtr, int o_flags, int fs_rights_base, long fs_rights_inheriting, long fdflags, int P_7, int retptr0, Context context)
    {
        var pathMem= context.Memory.AsSpan(pathPtr);
        var end = pathMem.IndexOf((byte)0);
        var pa = System.Text.Encoding.UTF8.GetString(pathMem.Slice(0, end));
        
        {
            int fd = context.OpenFile("/" + pa);
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
    public static int path_unlink_file(int P_0, int P_1, int P_2, Context context)
    {
        throw new NotImplementedException("Not Implemented");
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
        throw new NotImplementedException("Not Implemented");
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