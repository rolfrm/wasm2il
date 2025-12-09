namespace Wasm2IL.Dwarf;

public class DwarfStringTable
{
    private readonly byte[] _data;
    private readonly Dictionary<uint, string> _cache;

    public DwarfStringTable(byte[] data)
    {
        _data = data;
        _cache = new Dictionary<uint, string>();
    }

    public string GetString(uint offset)
    {
        if (offset >= _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), 
                $"Offset {offset} is beyond string table size {_data.Length}");
        }

        // Check cache first
        if (_cache.TryGetValue(offset, out var cached))
        {
            return cached;
        }

        // Find the null terminator
        int endOffset = (int)offset;
        while (endOffset < _data.Length && _data[endOffset] != 0)
        {
            endOffset++;
        }

        if (endOffset >= _data.Length)
        {
            throw new InvalidDataException(
                $"String at offset {offset} is not null-terminated");
        }

        // Extract and decode the string
        int length = endOffset - (int)offset;
        var str = System.Text.Encoding.UTF8.GetString(_data, (int)offset, length);

        // Cache it
        _cache[offset] = str;

        return str;
    }

    public bool TryGetString(uint offset, out string result)
    {
        try
        {
            result = GetString(offset);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}