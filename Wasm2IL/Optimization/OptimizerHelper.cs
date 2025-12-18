using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

class OptimizerHelper
{
    static int GetVarPopCount(Instruction instruction)
    {
        // VarPop is used for call, calli, callvirt
        if (instruction.OpCode.Code == Code.Call ||
            instruction.OpCode.Code == Code.Callvirt ||
            instruction.OpCode.Code == Code.Calli)
        {
            if (instruction.Operand is MethodReference method)
            {
                int count = method.Parameters.Count;
                // Add 1 if not static (for 'this')
                if (method.HasThis && instruction.OpCode.Code != Code.Calli)
                    count++;
                return count;
            }
        }

        return 0;
    }

    static int GetPopCount(StackBehaviour behavior, Instruction instruction)
    {
        return behavior switch
        {
            StackBehaviour.Pop0 => 0,
            StackBehaviour.Pop1 => 1,
            StackBehaviour.Pop1_pop1 => 2,
            StackBehaviour.Popi => 1,
            StackBehaviour.Popi_pop1 => 2,
            StackBehaviour.Popi_popi => 2,
            StackBehaviour.Popi_popi8 => 2,
            StackBehaviour.Popi_popi_popi => 3,
            StackBehaviour.Popi_popr4 => 2,
            StackBehaviour.Popi_popr8 => 2,
            StackBehaviour.Popref => 1,
            StackBehaviour.Popref_pop1 => 2,
            StackBehaviour.Popref_popi => 2,
            StackBehaviour.Popref_popi_popi => 3,
            StackBehaviour.Popref_popi_popi8 => 3,
            StackBehaviour.Popref_popi_popr4 => 3,
            StackBehaviour.Popref_popi_popr8 => 3,
            StackBehaviour.Popref_popi_popref => 3,
            StackBehaviour.PopAll => throw new NotImplementedException("PopAll requires exception handling analysis"),
            StackBehaviour.Varpop => GetVarPopCount(instruction),
            _ => 0
        };
    }

    static int GetPushCount(StackBehaviour behavior, Instruction instruction)
    {
        return behavior switch
        {
            StackBehaviour.Push0 => 0,
            StackBehaviour.Push1 => 1,
            StackBehaviour.Push1_push1 => 2,
            StackBehaviour.Pushi => 1,
            StackBehaviour.Pushi8 => 1,
            StackBehaviour.Pushr4 => 1,
            StackBehaviour.Pushr8 => 1,
            StackBehaviour.Pushref => 1,
            StackBehaviour.Varpush => GetVarPushCount(instruction),
            _ => 0
        };
    }

    public static List<Instruction> GetValueSource(MethodBody method, int popCount, int it = -1)
    {
        if (it == -1)
            it = method.Instructions.Count;

        int stack = 0;
        int start = it;
        List<Instruction> inputs = [];
        while (popCount > 0 && it > 0 && (start - it) < 10)
        {
            it--;
            var instr2 = method.Instructions[it];
            if (instr2.OpCode == OpCodes.Nop)
                break;

            int pushCount = GetPushCount(instr2.OpCode.StackBehaviourPush, instr2);
            int popCount2 = GetPopCount(instr2.OpCode.StackBehaviourPop, instr2);
            stack -= pushCount;
            while (stack < 0)
            {
                popCount--;
                stack++;
                inputs.Add(instr2);
            }

            stack += popCount2;
        }

        return inputs;
    }

    static int GetVarPushCount(Instruction instruction)
    {
        // Most methods push 1 value (or 0 for void)
        if (instruction.Operand is MethodReference method)
        {
            return method.ReturnType.FullName == "System.Void" ? 0 : 1;
        }

        return 0;
    }
}