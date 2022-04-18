using Mono.Cecil;

namespace Wasm2Il;

struct TypeId
{
    public uint ParamCount;
    public uint ReturnCount;
    public TypeReference[] ParamTypes;
    public TypeReference ReturnType;
}