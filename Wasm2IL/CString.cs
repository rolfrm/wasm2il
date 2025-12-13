namespace Wasm2IL;

/// <summary>
/// Wrapper for null-terminated UTF-8 strings in WASM heap memory.
/// </summary>
public ref struct CString
{
    readonly Span<byte> heap;
    readonly int offset;

    public CString(Span<byte> heap, int offset)
    {
        this.heap = heap;
        this.offset = offset;
    }

    public unsafe CString(byte* heap, int offset)
    {
        // Assume max 1MB string when using raw pointer (cannot determine actual bounds)
        this.heap = new Span<byte>(heap + offset, 1024 * 1024);
        this.offset = 0;
    }

    public static CString New(byte[] heap, int offset) => new(heap, offset);
    public static unsafe CString New2(byte* heap, int offset) => new(heap, offset);

    public int Length
    {
        get
        {
            for (int i = offset; ; i++)
                if (heap[i] == 0) return i - offset;
        }
    }

    public override string ToString() =>
        System.Text.Encoding.UTF8.GetString(heap.Slice(offset, Length));
}
