using Mono.Cecil;

namespace Wasm2Il;

public class FuncDeclType
{
    public MethodDefinition? Method;
    public uint TypeId;
    public bool IsDefaultName;
    public string? ImportName { get; set; }
    public override string ToString() => Method?.Name ?? "Func?";
}