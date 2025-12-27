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
    public static unsafe int I32AtomicLoad(int* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD)]
    public static unsafe long I64AtomicLoad(long* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_LOAD8_U)]
    public static unsafe int I32AtomicLoad8U(byte* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_LOAD16_U)]
    public static unsafe int I32AtomicLoad16U(ushort* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD8_U)]
    public static unsafe long I64AtomicLoad8U(byte* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD16_U)]
    public static unsafe long I64AtomicLoad16U(ushort* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_LOAD32_U)]
    public static unsafe long I64AtomicLoad32U(uint* addr, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Volatile.Read(ref *(addr + offset));
    }

    // ==================== Atomic Store Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE)]
    public static unsafe void I32AtomicStore(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE)]
    public static unsafe void I64AtomicStore(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Interlocked.Exchange(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE8)]
    public static unsafe void I32AtomicStore8(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), (byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_STORE16)]
    public static unsafe void I32AtomicStore16(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), (ushort)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE8)]
    public static unsafe void I64AtomicStore8(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), (byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE16)]
    public static unsafe void I64AtomicStore16(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), (ushort)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_STORE32)]
    public static unsafe void I64AtomicStore32(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        Volatile.Write(ref *(addr + offset), (uint)value);
    }

    // ==================== Atomic RMW Add Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_ADD)]
    public static unsafe int I32AtomicRmwAdd(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Add(ref *(addr + offset), value) - value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_ADD)]
    public static unsafe long I64AtomicRmwAdd(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Add(ref *(addr + offset), value) - value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_ADD_U)]
    public static unsafe int I32AtomicRmw8AddU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldByte = (oldWord >> shift) & 0xFF;
            int newByte = (oldByte + value) & 0xFF;
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_ADD_U)]
    public static unsafe int I32AtomicRmw16AddU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldShort = (oldWord >> shift) & 0xFFFF;
            int newShort = (oldShort + value) & 0xFFFF;
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_ADD_U)]
    public static unsafe long I64AtomicRmw8AddU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8AddU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_ADD_U)]
    public static unsafe long I64AtomicRmw16AddU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16AddU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_ADD_U)]
    public static unsafe long I64AtomicRmw32AddU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)(Interlocked.Add(ref *(int*)(addr + offset), (int)value) - (int)value);
    }

    // ==================== Atomic RMW Sub Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_SUB)]
    public static unsafe int I32AtomicRmwSub(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Add(ref *(addr + offset), -value) + value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_SUB)]
    public static unsafe long I64AtomicRmwSub(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Add(ref *(addr + offset), -value) + value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_SUB_U)]
    public static unsafe int I32AtomicRmw8SubU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldByte = (oldWord >> shift) & 0xFF;
            int newByte = (oldByte - value) & 0xFF;
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_SUB_U)]
    public static unsafe int I32AtomicRmw16SubU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldShort = (oldWord >> shift) & 0xFFFF;
            int newShort = (oldShort - value) & 0xFFFF;
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_SUB_U)]
    public static unsafe long I64AtomicRmw8SubU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8SubU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_SUB_U)]
    public static unsafe long I64AtomicRmw16SubU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16SubU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_SUB_U)]
    public static unsafe long I64AtomicRmw32SubU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)(Interlocked.Add(ref *(int*)(addr + offset), -(int)value) + (int)value);
    }

    // ==================== Atomic RMW And Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_AND)]
    public static unsafe int I32AtomicRmwAnd(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.And(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_AND)]
    public static unsafe long I64AtomicRmwAnd(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.And(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_AND_U)]
    public static unsafe int I32AtomicRmw8AndU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldByte = (oldWord >> shift) & 0xFF;
            int newByte = oldByte & value;
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_AND_U)]
    public static unsafe int I32AtomicRmw16AndU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldShort = (oldWord >> shift) & 0xFFFF;
            int newShort = oldShort & value;
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_AND_U)]
    public static unsafe long I64AtomicRmw8AndU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8AndU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_AND_U)]
    public static unsafe long I64AtomicRmw16AndU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16AndU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_AND_U)]
    public static unsafe long I64AtomicRmw32AndU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)Interlocked.And(ref *(int*)(addr + offset), (int)value);
    }

    // ==================== Atomic RMW Or Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_OR)]
    public static unsafe int I32AtomicRmwOr(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Or(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_OR)]
    public static unsafe long I64AtomicRmwOr(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Or(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_OR_U)]
    public static unsafe int I32AtomicRmw8OrU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldByte = (oldWord >> shift) & 0xFF;
            int newByte = oldByte | value;
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_OR_U)]
    public static unsafe int I32AtomicRmw16OrU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldShort = (oldWord >> shift) & 0xFFFF;
            int newShort = oldShort | value;
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_OR_U)]
    public static unsafe long I64AtomicRmw8OrU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8OrU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_OR_U)]
    public static unsafe long I64AtomicRmw16OrU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16OrU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_OR_U)]
    public static unsafe long I64AtomicRmw32OrU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)Interlocked.Or(ref *(int*)(addr + offset), (int)value);
    }

    // ==================== Atomic RMW Xor Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_XOR)]
    public static unsafe int I32AtomicRmwXor(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *ptr);
            newVal = oldVal ^ value;
        } while (Interlocked.CompareExchange(ref *ptr, newVal, oldVal) != oldVal);
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_XOR)]
    public static unsafe long I64AtomicRmwXor(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        long oldVal, newVal;
        do
        {
            oldVal = Interlocked.Read(ref *ptr);
            newVal = oldVal ^ value;
        } while (Interlocked.CompareExchange(ref *ptr, newVal, oldVal) != oldVal);
        return oldVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_XOR_U)]
    public static unsafe int I32AtomicRmw8XorU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldByte = (oldWord >> shift) & 0xFF;
            int newByte = oldByte ^ value;
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_XOR_U)]
    public static unsafe int I32AtomicRmw16XorU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int oldShort = (oldWord >> shift) & 0xFFFF;
            int newShort = oldShort ^ value;
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_XOR_U)]
    public static unsafe long I64AtomicRmw8XorU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8XorU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_XOR_U)]
    public static unsafe long I64AtomicRmw16XorU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16XorU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_XOR_U)]
    public static unsafe long I64AtomicRmw32XorU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = (int*)(addr + offset);
        int oldVal, newVal;
        do
        {
            oldVal = Volatile.Read(ref *ptr);
            newVal = oldVal ^ (int)value;
        } while (Interlocked.CompareExchange(ref *ptr, newVal, oldVal) != oldVal);
        return (uint)oldVal;
    }

    // ==================== Atomic RMW Exchange Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_XCHG)]
    public static unsafe int I32AtomicRmwXchg(int* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Exchange(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_XCHG)]
    public static unsafe long I64AtomicRmwXchg(long* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.Exchange(ref *(addr + offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_XCHG_U)]
    public static unsafe int I32AtomicRmw8XchgU(byte* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int newByte = value & 0xFF;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            newWord = (oldWord & ~mask) | (newByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_XCHG_U)]
    public static unsafe int I32AtomicRmw16XchgU(ushort* addr, int value, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int newShort = value & 0xFFFF;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            newWord = (oldWord & ~mask) | (newShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return (oldWord >> shift) & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_XCHG_U)]
    public static unsafe long I64AtomicRmw8XchgU(byte* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8XchgU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_XCHG_U)]
    public static unsafe long I64AtomicRmw16XchgU(ushort* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16XchgU(addr, (int)value, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_XCHG_U)]
    public static unsafe long I64AtomicRmw32XchgU(uint* addr, long value, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)Interlocked.Exchange(ref *(int*)(addr + offset), (int)value);
    }

    // ==================== Atomic RMW Compare-Exchange Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW_CMPXCHG)]
    public static unsafe int I32AtomicRmwCmpxchg(int* addr, int expected, int replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.CompareExchange(ref *(addr + offset), replacement, expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW_CMPXCHG)]
    public static unsafe long I64AtomicRmwCmpxchg(long* addr, long expected, long replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        return Interlocked.CompareExchange(ref *(addr + offset), replacement, expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW8_CMPXCHG_U)]
    public static unsafe int I32AtomicRmw8CmpxchgU(byte* addr, int expected, int replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 3) * 8);
        int mask = 0xFF << shift;
        int expectedByte = expected & 0xFF;
        int replacementByte = replacement & 0xFF;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int currentByte = (oldWord >> shift) & 0xFF;
            if (currentByte != expectedByte)
                return currentByte;
            newWord = (oldWord & ~mask) | (replacementByte << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return expectedByte;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I32_ATOMIC_RMW16_CMPXCHG_U)]
    public static unsafe int I32AtomicRmw16CmpxchgU(ushort* addr, int expected, int replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        int* alignedAddr = (int*)((nint)ptr & ~3);
        int shift = (int)(((nint)ptr & 2) * 8);
        int mask = 0xFFFF << shift;
        int expectedShort = expected & 0xFFFF;
        int replacementShort = replacement & 0xFFFF;
        int oldWord, newWord;
        do
        {
            oldWord = Volatile.Read(ref *alignedAddr);
            int currentShort = (oldWord >> shift) & 0xFFFF;
            if (currentShort != expectedShort)
                return currentShort;
            newWord = (oldWord & ~mask) | (replacementShort << shift);
        } while (Interlocked.CompareExchange(ref *alignedAddr, newWord, oldWord) != oldWord);
        return expectedShort;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW8_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw8CmpxchgU(byte* addr, long expected, long replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw8CmpxchgU(addr, (int)expected, (int)replacement, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW16_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw16CmpxchgU(ushort* addr, long expected, long replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        return I32AtomicRmw16CmpxchgU(addr, (int)expected, (int)replacement, align, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.I64_ATOMIC_RMW32_CMPXCHG_U)]
    public static unsafe long I64AtomicRmw32CmpxchgU(uint* addr, long expected, long replacement, [WasmConst] byte align, [WasmConst] int offset)
    {
        return (uint)Interlocked.CompareExchange(ref *(int*)(addr + offset), (int)replacement, (int)expected);
    }

    // ==================== Memory Atomic Wait/Notify Operations ====================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_NOTIFY)]
    public static unsafe int MemoryAtomicNotify(int* addr, int count, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        lock (GetWaitLock(ptr))
        {
            if (count == int.MaxValue)
            {
                Monitor.PulseAll(GetWaitLock(ptr));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Monitor.Pulse(GetWaitLock(ptr));
                }
            }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_WAIT32)]
    public static unsafe int MemoryAtomicWait32(int* addr, int expected, long timeout, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        if (Volatile.Read(ref *ptr) != expected)
            return 1;

        lock (GetWaitLock(ptr))
        {
            if (Volatile.Read(ref *ptr) != expected)
                return 1;

            bool signaled;
            if (timeout < 0)
            {
                Monitor.Wait(GetWaitLock(ptr));
                signaled = true;
            }
            else
            {
                int timeoutMs = (int)(timeout / 1_000_000);
                if (timeoutMs == 0 && timeout > 0) timeoutMs = 1;
                signaled = Monitor.Wait(GetWaitLock(ptr), timeoutMs);
            }
            return signaled ? 0 : 2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [WasmOpcode(AtomicInstruction.MEMORY_ATOMIC_WAIT64)]
    public static unsafe int MemoryAtomicWait64(long* addr, long expected, long timeout, [WasmConst] byte align, [WasmConst] int offset)
    {
        var ptr = addr + offset;
        if (Interlocked.Read(ref *ptr) != expected)
            return 1;

        lock (GetWaitLock(ptr))
        {
            if (Interlocked.Read(ref *ptr) != expected)
                return 1;

            bool signaled;
            if (timeout < 0)
            {
                Monitor.Wait(GetWaitLock(ptr));
                signaled = true;
            }
            else
            {
                int timeoutMs = (int)(timeout / 1_000_000);
                if (timeoutMs == 0 && timeout > 0) timeoutMs = 1;
                signaled = Monitor.Wait(GetWaitLock(ptr), timeoutMs);
            }
            return signaled ? 0 : 2;
        }
    }

    private static readonly object[] WaitLocks = CreateWaitLocks();

    private static object[] CreateWaitLocks()
    {
        var locks = new object[256];
        for (int i = 0; i < locks.Length; i++)
            locks[i] = new object();
        return locks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object GetWaitLock(void* addr)
    {
        var index = ((nint)addr >> 2) & 0xFF;
        return WaitLocks[index];
    }
}
