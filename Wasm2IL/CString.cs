namespace Wasm2IL;

public ref struct CString
{
    private readonly int offset;
    private readonly Span<byte> heap;

    public CString(Span<byte> heap, int offset)
    {
        this.heap = heap;
        this.offset = offset;
    }

    unsafe public CString(byte* heap, int offset)
    {
        this.heap = new Span<byte>(heap + offset, 1024 * 1024);
        this.offset = 0;
    }

    public static CString New(byte[] heap, int offset)
    {
        return new CString(heap, offset);
    }

    public static unsafe CString New2(byte* heap, int offset)
    {
        return new CString(heap, offset);
    }

    public override string ToString()
    {
        var span = heap.Slice(offset, Length);
        return System.Text.Encoding.UTF8.GetString(span);
    }

    public int Length
    {
        get
        {
            int i = offset;
            for (;; i++)
            {
                var b = heap[i];
                if (b == 0)
                    return i - offset;
            }
        }
    }
}