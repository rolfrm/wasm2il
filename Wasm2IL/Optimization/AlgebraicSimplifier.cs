using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Algebraic simplification pass that removes identity operations.
/// Examples:
/// - a + 0, 0 + a => a
/// - a - 0 => a
/// - a * 1, 1 * a => a
/// - a * 0, 0 * a => 0
/// - a / 1 => a
/// - a | 0, 0 | a => a
/// - a &amp; -1, -1 &amp; a => a
/// - a ^ 0, 0 ^ a => a
/// - a &lt;&lt; 0, a &gt;&gt; 0 => a
/// </summary>
public class AlgebraicSimplifier : IOptimizationPass
{
    public bool Run(MethodBody body)
    {
        bool changed = false;
        var instructions = body.Instructions;

        for (int i = 0; i < instructions.Count - 1; i++)
        {
            // Try patterns with constant as second operand: a, const, op
            if (i >= 1 && TrySimplifyBinaryOpWithConstantSecond(body, i - 1))
            {
                changed = true;
                i = Math.Max(0, i - 2);
                continue;
            }

            // Try patterns with constant as first operand: const, a, op
            if (i >= 1 && TrySimplifyBinaryOpWithConstantFirst(body, i - 1))
            {
                changed = true;
                i = Math.Max(0, i - 2);
                continue;
            }
        }

        return changed;
    }

    /// <summary>
    /// Try to simplify: [any], [const], [op] patterns
    /// e.g., a + 0 => a, a * 1 => a, a * 0 => 0
    /// </summary>
    bool TrySimplifyBinaryOpWithConstantSecond(MethodBody body, int index)
    {
        var instructions = body.Instructions;
        if (index + 2 >= instructions.Count)
            return false;

        var instr0 = instructions[index];      // first operand (unknown)
        var instr1 = instructions[index + 1];  // second operand (constant)
        var instr2 = instructions[index + 2];  // operation

        // Skip if first instruction is a constant (handled by ConstantFolder)
        if (IsConstant(instr0))
            return false;

        // NOTE: We do NOT need to check if instr0 is a simple load here.
        // Since const (instr1) is directly before op (instr2), the constant
        // is definitely consumed by the op - it cannot be consumed by instr0
        // because instr0 executes BEFORE the constant is pushed.
        //
        // Example that works:
        //   call SomeMethod()  ; pushes result
        //   ldc.i4.1           ; pushes 1
        //   mul                ; result * 1 => result
        //
        // The call can be anything - it doesn't matter because the constant
        // is adjacent to the mul, so the mul definitely consumes the 1.

        // Check for i32 identity patterns
        if (TryGetI32Constant(instr1, out int i32val))
        {
            return TrySimplifyI32WithConstantSecond(body, index, i32val, instr2.OpCode);
        }

        // Check for i64 identity patterns
        if (TryGetI64Constant(instr1, out long i64val))
        {
            return TrySimplifyI64WithConstantSecond(body, index, i64val, instr2.OpCode);
        }

        // Check for f32 identity patterns
        if (TryGetF32Constant(instr1, out float f32val))
        {
            return TrySimplifyF32WithConstantSecond(body, index, f32val, instr2.OpCode);
        }

        // Check for f64 identity patterns
        if (TryGetF64Constant(instr1, out double f64val))
        {
            return TrySimplifyF64WithConstantSecond(body, index, f64val, instr2.OpCode);
        }

        return false;
    }

    /// <summary>
    /// Try to simplify: [const], [any], [op] patterns
    /// e.g., 0 + a => a, 1 * a => a, 0 * a => 0
    /// </summary>
    bool TrySimplifyBinaryOpWithConstantFirst(MethodBody body, int index)
    {
        var instructions = body.Instructions;
        if (index + 2 >= instructions.Count)
            return false;

        var instr0 = instructions[index];      // first operand (constant)
        var instr1 = instructions[index + 1];  // second operand (unknown)
        var instr2 = instructions[index + 2];  // operation

        // Skip if second instruction is also a constant (handled by ConstantFolder)
        if (IsConstant(instr1))
            return false;

        // IMPORTANT: The second operand must be a "simple load" - an instruction that
        // pushes exactly 1 value and pops 0 values. Otherwise, the constant we pushed
        // might be consumed by instr1 (e.g., as an argument to a call), and the pattern
        // doesn't match what we think it does.
        // Example of bad pattern:
        //   ldc.i4.0
        //   call SomeMethod(int)  <- consumes the 0!
        //   add                   <- operates on different values
        if (!IsSimpleLoad(instr1))
            return false;

        // Check for i32 identity patterns
        if (TryGetI32Constant(instr0, out int i32val))
        {
            return TrySimplifyI32WithConstantFirst(body, index, i32val, instr2.OpCode);
        }

        // Check for i64 identity patterns
        if (TryGetI64Constant(instr0, out long i64val))
        {
            return TrySimplifyI64WithConstantFirst(body, index, i64val, instr2.OpCode);
        }

        // Check for f32 identity patterns
        if (TryGetF32Constant(instr0, out float f32val))
        {
            return TrySimplifyF32WithConstantFirst(body, index, f32val, instr2.OpCode);
        }

        // Check for f64 identity patterns
        if (TryGetF64Constant(instr0, out double f64val))
        {
            return TrySimplifyF64WithConstantFirst(body, index, f64val, instr2.OpCode);
        }

        return false;
    }

    #region I32 Simplification

    bool TrySimplifyI32WithConstantSecond(MethodBody body, int index, int constVal, OpCode op)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;

        // a + 0 => a
        // a - 0 => a
        // a | 0 => a
        // a ^ 0 => a
        // a << 0 => a
        // a >> 0 => a
        if (constVal == 0 && (op == OpCodes.Add || op == OpCodes.Sub ||
                              op == OpCodes.Or || op == OpCodes.Xor ||
                              op == OpCodes.Shl || op == OpCodes.Shr || op == OpCodes.Shr_Un))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        // a * 1 => a
        // a / 1 => a
        if (constVal == 1 && (op == OpCodes.Mul || op == OpCodes.Div || op == OpCodes.Div_Un))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        // a & -1 => a (all bits set)
        if (constVal == -1 && op == OpCodes.And)
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }
        
        // a ^ -1 => a (all bits set)
        if (constVal == -1 && op == OpCodes.Xor)
        {
            instructions.RemoveAt(index + 2);
            instructions.RemoveAt(index + 1);
            instructions.Insert(index + 1, il.Create(OpCodes.Not));
            return true;
        }

        // a * 0 => 0 (but need to keep side effects - only if first operand is simple)
        if (constVal == 0 && op == OpCodes.Mul && IsSimpleLoad(instructions[index]))
        {
            ReplaceWithConstant(body, index, 0);
            return true;
        }

        return false;
    }

    bool TrySimplifyI32WithConstantFirst(MethodBody body, int index, int constVal, OpCode op)
    {
        var instructions = body.Instructions;

        // 0 + a => a
        // 0 | a => a
        // 0 ^ a => a
        if (constVal == 0 && (op == OpCodes.Add || op == OpCodes.Or || op == OpCodes.Xor))
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        // 1 * a => a
        if (constVal == 1 && op == OpCodes.Mul)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        // -1 & a => a
        if (constVal == -1 && op == OpCodes.And)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }
        
        // -1 ^ a  => a (all bits set)
        if (constVal == -1 && op == OpCodes.Xor)
        {
            var il = body.GetILProcessor();
            instructions.RemoveAt(index + 1);
            instructions.RemoveAt(index);
            instructions.Insert(index + 1, il.Create(OpCodes.Not));
            return true;
        }

        // 0 * a => 0 (but need to keep side effects - only if second operand is simple)
        if (constVal == 0 && op == OpCodes.Mul && IsSimpleLoad(instructions[index + 1]))
        {
            ReplaceWithConstant(body, index, 0, removeSecondOperand: true);
            return true;
        }

        return false;
    }

    #endregion

    #region I64 Simplification

    bool TrySimplifyI64WithConstantSecond(MethodBody body, int index, long constVal, OpCode op)
    {
        var instructions = body.Instructions;

        if (constVal == 0 && (op == OpCodes.Add || op == OpCodes.Sub ||
                              op == OpCodes.Or || op == OpCodes.Xor ||
                              op == OpCodes.Shl || op == OpCodes.Shr || op == OpCodes.Shr_Un))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        if (constVal == 1 && (op == OpCodes.Mul || op == OpCodes.Div || op == OpCodes.Div_Un))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        if (constVal == -1 && op == OpCodes.And)
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        if (constVal == 0 && op == OpCodes.Mul && IsSimpleLoad(instructions[index]))
        {
            ReplaceWithI64Constant(body, index, 0);
            return true;
        }

        return false;
    }

    bool TrySimplifyI64WithConstantFirst(MethodBody body, int index, long constVal, OpCode op)
    {
        var instructions = body.Instructions;

        if (constVal == 0 && (op == OpCodes.Add || op == OpCodes.Or || op == OpCodes.Xor))
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        if (constVal == 1 && op == OpCodes.Mul)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        if (constVal == -1 && op == OpCodes.And)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        if (constVal == 0 && op == OpCodes.Mul && IsSimpleLoad(instructions[index + 1]))
        {
            ReplaceWithI64Constant(body, index, 0, removeSecondOperand: true);
            return true;
        }

        return false;
    }

    #endregion

    #region F32 Simplification

    bool TrySimplifyF32WithConstantSecond(MethodBody body, int index, float constVal, OpCode op)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 0.0f && (op == OpCodes.Add || op == OpCodes.Sub))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 1.0f && (op == OpCodes.Mul || op == OpCodes.Div))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        return false;
    }

    bool TrySimplifyF32WithConstantFirst(MethodBody body, int index, float constVal, OpCode op)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 0.0f && op == OpCodes.Add)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 1.0f && op == OpCodes.Mul)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        return false;
    }

    #endregion

    #region F64 Simplification

    bool TrySimplifyF64WithConstantSecond(MethodBody body, int index, double constVal, OpCode op)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 0.0 && (op == OpCodes.Add || op == OpCodes.Sub))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 1.0 && (op == OpCodes.Mul || op == OpCodes.Div))
        {
            RemoveConstantAndOp(body, index + 1);
            return true;
        }

        return false;
    }

    bool TrySimplifyF64WithConstantFirst(MethodBody body, int index, double constVal, OpCode op)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 0.0 && op == OpCodes.Add)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (constVal == 1.0 && op == OpCodes.Mul)
        {
            RemoveConstantAndOp(body, index, removeFirst: true);
            return true;
        }

        return false;
    }

    #endregion

    #region Helpers

    static bool IsConstant(Instruction instr)
    {
        var op = instr.OpCode;
        return op == OpCodes.Ldc_I4_M1 || op == OpCodes.Ldc_I4_0 || op == OpCodes.Ldc_I4_1 ||
               op == OpCodes.Ldc_I4_2 || op == OpCodes.Ldc_I4_3 || op == OpCodes.Ldc_I4_4 ||
               op == OpCodes.Ldc_I4_5 || op == OpCodes.Ldc_I4_6 || op == OpCodes.Ldc_I4_7 ||
               op == OpCodes.Ldc_I4_8 || op == OpCodes.Ldc_I4_S || op == OpCodes.Ldc_I4 ||
               op == OpCodes.Ldc_I8 || op == OpCodes.Ldc_R4 || op == OpCodes.Ldc_R8;
    }

    /// <summary>
    /// Check if an instruction is a "simple load" - pushes exactly 1 value and pops 0 values.
    /// This includes: ldarg, ldloc, ldsfld, constants, and parameterless calls that return a value.
    /// </summary>
    static bool IsSimpleLoad(Instruction instr)
    {
        var op = instr.OpCode;

        // Standard loads that push 1 value and pop 0
        if (op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg_1 || op == OpCodes.Ldarg_2 ||
            op == OpCodes.Ldarg_3 || op == OpCodes.Ldarg_S || op == OpCodes.Ldarg ||
            op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 ||
            op == OpCodes.Ldloc_3 || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc ||
            op == OpCodes.Ldsfld)
        {
            return true;
        }

        // Constants are simple loads
        if (IsConstant(instr))
            return true;

        // Calls with no parameters that return a value are also simple loads
        // (they push 1 value and pop 0 values)
        if (op == OpCodes.Call && instr.Operand is MethodReference method)
        {
            bool hasNoParams = method.Parameters.Count == 0;
            bool returnsValue = method.ReturnType.FullName != "System.Void";
            return hasNoParams && returnsValue;
        }

        return false;
    }

    static bool TryGetI32Constant(Instruction instr, out int value)
    {
        value = 0;
        var opCode = instr.OpCode;

        if (opCode == OpCodes.Ldc_I4_M1) { value = -1; return true; }
        if (opCode == OpCodes.Ldc_I4_0) { value = 0; return true; }
        if (opCode == OpCodes.Ldc_I4_1) { value = 1; return true; }
        if (opCode == OpCodes.Ldc_I4_2) { value = 2; return true; }
        if (opCode == OpCodes.Ldc_I4_3) { value = 3; return true; }
        if (opCode == OpCodes.Ldc_I4_4) { value = 4; return true; }
        if (opCode == OpCodes.Ldc_I4_5) { value = 5; return true; }
        if (opCode == OpCodes.Ldc_I4_6) { value = 6; return true; }
        if (opCode == OpCodes.Ldc_I4_7) { value = 7; return true; }
        if (opCode == OpCodes.Ldc_I4_8) { value = 8; return true; }
        if (opCode == OpCodes.Ldc_I4_S) { value = (sbyte)instr.Operand; return true; }
        if (opCode == OpCodes.Ldc_I4) { value = (int)instr.Operand; return true; }

        return false;
    }

    static bool TryGetI64Constant(Instruction instr, out long value)
    {
        value = 0;
        if (instr.OpCode == OpCodes.Ldc_I8)
        {
            value = (long)instr.Operand;
            return true;
        }
        return false;
    }

    static bool TryGetF32Constant(Instruction instr, out float value)
    {
        value = 0;
        if (instr.OpCode == OpCodes.Ldc_R4)
        {
            value = (float)instr.Operand;
            return true;
        }
        return false;
    }

    static bool TryGetF64Constant(Instruction instr, out double value)
    {
        value = 0;
        if (instr.OpCode == OpCodes.Ldc_R8)
        {
            value = (double)instr.Operand;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove constant at index and the operation at index+1
    /// Used for patterns like: a, 0, add => a
    /// </summary>
    void RemoveConstantAndOp(MethodBody body, int constIndex, bool removeFirst = false)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;

        if (removeFirst)
        {
            // Pattern: const, a, op => a
            // Remove const (index) and op (index+2, becomes index+1 after first removal)
            var constInstr = instructions[constIndex];
            var opInstr = instructions[constIndex + 2];

            // Redirect branches from const to a
            RedirectBranches(body, constInstr, instructions[constIndex + 1]);
            il.Remove(constInstr);
            il.Remove(opInstr); // now at constIndex + 1
        }
        else
        {
            // Pattern: a, const, op => a
            // Remove const (constIndex) and op (constIndex+1)
            var constInstr = instructions[constIndex];
            var opInstr = instructions[constIndex + 1];

            il.Remove(constInstr);
            il.Remove(opInstr); // now at constIndex
        }
    }

    
    /// <summary>
    /// Replace the entire expression with a constant.
    /// Used for patterns like: a * 0 => 0
    /// </summary>
    void ReplaceWithConstant(MethodBody body, int index, int value, bool removeSecondOperand = false)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;

        var firstInstr = instructions[index];
        var newInstr = CreateLdcI4(il, value);

        RedirectBranches(body, firstInstr, newInstr);
        il.Replace(firstInstr, newInstr);
        il.Remove(instructions[index + 1]); // second operand or const
        il.Remove(instructions[index + 1]); // op
    }

    void ReplaceWithI64Constant(MethodBody body, int index, long value, bool removeSecondOperand = false)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;

        var firstInstr = instructions[index];
        var newInstr = il.Create(OpCodes.Ldc_I8, value);

        RedirectBranches(body, firstInstr, newInstr);
        il.Replace(firstInstr, newInstr);
        il.Remove(instructions[index + 1]);
        il.Remove(instructions[index + 1]);
    }

    static Instruction CreateLdcI4(ILProcessor il, int value)
    {
        return value switch
        {
            -1 => il.Create(OpCodes.Ldc_I4_M1),
            0 => il.Create(OpCodes.Ldc_I4_0),
            1 => il.Create(OpCodes.Ldc_I4_1),
            2 => il.Create(OpCodes.Ldc_I4_2),
            3 => il.Create(OpCodes.Ldc_I4_3),
            4 => il.Create(OpCodes.Ldc_I4_4),
            5 => il.Create(OpCodes.Ldc_I4_5),
            6 => il.Create(OpCodes.Ldc_I4_6),
            7 => il.Create(OpCodes.Ldc_I4_7),
            8 => il.Create(OpCodes.Ldc_I4_8),
            >= -128 and <= 127 => il.Create(OpCodes.Ldc_I4_S, (sbyte)value),
            _ => il.Create(OpCodes.Ldc_I4, value)
        };
    }

    static void RedirectBranches(MethodBody body, Instruction oldTarget, Instruction newTarget)
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

    #endregion
}
