using Mono.Cecil;

namespace Wasm2IL;

struct ImportFunc
{
    public string Name;
    public string Module;
    public uint Index;
    public uint? TypeId;
    public MethodReference? Method;
    public string CustomName;

    public override string ToString()
    {
        return $"import: {Name}";
    }
}