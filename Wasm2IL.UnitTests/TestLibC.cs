using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wasm2IL.UnitTests;

/// <summary>
/// Tests for LibC override functions (high-performance native implementations)
/// and LibC static functions used by WASM modules.
/// </summary>
[TestFixture]
public class TestLibC
{
    #region LibCOverride.memcpy Tests

    [Test]
    public unsafe void MemcpyBasic()
    {
        byte[] src = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] dst = new byte[8];

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.LibCOverride.memcpy(dstPtr, srcPtr, 8);
        }

        Assert.SequenceEqual(src, dst);
    }

    [Test]
    public unsafe void MemcpyZeroLength()
    {
        byte[] src = [1, 2, 3, 4];
        byte[] dst = [0, 0, 0, 0];

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.LibCOverride.memcpy(dstPtr, srcPtr, 0);
        }

        // dst should remain unchanged
        Assert.SequenceEqual(new byte[] { 0, 0, 0, 0 }, dst);
    }

    [Test]
    public unsafe void MemcpyLargeBuffer()
    {
        int size = 4096;
        byte[] src = new byte[size];
        byte[] dst = new byte[size];

        // Fill with pattern
        for (int i = 0; i < size; i++)
            src[i] = (byte)(i % 256);

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.LibCOverride.memcpy(dstPtr, srcPtr, size);
        }

        Assert.SequenceEqual(src, dst);
    }

    [Test]
    public unsafe void MemcpyPartialCopy()
    {
        byte[] src = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] dst = new byte[8];

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.LibCOverride.memcpy(dstPtr + 2, srcPtr + 1, 4);
        }

        Assert.SequenceEqual(new byte[] { 0, 0, 2, 3, 4, 5, 0, 0 }, dst);
    }

    #endregion

    #region LibCOverride.memfill Tests

    [Test]
    public unsafe void MemfillBasic()
    {
        byte[] buffer = new byte[8];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memfill(ptr, 0xAB, 8);
        }

        Assert.SequenceEqual(Enumerable.Repeat((byte)0xAB, 8), buffer);
    }

    [Test]
    public unsafe void MemfillZeroLength()
    {
        byte[] buffer = [1, 2, 3, 4];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memfill(ptr, 0xFF, 0);
        }

        Assert.SequenceEqual(new byte[] { 1, 2, 3, 4 }, buffer);
    }

    [Test]
    public unsafe void MemfillLargeBuffer()
    {
        int size = 4096;
        byte[] buffer = new byte[size];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memfill(ptr, 0x42, size);
        }

        Assert.IsTrue(buffer.All(b => b == 0x42));
    }

    [Test]
    public unsafe void MemfillValueTruncation()
    {
        // memfill should only use the low byte of the value
        byte[] buffer = new byte[4];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memfill(ptr, 0x1AB, 4); // 0xAB after truncation
        }

        Assert.SequenceEqual(Enumerable.Repeat((byte)0xAB, 4), buffer);
    }

    #endregion

    #region LibCOverride.memmove Tests

    [Test]
    public unsafe void MemmoveBasic()
    {
        byte[] src = [1, 2, 3, 4, 5];
        byte[] dst = new byte[5];

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.LibCOverride.memmove(dstPtr, srcPtr, 5);
        }

        Assert.SequenceEqual(src, dst);
    }

    [Test]
    public unsafe void MemmoveOverlappingForward()
    {
        // Test overlapping copy where dst > src
        byte[] buffer = [1, 2, 3, 4, 5, 0, 0, 0];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memmove(ptr + 3, ptr, 5);
        }

        Assert.SequenceEqual(new byte[] { 1, 2, 3, 1, 2, 3, 4, 5 }, buffer);
    }

    [Test]
    public unsafe void MemmoveOverlappingBackward()
    {
        // Test overlapping copy where dst < src
        byte[] buffer = [0, 0, 0, 1, 2, 3, 4, 5];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memmove(ptr, ptr + 3, 5);
        }

        Assert.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 3, 4, 5 }, buffer);
    }

    #endregion

    #region LibCOverride.memset Tests

    [Test]
    public unsafe void MemsetBasic()
    {
        byte[] buffer = new byte[8];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memset(ptr, 0xCD, 8);
        }

        Assert.SequenceEqual(Enumerable.Repeat((byte)0xCD, 8), buffer);
    }

    [Test]
    public unsafe void MemsetZeroLength()
    {
        byte[] buffer = [1, 2, 3, 4];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memset(ptr, 0xFF, 0);
        }

        Assert.SequenceEqual(new byte[] { 1, 2, 3, 4 }, buffer);
    }

    [Test]
    public unsafe void MemsetZeroValue()
    {
        byte[] buffer = [1, 2, 3, 4, 5, 6, 7, 8];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memset(ptr, 0, 8);
        }

        Assert.SequenceEqual(new byte[8], buffer);
    }

    [Test]
    public unsafe void MemsetLargeBuffer()
    {
        uint size = 8192;
        byte[] buffer = new byte[size];

        fixed (byte* ptr = buffer)
        {
            LibC.LibCOverride.memset(ptr, 0x55, size);
        }

        Assert.IsTrue(buffer.All(b => b == 0x55));
    }

    #endregion

    #region LibCOverride.memcmp Tests

    [Test]
    public unsafe void MemcmpEqual()
    {
        byte[] a = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] b = [1, 2, 3, 4, 5, 6, 7, 8];

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, 8);
        }

        Assert.AreEqual(0, result);
    }

    [Test]
    public unsafe void MemcmpFirstLess()
    {
        byte[] a = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] b = [1, 2, 3, 4, 5, 6, 7, 9];

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, 8);
        }

        Assert.Less(result, 0);
    }

    [Test]
    public unsafe void MemcmpFirstGreater()
    {
        byte[] a = [1, 2, 3, 4, 5, 6, 7, 9];
        byte[] b = [1, 2, 3, 4, 5, 6, 7, 8];

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, 8);
        }

        Assert.Greater(result, 0);
    }

    [Test]
    public unsafe void MemcmpZeroLength()
    {
        byte[] a = [1, 2, 3, 4];
        byte[] b = [5, 6, 7, 8];

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, 0);
        }

        Assert.AreEqual(0, result);
    }

    [Test]
    public unsafe void MemcmpLargeBuffer()
    {
        int size = 4096;
        byte[] a = new byte[size];
        byte[] b = new byte[size];

        for (int i = 0; i < size; i++)
        {
            a[i] = (byte)(i % 256);
            b[i] = (byte)(i % 256);
        }

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, size);
        }

        Assert.AreEqual(0, result);
    }

    [Test]
    public unsafe void MemcmpLargeBufferDifferentAtEnd()
    {
        int size = 4096;
        byte[] a = new byte[size];
        byte[] b = new byte[size];

        for (int i = 0; i < size; i++)
        {
            a[i] = (byte)(i % 256);
            b[i] = (byte)(i % 256);
        }
        // a[size-1] is 255, so adding 1 wraps to 0, making b < a
        // Instead, set explicit values to avoid wrap issues
        a[size - 1] = 100;
        b[size - 1] = 101;

        int result;
        fixed (byte* aPtr = a, bPtr = b)
        {
            result = LibC.LibCOverride.memcmp(aPtr, bPtr, size);
        }

        Assert.Less(result, 0);
    }

    #endregion

    #region LibC Static Function Tests

    [Test]
    public void GetPidReturnsValidPid()
    {
        int pid = LibC.getpid();
        int expected = Process.GetCurrentProcess().Id;
        Assert.AreEqual(expected, pid);
    }

    [Test]
    public void GetUidReturnsValue()
    {
        int uid = LibC.getuid();
        Assert.AreEqual(996, uid);
    }

    [Test]
    public void GetEuidReturnsValue()
    {
        int euid = LibC.geteuid();
        Assert.AreEqual(5, euid);
    }

    [Test]
    public void SysconfPageSize()
    {
        long pageSize = LibC.sysconf(SysConf.SC_PAGE_SIZE);
        Assert.AreEqual(1 << 16, pageSize);
    }

    [Test]
    public void SysconfUnimplementedThrows()
    {
        Assert.Throws<NotImplementedException>(() => LibC.sysconf(SysConf.SC_ARG_MAX));
    }

    [Test]
    public void AtexitReturnsZero()
    {
        int result = LibC.atexit(0);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void NanosleepReturnsZero()
    {
        int result = LibC.nanosleep(1, 0);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void FchmodReturnsZero()
    {
        int result = LibC.fchmod(0, FilePermissions.S_IRUSR);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void PutcharReturnsInput()
    {
        int result = LibC.putchar(65); // 'A'
        Assert.AreEqual(65, result);
    }

    [Test]
    public void FloatditfConvertsCorrectly()
    {
        double result = LibC.__floatditf(42);
        Assert.AreEqual(42.0, result);

        result = LibC.__floatditf(-100);
        Assert.AreEqual(-100.0, result);

        result = LibC.__floatditf(0);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region Slow LibC function tests (strlen, strnlen)

    [Test]
    public unsafe void StrnlenBasic()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello\0World");
        fixed (byte* ptr = data)
        {
            int len = LibC.strnlen(ptr, 100);
            Assert.AreEqual(5, len);
        }
    }

    [Test]
    public unsafe void StrnlenNoNull()
    {
        byte[] data = [1, 2, 3, 4, 5];
        fixed (byte* ptr = data)
        {
            int len = LibC.strnlen(ptr, 5);
            Assert.AreEqual(-1, len);
        }
    }

    [Test]
    public unsafe void StrnlenEmptyString()
    {
        byte[] data = [0, 1, 2, 3];
        fixed (byte* ptr = data)
        {
            int len = LibC.strnlen(ptr, 4);
            Assert.AreEqual(0, len);
        }
    }

    [Test]
    public unsafe void StrnlenMaxLen()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello World!\0");
        fixed (byte* ptr = data)
        {
            int len = LibC.strnlen(ptr, 5);
            Assert.AreEqual(-1, len); // No null within 5 bytes
        }
    }

    #endregion

    #region Slow memcpy (non-override) tests

    [Test]
    public unsafe void SlowMemcpyBasic()
    {
        byte[] src = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] dst = new byte[8];

        fixed (byte* srcPtr = src, dstPtr = dst)
        {
            LibC.memcpy(dstPtr, srcPtr, 8);
        }

        Assert.SequenceEqual(src, dst);
    }

    [Test]
    public unsafe void SlowMemfillBasic()
    {
        byte[] buffer = new byte[8];

        fixed (byte* ptr = buffer)
        {
            LibC.memfill(ptr, 0xAB, 8);
        }

        Assert.SequenceEqual(Enumerable.Repeat((byte)0xAB, 8), buffer);
    }

    #endregion
}

/// <summary>
/// Tests for LibC File I/O operations
/// </summary>
[TestFixture]
public class TestLibCFileIO
{
    string _testDir;
    string _testFile;

    public TestLibCFileIO()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "wasm2il_test_" + Guid.NewGuid().ToString("N"));
        _testFile = Path.Combine(_testDir, "test.txt");
    }

    void EnsureTestDir()
    {
        if (!Directory.Exists(_testDir))
            Directory.CreateDirectory(_testDir);
    }

    void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }
        catch { }
    }

    [Test]
    public void OpenCloseFile()
    {
        EnsureTestDir();
        File.WriteAllText(_testFile, "test content");

        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDWR, OpenMode.S_IRUSR);

        Assert.Greater(fd, 0);

        int closeResult = LibC.close(fd);
        Assert.AreEqual(0, closeResult);

        Cleanup();
    }

    [Test]
    public unsafe void ReadWriteFile()
    {
        EnsureTestDir();
        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDWR | OpenFlags.O_CREAT, OpenMode.S_IRUSR | OpenMode.S_IWUSR);

        Assert.Greater(fd, 0);

        // Write data
        byte[] writeData = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
        fixed (byte* writePtr = writeData)
        {
            int written = LibC.write(fd, writePtr, writeData.Length);
            Assert.AreEqual(writeData.Length, written);
        }

        // Seek back to start
        int newPos = LibC.lseek(fd, 0, SeekOrigin.SeekSet);
        Assert.AreEqual(0, newPos);

        // Read data back
        byte[] readBuffer = new byte[writeData.Length];
        fixed (byte* readPtr = readBuffer)
        {
            int bytesRead = LibC.read(fd, readPtr, readBuffer.Length);
            Assert.AreEqual(writeData.Length, bytesRead);
        }

        Assert.SequenceEqual(writeData, readBuffer);

        LibC.close(fd);
        Cleanup();
    }

    [Test]
    public void LseekOperations()
    {
        EnsureTestDir();
        File.WriteAllText(_testFile, "0123456789");

        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDWR, OpenMode.S_IRUSR);

        // Seek from beginning
        int pos = LibC.lseek(fd, 5, SeekOrigin.SeekSet);
        Assert.AreEqual(5, pos);

        // Seek from current
        pos = LibC.lseek(fd, 2, SeekOrigin.SeekCur);
        Assert.AreEqual(7, pos);

        // Seek from end
        pos = LibC.lseek(fd, -3, SeekOrigin.SeekEnd);
        Assert.AreEqual(7, pos);

        LibC.close(fd);
        Cleanup();
    }

    [Test]
    public void FtruncateFile()
    {
        EnsureTestDir();
        File.WriteAllText(_testFile, "0123456789");

        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDWR, OpenMode.S_IRUSR);

        int result = LibC.ftruncate(fd, 5);
        Assert.AreEqual(0, result);

        LibC.close(fd);

        string content = File.ReadAllText(_testFile);
        Assert.AreEqual("01234", content);

        Cleanup();
    }

    [Test]
    public void FsyncFile()
    {
        EnsureTestDir();
        File.WriteAllText(_testFile, "test");

        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDWR, OpenMode.S_IRUSR);

        int result = LibC.fsync(fd);
        Assert.AreEqual(0, result);

        LibC.close(fd);
        Cleanup();
    }

    [Test]
    public void UnlinkFile()
    {
        EnsureTestDir();
        File.WriteAllText(_testFile, "to be deleted");

        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testFile + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int result = LibC.unlink(cstr);

        Assert.AreEqual(0, result);
        Assert.IsFalse(File.Exists(_testFile));

        Cleanup();
    }

    [Test]
    public void OpenDirectory()
    {
        EnsureTestDir();
        var heap = new byte[1024];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(_testDir + "\0");
        Array.Copy(pathBytes, heap, pathBytes.Length);

        var cstr = new CString(heap, 0);
        int fd = LibC.open(default, cstr, OpenFlags.O_RDONLY | OpenFlags.O_DIRECTORY, 0);

        Assert.Greater(fd, 0);

        int closeResult = LibC.close(fd);
        Assert.AreEqual(0, closeResult);

        Cleanup();
    }
}

/// <summary>
/// Tests for error number handling
/// </summary>
[TestFixture]
public class TestErrno
{
    [Test]
    public void ErrnoValuesAreDefined()
    {
        Assert.AreEqual(1, (int)Errno.EPERM);
        Assert.AreEqual(2, (int)Errno.ENOENT);
        Assert.AreEqual(13, (int)Errno.EACCES);
        Assert.AreEqual(22, (int)Errno.EINVAL);
        Assert.AreEqual(28, (int)Errno.ENOSPC);
    }

    [Test]
    public void ErrnoAliasesAreCorrect()
    {
        Assert.AreEqual((int)Errno.EAGAIN, (int)Errno.EWOULDBLOCK);
        Assert.AreEqual((int)Errno.EDEADLK, (int)Errno.EDEADLOCK);
        Assert.AreEqual((int)Errno.EOPNOTSUPP, (int)Errno.ENOTSUP);
    }
}

/// <summary>
/// Tests for file permission and flag enums
/// </summary>
[TestFixture]
public class TestFileEnums
{
    [Test]
    public void FilePermissionsAreBinaryValues()
    {
        Assert.AreEqual(0b_000_100_000_000, (int)FilePermissions.S_IRUSR);
        Assert.AreEqual(0b_000_010_000_000, (int)FilePermissions.S_IWUSR);
        Assert.AreEqual(0b_000_001_000_000, (int)FilePermissions.S_IXUSR);
        Assert.AreEqual(0b_000_111_000_000, (int)FilePermissions.S_IRWXU);
    }

    [Test]
    public void OpenFlagsAreCorrect()
    {
        Assert.AreEqual(0, (int)OpenFlags.O_RDONLY);
        Assert.AreEqual(1, (int)OpenFlags.O_WRONLY);
        Assert.AreEqual(2, (int)OpenFlags.O_RDWR);
        Assert.AreEqual(64, (int)OpenFlags.O_CREAT);
    }

    [Test]
    public void SeekOriginValues()
    {
        Assert.AreEqual(0, (int)SeekOrigin.SeekSet);
        Assert.AreEqual(1, (int)SeekOrigin.SeekCur);
        Assert.AreEqual(2, (int)SeekOrigin.SeekEnd);
    }

    [Test]
    public void FcntlCommandValues()
    {
        Assert.AreEqual(5, (int)FcntlCommand.F_GETLK);
        Assert.AreEqual(6, (int)FcntlCommand.F_SETLK);
        Assert.AreEqual(7, (int)FcntlCommand.F_SETLKW);
    }
}

/// <summary>
/// Tests for Stat and Timespec structures
/// </summary>
[TestFixture]
public class TestStructures
{
    [Test]
    public void TimespecConversion()
    {
        var dt = new DateTime(2024, 1, 15, 12, 30, 45, 500, DateTimeKind.Utc);
        var ts = Timespec.ConvertDateTimeToTimespec(dt);

        Assert.Greater(ts.tv_sec, 0);
        Assert.AreEqual(500_000_000, ts.tv_nsec);
    }

    [Test]
    public void TimespecEpoch()
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ts = Timespec.ConvertDateTimeToTimespec(epoch);

        Assert.AreEqual(0, ts.tv_sec);
        Assert.AreEqual(0, ts.tv_nsec);
    }

    [Test]
    public void StatStructureSize()
    {
        // Ensure the Stat structure has the expected layout
        int size = Marshal.SizeOf<Stat>();
        // The structure should be reasonably sized for a 32-bit stat structure
        Assert.Greater(size, 0);
    }
}

/// <summary>
/// Tests for SysConf enum values
/// </summary>
[TestFixture]
public class TestSysConf
{
    [Test]
    public void SysConfPageSizeAliases()
    {
        Assert.AreEqual((int)SysConf.SC_PAGE_SIZE, (int)SysConf.SC_PAGESIZE);
    }

    [Test]
    public void SysConfIovMaxAliases()
    {
        Assert.AreEqual((int)SysConf.SC_UIO_MAXIOV, (int)SysConf.SC_IOV_MAX);
    }

    [Test]
    public void SysConfValuesAreInRange()
    {
        // Check that some key values are in expected ranges
        Assert.AreEqual(30, (int)SysConf.SC_PAGE_SIZE);
        Assert.AreEqual(84, (int)SysConf.SC_NPROCESSORS_ONLN);
    }
}
