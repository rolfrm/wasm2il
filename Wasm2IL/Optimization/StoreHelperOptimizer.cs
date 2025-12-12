using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Optimizes the store-helper pattern generated for WASM memory stores.
///
/// The pattern (from WASM store translation):
///   [address base]   ; e.g., ldarg.1
///   [value]          ; simple load like ldarg.2, ldloc.X, or ldc.i4.X
///   stloc helper     ; save value to helper variable
///   [address calc]   ; ldloc memory, add, ldc offset, add
///   ldloc helper     ; reload value
///   stind.XX         ; store
///
/// Optimized to:
///   [address base]
///   [address calc]   ; compute full memory address
///   [value]          ; emit value load here instead
///   stind.XX         ; store
///
/// This saves 2 instructions (stloc + ldloc) per memory store when the value
/// is a simple expression that can be safely re-emitted.
/// </summary>
public class StoreHelperOptimizer : IOptimizationPass
{
    public bool Run(MethodBody body)
    {
        var instructions = body.Instructions;
        if (instructions.Count < 5)
            return false;

        bool changed = false;
        var il = body.GetILProcessor();

        // Process forward, looking for the pattern
        int i = 0;
        while (i < instructions.Count - 4)
        {
            // Look for: [simple load], stloc X
            var valueInstr = instructions[i];
            var stlocInstr = instructions[i + 1];

            // Check if it's a simple load followed by stloc
            if (!IsSimpleLoad(valueInstr) || !IsStloc(stlocInstr))
            {
                i++;
                continue;
            }

            int helperIndex = GetLocalIndex(stlocInstr);
            if (helperIndex == -1)
            {
                i++;
                continue;
            }

            // Find the matching ldloc followed by stind
            int ldlocIndex = FindMatchingLdlocBeforeStind(instructions, i + 2, helperIndex);
            if (ldlocIndex == -1)
            {
                i++;
                continue;
            }

            var ldlocInstr = instructions[ldlocIndex];
            var stindInstr = instructions[ldlocIndex + 1];

            // Verify the pattern: ldloc helper, stind
            if (!IsStind(stindInstr))
            {
                i++;
                continue;
            }

            // Check that the helper variable is not used elsewhere between stloc and ldloc
            if (IsVariableUsedBetween(instructions, i + 2, ldlocIndex, helperIndex))
            {
                i++;
                continue;
            }

            // If the value is a ldloc, check that the local is not modified between
            // the original position and where we're moving it (before stind)
            if (IsLdloc(valueInstr))
            {
                int valueLocalIndex = GetLocalIndex(valueInstr);
                if (valueLocalIndex != -1 && IsLocalModifiedBetween(instructions, i + 1, ldlocIndex + 1, valueLocalIndex))
                {
                    i++;
                    continue;
                }
            }

            // Perform the optimization:
            // 1. Create a copy of the value load instruction
            // 2. Remove the original value load at i
            // 3. Remove the stloc at i+1 (now at i after previous removal)
            // 4. Replace ldloc with the copied value load

            // Create a copy of the simple load instruction
            var newValueInstr = CopyInstruction(il, valueInstr);

            // Remove original value load at i
            il.Remove(valueInstr);

            // After removal, stloc is now at index i
            // Remove stloc
            il.Remove(instructions[i]);

            // After both removals, ldlocIndex shifted by -2
            int newLdlocIndex = ldlocIndex - 2;

            // Replace ldloc with the new value load
            var oldLdloc = instructions[newLdlocIndex];
            il.Replace(oldLdloc, newValueInstr);

            changed = true;
            // Don't increment i - check this position again for consecutive patterns
        }

        return changed;
    }

    /// <summary>
    /// Check if instruction is a simple load (can be safely duplicated/moved).
    /// </summary>
    private static bool IsSimpleLoad(Instruction instr)
    {
        var op = instr.OpCode;

        // Argument loads
        if (op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg_1 || op == OpCodes.Ldarg_2 ||
            op == OpCodes.Ldarg_3 || op == OpCodes.Ldarg_S || op == OpCodes.Ldarg)
            return true;

        // Local loads
        if (op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 ||
            op == OpCodes.Ldloc_3 || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc)
            return true;

        // Constants
        if (op == OpCodes.Ldc_I4_M1 || op == OpCodes.Ldc_I4_0 || op == OpCodes.Ldc_I4_1 ||
            op == OpCodes.Ldc_I4_2 || op == OpCodes.Ldc_I4_3 || op == OpCodes.Ldc_I4_4 ||
            op == OpCodes.Ldc_I4_5 || op == OpCodes.Ldc_I4_6 || op == OpCodes.Ldc_I4_7 ||
            op == OpCodes.Ldc_I4_8 || op == OpCodes.Ldc_I4_S || op == OpCodes.Ldc_I4 ||
            op == OpCodes.Ldc_I8 || op == OpCodes.Ldc_R4 || op == OpCodes.Ldc_R8)
            return true;

        return false;
    }

    private static bool IsStloc(Instruction instr)
    {
        var op = instr.OpCode;
        return op == OpCodes.Stloc_0 || op == OpCodes.Stloc_1 || op == OpCodes.Stloc_2 ||
               op == OpCodes.Stloc_3 || op == OpCodes.Stloc_S || op == OpCodes.Stloc;
    }

    private static bool IsLdloc(Instruction instr)
    {
        var op = instr.OpCode;
        return op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 ||
               op == OpCodes.Ldloc_3 || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc;
    }

    private static bool IsStind(Instruction instr)
    {
        var op = instr.OpCode;
        return op == OpCodes.Stind_I1 || op == OpCodes.Stind_I2 || op == OpCodes.Stind_I4 ||
               op == OpCodes.Stind_I8 || op == OpCodes.Stind_R4 || op == OpCodes.Stind_R8 ||
               op == OpCodes.Stind_I || op == OpCodes.Stind_Ref;
    }

    /// <summary>
    /// Get the local variable index from a stloc or ldloc instruction.
    /// </summary>
    private static int GetLocalIndex(Instruction instr)
    {
        var op = instr.OpCode;

        if (op == OpCodes.Stloc_0 || op == OpCodes.Ldloc_0) return 0;
        if (op == OpCodes.Stloc_1 || op == OpCodes.Ldloc_1) return 1;
        if (op == OpCodes.Stloc_2 || op == OpCodes.Ldloc_2) return 2;
        if (op == OpCodes.Stloc_3 || op == OpCodes.Ldloc_3) return 3;

        if (instr.Operand is VariableDefinition varDef)
            return varDef.Index;

        return -1;
    }

    /// <summary>
    /// Find ldloc X followed by stind, starting from startIndex.
    /// Returns the index of ldloc, or -1 if not found.
    /// </summary>
    private static int FindMatchingLdlocBeforeStind(
        Mono.Collections.Generic.Collection<Instruction> instructions,
        int startIndex,
        int helperIndex)
    {
        for (int i = startIndex; i < instructions.Count - 1; i++)
        {
            var instr = instructions[i];
            if (!IsLdloc(instr))
                continue;

            if (GetLocalIndex(instr) != helperIndex)
                continue;

            // Check if next instruction is stind
            if (IsStind(instructions[i + 1]))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Check if the variable is used (read or written) between two indices.
    /// </summary>
    private static bool IsVariableUsedBetween(
        Mono.Collections.Generic.Collection<Instruction> instructions,
        int startIndex,
        int endIndex,
        int helperIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            var instr = instructions[i];
            if (IsStloc(instr) || IsLdloc(instr))
            {
                if (GetLocalIndex(instr) == helperIndex)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a local variable is modified (stloc) between two indices.
    /// </summary>
    private static bool IsLocalModifiedBetween(
        Mono.Collections.Generic.Collection<Instruction> instructions,
        int startIndex,
        int endIndex,
        int localIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            var instr = instructions[i];
            if (IsStloc(instr) && GetLocalIndex(instr) == localIndex)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Create a copy of an instruction.
    /// </summary>
    private static Instruction CopyInstruction(ILProcessor il, Instruction original)
    {
        if (original.Operand == null)
            return il.Create(original.OpCode);

        return original.Operand switch
        {
            int i => il.Create(original.OpCode, i),
            long l => il.Create(original.OpCode, l),
            float f => il.Create(original.OpCode, f),
            double d => il.Create(original.OpCode, d),
            sbyte sb => il.Create(original.OpCode, sb),
            byte b => il.Create(original.OpCode, (sbyte)b),
            VariableDefinition v => il.Create(original.OpCode, v),
            ParameterDefinition p => il.Create(original.OpCode, p),
            _ => il.Create(original.OpCode)
        };
    }
}
