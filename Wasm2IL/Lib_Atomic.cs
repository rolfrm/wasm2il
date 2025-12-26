using System.Runtime.CompilerServices;
using System.Threading;
using Wasm;

namespace Wasm2IL;

// All atomic/threading instructions are implemented here.
public partial class Lib
{
    // ==================== Atomic Fence ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.ATOMIC_FENCE)]
    public static void AtomicFence()
    {
        Thread.MemoryBarrier();
    }

    // ==================== Atomic Load Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_LOAD)]
    public static unsafe int I32AtomicLoad(int* addr)
    {
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD)]
    public static unsafe long I64AtomicLoad(long* addr)
    {
        return Interlocked.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_LOAD8_U)]
    public static unsafe int I32AtomicLoad8U(byte* addr)
    {
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_LOAD16_U)]
    public static unsafe int I32AtomicLoad16U(ushort* addr)
    {
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD8_U)]
    public static unsafe long I64AtomicLoad8U(byte* addr)
    {
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD16_U)]
    public static unsafe long I64AtomicLoad16U(ushort* addr)
    {
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD32_U)]
    public static unsafe long I64AtomicLoad32U(uint* addr)
    {
        return Volatile.Read(ref *addr);
    }

    // ==================== Atomic Store Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE)]
    public static unsafe void I32AtomicStore(int* addr, int value)
    {
        Volatile.Write(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE)]
    public static unsafe void I64AtomicStore(long* addr, long value)
    {
        Interlocked.Exchange(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE8)]
    public static unsafe void I32AtomicStore8(byte* addr, int value)
    {
        Volatile.Write(ref *addr, (byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE16)]
    public static unsafe void I32AtomicStore16(ushort* addr, int value)
    {
        Volatile.Write(ref *addr, (ushort)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE8)]
    public static unsafe void I64AtomicStore8(byte* addr, long value)
    {
        Volatile.Write(ref *addr, (byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE16)]
    public static unsafe void I64AtomicStore16(ushort* addr, long value)
    {
        Volatile.Write(ref *addr, (ushort)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE32)]
    public static unsafe void I64AtomicStore32(uint* addr, long value)
    {
        Volatile.Write(ref *addr, (uint)value);
    }

    // ==================== Atomic RMW Add Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_ADD)]
    public static unsafe int I32AtomicRmwAdd(int* addr, int value)
    {
        return Interlocked.Add(ref *addr, value) - value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_ADD)]
    public static unsafe long I64AtomicRmwAdd(long* addr, long value)
    {
        return Interlocked.Add(ref *addr, value) - value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_ADD_U)]
    public static unsafe int I32AtomicRmw8AddU(byte* addr, int value)
    {
        byte oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (byte)(oldVal + value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), newVal),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal))
            != ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReplaceByteInInt(int value, int byteIndex, byte newByte)
    {
        int shift = byteIndex * 8;
        return (value & ~(0xFF << shift)) | (newByte << shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_ADD_U)]
    public static unsafe int I32AtomicRmw16AddU(ushort* addr, int value)
    {
        ushort oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (ushort)(oldVal + value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, newVal),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal))
            != ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReplaceShortInInt(int value, int shortIndex, ushort newShort)
    {
        int shift = shortIndex * 16;
        return (value & ~(0xFFFF << shift)) | (newShort << shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_ADD_U)]
    public static unsafe long I64AtomicRmw8AddU(byte* addr, long value)
    {
        return I32AtomicRmw8AddU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_ADD_U)]
    public static unsafe long I64AtomicRmw16AddU(ushort* addr, long value)
    {
        return I32AtomicRmw16AddU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_ADD_U)]
    public static unsafe long I64AtomicRmw32AddU(uint* addr, long value)
    {
        return (uint)(Interlocked.Add(ref *(int*)addr, (int)value) - (int)value);
    }

    // ==================== Atomic RMW Sub Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_SUB)]
    public static unsafe int I32AtomicRmwSub(int* addr, int value)
    {
        return Interlocked.Add(ref *addr, -value) + value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_SUB)]
    public static unsafe long I64AtomicRmwSub(long* addr, long value)
    {
        return Interlocked.Add(ref *addr, -value) + value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_SUB_U)]
    public static unsafe int I32AtomicRmw8SubU(byte* addr, int value)
    {
        return I32AtomicRmw8AddU(addr, -value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_SUB_U)]
    public static unsafe int I32AtomicRmw16SubU(ushort* addr, int value)
    {
        return I32AtomicRmw16AddU(addr, -value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_SUB_U)]
    public static unsafe long I64AtomicRmw8SubU(byte* addr, long value)
    {
        return I32AtomicRmw8AddU(addr, -(int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_SUB_U)]
    public static unsafe long I64AtomicRmw16SubU(ushort* addr, long value)
    {
        return I32AtomicRmw16AddU(addr, -(int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_SUB_U)]
    public static unsafe long I64AtomicRmw32SubU(uint* addr, long value)
    {
        return (uint)(Interlocked.Add(ref *(int*)addr, -(int)value) + (int)value);
    }

    // ==================== Atomic RMW And Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_AND)]
    public static unsafe int I32AtomicRmwAnd(int* addr, int value)
    {
        return Interlocked.And(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_AND)]
    public static unsafe long I64AtomicRmwAnd(long* addr, long value)
    {
        return Interlocked.And(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_AND_U)]
    public static unsafe int I32AtomicRmw8AndU(byte* addr, int value)
    {
        byte oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (byte)(oldVal & value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), newVal),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal))
            != ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_AND_U)]
    public static unsafe int I32AtomicRmw16AndU(ushort* addr, int value)
    {
        ushort oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (ushort)(oldVal & value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, newVal),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal))
            != ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_AND_U)]
    public static unsafe long I64AtomicRmw8AndU(byte* addr, long value)
    {
        return I32AtomicRmw8AndU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_AND_U)]
    public static unsafe long I64AtomicRmw16AndU(ushort* addr, long value)
    {
        return I32AtomicRmw16AndU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_AND_U)]
    public static unsafe long I64AtomicRmw32AndU(uint* addr, long value)
    {
        return (uint)Interlocked.And(ref *(int*)addr, (int)value);
    }

    // ==================== Atomic RMW Or Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_OR)]
    public static unsafe int I32AtomicRmwOr(int* addr, int value)
    {
        return Interlocked.Or(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_OR)]
    public static unsafe long I64AtomicRmwOr(long* addr, long value)
    {
        return Interlocked.Or(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_OR_U)]
    public static unsafe int I32AtomicRmw8OrU(byte* addr, int value)
    {
        byte oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (byte)(oldVal | value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), newVal),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal))
            != ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_OR_U)]
    public static unsafe int I32AtomicRmw16OrU(ushort* addr, int value)
    {
        ushort oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (ushort)(oldVal | value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, newVal),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal))
            != ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_OR_U)]
    public static unsafe long I64AtomicRmw8OrU(byte* addr, long value)
    {
        return I32AtomicRmw8OrU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_OR_U)]
    public static unsafe long I64AtomicRmw16OrU(ushort* addr, long value)
    {
        return I32AtomicRmw16OrU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_OR_U)]
    public static unsafe long I64AtomicRmw32OrU(uint* addr, long value)
    {
        return (uint)Interlocked.Or(ref *(int*)addr, (int)value);
    }

    // ==================== Atomic RMW Xor Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_XOR)]
    public static unsafe int I32AtomicRmwXor(int* addr, int value)
    {
        int oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = oldVal ^ value;
        } while (Interlocked.CompareExchange(ref *addr, newVal, oldVal) != oldVal);
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_XOR)]
    public static unsafe long I64AtomicRmwXor(long* addr, long value)
    {
        long oldVal, newVal;
        do
        {
            oldVal = Interlocked.Read(ref *addr);
            newVal = oldVal ^ value;
        } while (Interlocked.CompareExchange(ref *addr, newVal, oldVal) != oldVal);
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_XOR_U)]
    public static unsafe int I32AtomicRmw8XorU(byte* addr, int value)
    {
        byte oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (byte)(oldVal ^ value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), newVal),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal))
            != ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_XOR_U)]
    public static unsafe int I32AtomicRmw16XorU(ushort* addr, int value)
    {
        ushort oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *addr);
            newVal = (ushort)(oldVal ^ value);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, newVal),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal))
            != ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_XOR_U)]
    public static unsafe long I64AtomicRmw8XorU(byte* addr, long value)
    {
        return I32AtomicRmw8XorU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_XOR_U)]
    public static unsafe long I64AtomicRmw16XorU(ushort* addr, long value)
    {
        return I32AtomicRmw16XorU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_XOR_U)]
    public static unsafe long I64AtomicRmw32XorU(uint* addr, long value)
    {
        int oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *(int*)addr);
            newVal = oldVal ^ (int)value;
        } while (Interlocked.CompareExchange(ref *(int*)addr, newVal, oldVal) != oldVal);
        return (uint)oldVal;
    }

    // ==================== Atomic RMW Exchange Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_XCHG)]
    public static unsafe int I32AtomicRmwXchg(int* addr, int value)
    {
        return Interlocked.Exchange(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_XCHG)]
    public static unsafe long I64AtomicRmwXchg(long* addr, long value)
    {
        return Interlocked.Exchange(ref *addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_XCHG_U)]
    public static unsafe int I32AtomicRmw8XchgU(byte* addr, int value)
    {
        byte oldVal;
        byte newVal = (byte)value;
        do
        {
            oldVal = Volatile.Read(ref *addr);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), newVal),
            ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal))
            != ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_XCHG_U)]
    public static unsafe int I32AtomicRmw16XchgU(ushort* addr, int value)
    {
        ushort oldVal;
        ushort newVal = (ushort)value;
        do
        {
            oldVal = Volatile.Read(ref *addr);
        } while (Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, newVal),
            ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal))
            != ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, oldVal));
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_XCHG_U)]
    public static unsafe long I64AtomicRmw8XchgU(byte* addr, long value)
    {
        return I32AtomicRmw8XchgU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_XCHG_U)]
    public static unsafe long I64AtomicRmw16XchgU(ushort* addr, long value)
    {
        return I32AtomicRmw16XchgU(addr, (int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_XCHG_U)]
    public static unsafe long I64AtomicRmw32XchgU(uint* addr, long value)
    {
        return (uint)Interlocked.Exchange(ref *(int*)addr, (int)value);
    }

    // ==================== Atomic RMW Compare-Exchange Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_CMPXCHG)]
    public static unsafe int I32AtomicRmwCmpxchg(int* addr, int expected, int replacement)
    {
        return Interlocked.CompareExchange(ref *addr, replacement, expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_CMPXCHG)]
    public static unsafe long I64AtomicRmwCmpxchg(long* addr, long expected, long replacement)
    {
        return Interlocked.CompareExchange(ref *addr, replacement, expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_CMPXCHG_U)]
    public static unsafe int I32AtomicRmw8CmpxchgU(byte* addr, int expected, int replacement)
    {
        byte oldVal = Volatile.Read(ref *addr);
        if (oldVal == (byte)expected)
        {
            Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
                ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), (byte)replacement),
                ReplaceByteInInt(*(int*)((nint)addr & ~3), (int)((nint)addr & 3), (byte)expected));
        }
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_CMPXCHG_U)]
    public static unsafe int I32AtomicRmw16CmpxchgU(ushort* addr, int expected, int replacement)
    {
        ushort oldVal = Volatile.Read(ref *addr);
        if (oldVal == (ushort)expected)
        {
            Interlocked.CompareExchange(ref *(int*)((nint)addr & ~3),
                ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, (ushort)replacement),
                ReplaceShortInInt(*(int*)((nint)addr & ~3), ((nint)addr & 2) != 0 ? 1 : 0, (ushort)expected));
        }
        return Volatile.Read(ref *addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw8CmpxchgU(byte* addr, long expected, long replacement)
    {
        return I32AtomicRmw8CmpxchgU(addr, (int)expected, (int)replacement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw16CmpxchgU(ushort* addr, long expected, long replacement)
    {
        return I32AtomicRmw16CmpxchgU(addr, (int)expected, (int)replacement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw32CmpxchgU(uint* addr, long expected, long replacement)
    {
        return (uint)Interlocked.CompareExchange(ref *(int*)addr, (int)replacement, (int)expected);
    }

    // ==================== Memory Atomic Wait/Notify Operations ====================
    // These are used for thread synchronization (futex-like operations)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_NOTIFY)]
    public static unsafe int MemoryAtomicNotify(int* addr, int count)
    {
        // Wake up 'count' threads waiting on this address
        // In .NET, we use Monitor for this
        // Returns the number of threads woken up
        // For single-threaded scenarios, this is effectively a no-op
        lock (GetWaitLock(addr))
        {
            if (count == int.MaxValue)
            {
                Monitor.PulseAll(GetWaitLock(addr));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Monitor.Pulse(GetWaitLock(addr));
                }
            }
        }
        // Return count as an approximation (actual count may differ)
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_WAIT32)]
    public static unsafe int MemoryAtomicWait32(int* addr, int expected, long timeout)
    {
        // Wait on address if value equals expected
        // Returns: 0 = woken by notify, 1 = value mismatch, 2 = timeout
        if (Volatile.Read(ref *addr) != expected)
        {
            return 1; // "not-equal"
        }

        lock (GetWaitLock(addr))
        {
            // Double-check after acquiring lock
            if (Volatile.Read(ref *addr) != expected)
            {
                return 1; // "not-equal"
            }

            bool signaled;
            if (timeout < 0)
            {
                Monitor.Wait(GetWaitLock(addr));
                signaled = true;
            }
            else
            {
                // Timeout is in nanoseconds, convert to milliseconds
                int timeoutMs = (int)(timeout / 1_000_000);
                if (timeoutMs == 0 && timeout > 0) timeoutMs = 1;
                signaled = Monitor.Wait(GetWaitLock(addr), timeoutMs);
            }

            return signaled ? 0 : 2; // 0 = "ok", 2 = "timed-out"
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_WAIT64)]
    public static unsafe int MemoryAtomicWait64(long* addr, long expected, long timeout)
    {
        // Wait on address if value equals expected
        // Returns: 0 = woken by notify, 1 = value mismatch, 2 = timeout
        if (Interlocked.Read(ref *addr) != expected)
        {
            return 1; // "not-equal"
        }

        lock (GetWaitLock(addr))
        {
            // Double-check after acquiring lock
            if (Interlocked.Read(ref *addr) != expected)
            {
                return 1; // "not-equal"
            }

            bool signaled;
            if (timeout < 0)
            {
                Monitor.Wait(GetWaitLock(addr));
                signaled = true;
            }
            else
            {
                // Timeout is in nanoseconds, convert to milliseconds
                int timeoutMs = (int)(timeout / 1_000_000);
                if (timeoutMs == 0 && timeout > 0) timeoutMs = 1;
                signaled = Monitor.Wait(GetWaitLock(addr), timeoutMs);
            }

            return signaled ? 0 : 2; // 0 = "ok", 2 = "timed-out"
        }
    }

    // Simple lock table for wait/notify operations
    // Using a fixed-size table with address hashing to avoid allocations
    private static readonly object[] WaitLocks = CreateWaitLocks();

    private static object[] CreateWaitLocks()
    {
        var locks = new object[256];
        for (int i = 0; i < locks.Length; i++)
        {
            locks[i] = new object();
        }
        return locks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object GetWaitLock(void* addr)
    {
        // Hash the address to get a lock index
        var index = ((nint)addr >> 2) & 0xFF;
        return WaitLocks[index];
    }
}
