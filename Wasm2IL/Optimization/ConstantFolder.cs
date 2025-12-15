using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Constant folding optimization pass.
/// Folds sequences like: ldc X, ldc Y, add => ldc (X+Y)
/// </summary>
public class ConstantFolder : IOptimizationPass
{
    public bool Run(MethodBody body)
    {
        bool changed = false;
        var instructions = body.Instructions;

        // Keep scanning until no more changes
        for (int i = 0; i < instructions.Count - 2; i++)
        {
            // Try to fold binary operations on two constants
            if (TryFoldBinaryOp(body, i))
            {
                changed = true;
                // Restart from current position since we removed instructions
                i = Math.Max(0, i - 1);
                continue;
            }

            // Try to fold unary operations on a constant
            if (TryFoldUnaryOp(body, i))
            {
                changed = true;
                i = Math.Max(0, i - 1);
                continue;
            }
        }

        return changed;
    }

    /// <summary>
    /// Try to fold a binary operation: ldc A, ldc B, op => ldc result
    /// </summary>
    bool TryFoldBinaryOp(MethodBody body, int index)
    {
        var instructions = body.Instructions;
        if (index + 2 >= instructions.Count)
            return false;

        var instr0 = instructions[index];
        var instr1 = instructions[index + 1];
        var instr2 = instructions[index + 2];

        // Check for i32 binary operations
        if (TryGetI32Constant(instr0, out int a) &&
            TryGetI32Constant(instr1, out int b))
        {
            int? result = EvaluateI32BinaryOp(instr2.OpCode, a, b);
            if (result.HasValue)
            {
                ReplaceBinaryWithConstant(body, index, result.Value);
                return true;
            }
        }

        // Check for i64 binary operations
        if (TryGetI64Constant(instr0, out long la) &&
            TryGetI64Constant(instr1, out long lb))
        {
            long? result = EvaluateI64BinaryOp(instr2.OpCode, la, lb);
            if (result.HasValue)
            {
                ReplaceBinaryWithI64Constant(body, index, result.Value);
                return true;
            }
        }

        // Check for f32 binary operations
        if (TryGetF32Constant(instr0, out float fa) &&
            TryGetF32Constant(instr1, out float fb))
        {
            float? result = EvaluateF32BinaryOp(instr2.OpCode, fa, fb);
            if (result.HasValue)
            {
                ReplaceBinaryWithF32Constant(body, index, result.Value);
                return true;
            }
        }

        // Check for f64 binary operations
        if (TryGetF64Constant(instr0, out double da) &&
            TryGetF64Constant(instr1, out double db))
        {
            double? result = EvaluateF64BinaryOp(instr2.OpCode, da, db);
            if (result.HasValue)
            {
                ReplaceBinaryWithF64Constant(body, index, result.Value);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Try to fold a unary operation: ldc A, op => ldc result
    /// </summary>
    bool TryFoldUnaryOp(MethodBody body, int index)
    {
        var instructions = body.Instructions;
        if (index + 1 >= instructions.Count)
            return false;

        var instr0 = instructions[index];
        var instr1 = instructions[index + 1];

        // i32 unary operations
        if (TryGetI32Constant(instr0, out int a))
        {
            int? result = EvaluateI32UnaryOp(instr1.OpCode, a);
            if (result.HasValue)
            {
                ReplaceUnaryWithConstant(body, index, result.Value);
                return true;
            }

            // i32 to i64 conversion
            if (instr1.OpCode == OpCodes.Conv_I8)
            {
                ReplaceUnaryWithI64Constant(body, index, (long)a);
                return true;
            }
            if (instr1.OpCode == OpCodes.Conv_U8)
            {
                ReplaceUnaryWithI64Constant(body, index, (long)(uint)a);
                return true;
            }
            // i32 to f32/f64 conversion
            if (instr1.OpCode == OpCodes.Conv_R4)
            {
                ReplaceUnaryWithF32Constant(body, index, (float)a);
                return true;
            }
            if (instr1.OpCode == OpCodes.Conv_R8)
            {
                ReplaceUnaryWithF64Constant(body, index, (double)a);
                return true;
            }
        }

        // i64 unary operations
        if (TryGetI64Constant(instr0, out long la))
        {
            long? result = EvaluateI64UnaryOp(instr1.OpCode, la);
            if (result.HasValue)
            {
                ReplaceUnaryWithI64Constant(body, index, result.Value);
                return true;
            }

            // i64 to i32 conversion
            if (instr1.OpCode == OpCodes.Conv_I4)
            {
                ReplaceUnaryWithConstant(body, index, (int)la);
                return true;
            }
            if (instr1.OpCode == OpCodes.Conv_U4)
            {
                ReplaceUnaryWithConstant(body, index, (int)(uint)la);
                return true;
            }
            // i64 to f32/f64 conversion
            if (instr1.OpCode == OpCodes.Conv_R4)
            {
                ReplaceUnaryWithF32Constant(body, index, (float)la);
                return true;
            }
            if (instr1.OpCode == OpCodes.Conv_R8)
            {
                ReplaceUnaryWithF64Constant(body, index, (double)la);
                return true;
            }
        }

        // f64 unary operations
        if (TryGetF64Constant(instr0, out double da))
        {
            double? result = EvaluateF64UnaryOp(instr1.OpCode, da);
            if (result.HasValue)
            {
                ReplaceUnaryWithF64Constant(body, index, result.Value);
                return true;
            }
        }

        // f32 unary operations
        if (TryGetF32Constant(instr0, out float fa))
        {
            float? result = EvaluateF32UnaryOp(instr1.OpCode, fa);
            if (result.HasValue)
            {
                ReplaceUnaryWithF32Constant(body, index, result.Value);
                return true;
            }
        }

        return false;
    }

    #region Constant Extraction

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

    #endregion

    #region Binary Operation Evaluation

    static int? EvaluateI32BinaryOp(OpCode opCode, int a, int b)
    {
        if (opCode == OpCodes.Add) return a + b;
        if (opCode == OpCodes.Sub) return a - b;
        if (opCode == OpCodes.Mul) return a * b;
        if (opCode == OpCodes.Div && b != 0) return a / b;
        if (opCode == OpCodes.Div_Un && b != 0) return (int)((uint)a / (uint)b);
        if (opCode == OpCodes.Rem && b != 0) return a % b;
        if (opCode == OpCodes.Rem_Un && b != 0) return (int)((uint)a % (uint)b);
        if (opCode == OpCodes.And) return a & b;
        if (opCode == OpCodes.Or) return a | b;
        if (opCode == OpCodes.Xor) return a ^ b;
        if (opCode == OpCodes.Shl) return a << (b & 0x1F);
        if (opCode == OpCodes.Shr) return a >> (b & 0x1F);
        if (opCode == OpCodes.Shr_Un) return (int)((uint)a >> (b & 0x1F));
        if (opCode == OpCodes.Ceq) return a == b ? 1 : 0;
        if (opCode == OpCodes.Clt) return a < b ? 1 : 0;
        if (opCode == OpCodes.Clt_Un) return (uint)a < (uint)b ? 1 : 0;
        if (opCode == OpCodes.Cgt) return a > b ? 1 : 0;
        if (opCode == OpCodes.Cgt_Un) return (uint)a > (uint)b ? 1 : 0;
        return null;
    }

    static long? EvaluateI64BinaryOp(OpCode opCode, long a, long b)
    {
        if (opCode == OpCodes.Add) return a + b;
        if (opCode == OpCodes.Sub) return a - b;
        if (opCode == OpCodes.Mul) return a * b;
        if (opCode == OpCodes.Div && b != 0) return a / b;
        if (opCode == OpCodes.Div_Un && b != 0) return (long)((ulong)a / (ulong)b);
        if (opCode == OpCodes.Rem && b != 0) return a % b;
        if (opCode == OpCodes.Rem_Un && b != 0) return (long)((ulong)a % (ulong)b);
        if (opCode == OpCodes.And) return a & b;
        if (opCode == OpCodes.Or) return a | b;
        if (opCode == OpCodes.Xor) return a ^ b;
        if (opCode == OpCodes.Shl) return a << (int)(b & 0x3F);
        if (opCode == OpCodes.Shr) return a >> (int)(b & 0x3F);
        if (opCode == OpCodes.Shr_Un) return (long)((ulong)a >> (int)(b & 0x3F));
        return null;
    }

    static float? EvaluateF32BinaryOp(OpCode opCode, float a, float b)
    {
        if (opCode == OpCodes.Add) return a + b;
        if (opCode == OpCodes.Sub) return a - b;
        if (opCode == OpCodes.Mul) return a * b;
        if (opCode == OpCodes.Div) return a / b;
        return null;
    }

    static double? EvaluateF64BinaryOp(OpCode opCode, double a, double b)
    {
        if (opCode == OpCodes.Add) return a + b;
        if (opCode == OpCodes.Sub) return a - b;
        if (opCode == OpCodes.Mul) return a * b;
        if (opCode == OpCodes.Div) return a / b;
        return null;
    }

    #endregion

    #region Unary Operation Evaluation

    static int? EvaluateI32UnaryOp(OpCode opCode, int a)
    {
        if (opCode == OpCodes.Neg) return -a;
        if (opCode == OpCodes.Not) return ~a;
        return null;
    }

    static long? EvaluateI64UnaryOp(OpCode opCode, long a)
    {
        if (opCode == OpCodes.Neg) return -a;
        if (opCode == OpCodes.Not) return ~a;
        return null;
    }

    static float? EvaluateF32UnaryOp(OpCode opCode, float a)
    {
        if (opCode == OpCodes.Neg) return -a;
        return null;
    }

    static double? EvaluateF64UnaryOp(OpCode opCode, double a)
    {
        if (opCode == OpCodes.Neg) return -a;
        return null;
    }

    #endregion

    #region Instruction Replacement

    void ReplaceBinaryWithConstant(MethodBody body, int index, int value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;

        // Get the target instruction (the first ldc) for branch retargeting
        var targetInstr = instructions[index];

        // Create the new constant instruction
        var newInstr = CreateLdcI4(il, value);

        // Replace the first instruction with the new constant
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);

        // Remove the second ldc and the operation
        il.Remove(instructions[index + 1]); // was instr1
        il.Remove(instructions[index + 1]); // was instr2 (now at index+1)
    }

    void ReplaceBinaryWithI64Constant(MethodBody body, int index, long value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_I8, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceBinaryWithF32Constant(MethodBody body, int index, float value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_R4, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceBinaryWithF64Constant(MethodBody body, int index, double value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_R8, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceUnaryWithConstant(MethodBody body, int index, int value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = CreateLdcI4(il, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceUnaryWithI64Constant(MethodBody body, int index, long value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_I8, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceUnaryWithF32Constant(MethodBody body, int index, float value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_R4, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
        il.Remove(instructions[index + 1]);
    }

    void ReplaceUnaryWithF64Constant(MethodBody body, int index, double value)
    {
        var il = body.GetILProcessor();
        var instructions = body.Instructions;
        var targetInstr = instructions[index];

        var newInstr = il.Create(OpCodes.Ldc_R8, value);
        RedirectBranches(body, targetInstr, newInstr);
        il.Replace(targetInstr, newInstr);
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

    /// <summary>
    /// Redirect all branches pointing to oldTarget to point to newTarget.
    /// </summary>
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

        // Also update exception handlers
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
