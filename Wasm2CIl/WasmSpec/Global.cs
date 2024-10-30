using Mono.Cecil;

namespace Wasm2Cil;

class Global
{
    public bool Const;
    public byte Type;
    public object? Value;
    public FieldDefinition? Field;
}