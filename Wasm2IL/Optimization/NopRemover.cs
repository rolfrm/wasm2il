using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Removes NOP instructions that are not used as branch targets.
/// NOPs that are branch targets have their branches redirected to the next instruction before removal.
/// </summary>
public class NopRemover : IOptimizationPass
{
    public bool Run(MethodBody body)
    {
        var instructions = body.Instructions;
        if (instructions.Count == 0)
            return false;

        // Collect all branch targets
        var branchTargets = CollectBranchTargets(body);

        bool changed = false;
        var il = body.GetILProcessor();

        // First pass: redirect branches from NOP targets to next non-NOP instruction
        // We do this separately to ensure all redirects happen before any removals
        foreach (var instr in instructions.ToList())
        {
            if (instr.OpCode != OpCodes.Nop)
                continue;

            if (!branchTargets.Contains(instr))
                continue;

            // Find the next non-nop instruction
            int index = instructions.IndexOf(instr);
            Instruction? nextInstr = FindNextNonNop(instructions, index);

            if (nextInstr != null)
            {
                RedirectBranches(body, instr, nextInstr);
            }
        }

        // Rebuild branch targets after redirects
        branchTargets = CollectBranchTargets(body);

        // Second pass: remove NOPs that are no longer branch targets
        // Process in reverse to avoid index shifting issues
        for (int i = instructions.Count - 1; i >= 0; i--)
        {
            var instr = instructions[i];
            if (instr.OpCode != OpCodes.Nop)
                continue;

            // Don't remove NOPs that are still branch targets
            if (branchTargets.Contains(instr))
                continue;

            // Find the next non-nop instruction
            Instruction? nextInstr = FindNextNonNop(instructions, i);

            // If this NOP is the last instruction (or only NOPs after), keep it
            if (nextInstr == null)
                continue;

            il.Remove(instr);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Find the next instruction that is not a NOP.
    /// </summary>
    private static Instruction? FindNextNonNop(Mono.Collections.Generic.Collection<Instruction> instructions, int currentIndex)
    {
        for (int i = currentIndex + 1; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode != OpCodes.Nop)
                return instructions[i];
        }
        return null;
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

    /// <summary>
    /// Redirect all branches from oldTarget to newTarget.
    /// </summary>
    private static void RedirectBranches(MethodBody body, Instruction oldTarget, Instruction newTarget)
    {
        foreach (var instr in body.Instructions)
        {
            if (instr.Operand == oldTarget)
            {
                instr.Operand = newTarget;
            }
            else if (instr.Operand is Instruction[] targets)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == oldTarget)
                        targets[i] = newTarget;
                }
            }
        }

        foreach (var handler in body.ExceptionHandlers)
        {
            if (handler.TryStart == oldTarget) handler.TryStart = newTarget;
            if (handler.TryEnd == oldTarget) handler.TryEnd = newTarget;
            if (handler.HandlerStart == oldTarget) handler.HandlerStart = newTarget;
            if (handler.HandlerEnd == oldTarget) handler.HandlerEnd = newTarget;
            if (handler.FilterStart == oldTarget) handler.FilterStart = newTarget;
        }
    }
}
