using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Removes NOP instructions that are not used as branch targets.
/// NOPs that ARE branch targets are preserved to maintain correct control flow.
/// </summary>
public class NopRemover : IOptimizationPass
{
    public bool Run(MethodBody body)
    {
        var instructions = body.Instructions;
        if (instructions.Count == 0)
            return false;

        // Collect all branch targets - these NOPs must be preserved
        var branchTargets = CollectBranchTargets(body);

        bool changed = false;
        var il = body.GetILProcessor();

        // Process in reverse to avoid index shifting issues
        for (int i = instructions.Count - 1; i >= 0; i--)
        {
            var instr = instructions[i];
            if (instr.OpCode != OpCodes.Nop)
                continue;

            // Don't remove NOPs that are branch targets
            if (branchTargets.Contains(instr))
                continue;

            // Don't remove the last instruction
            if (i == instructions.Count - 1)
                continue;

            il.Remove(instr);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Collect all instructions that are targets of branches or exception handlers.
    /// </summary>
    private static HashSet<Instruction> CollectBranchTargets(MethodBody body)
    {
        var targets = new HashSet<Instruction>();

        foreach (var instr in body.Instructions)
        {
            // Single branch target
            if (instr.Operand is Instruction target)
            {
                targets.Add(target);
            }
            // Switch instruction with multiple targets
            else if (instr.Operand is Instruction[] switchTargets)
            {
                foreach (var t in switchTargets)
                    targets.Add(t);
            }
        }

        // Exception handler boundaries are also "branch targets"
        foreach (var handler in body.ExceptionHandlers)
        {
            if (handler.TryStart != null) targets.Add(handler.TryStart);
            if (handler.TryEnd != null) targets.Add(handler.TryEnd);
            if (handler.HandlerStart != null) targets.Add(handler.HandlerStart);
            if (handler.HandlerEnd != null) targets.Add(handler.HandlerEnd);
            if (handler.FilterStart != null) targets.Add(handler.FilterStart);
        }

        return targets;
    }
}
