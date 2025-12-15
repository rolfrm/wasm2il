using Mono.Cecil;

namespace Wasm2IL;

internal struct TypeId
{
    public uint ParamCount;
    public uint ReturnCount;
    public TypeReference[] ParamTypes;
    public TypeReference ReturnType;
}