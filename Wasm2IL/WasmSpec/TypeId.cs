using Mono.Cecil;

namespace Wasm2Cil;

struct TypeId
{
    public uint ParamCount;
    public uint ReturnCount;
    public TypeReference[] ParamTypes;
    public TypeReference ReturnType;
}