using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Wasm2IL.Utils;

public static class IlExtensions
{
    public static void EmitCall(this ILProcessor gen, Expression expr)
    {
        var f =
            (((expr as LambdaExpression).Body as UnaryExpression).Operand as MethodCallExpression).Object as
            ConstantExpression;
        var method = (MethodInfo)f.Value;
        var declType = gen.Body.Method.DeclaringType;
        gen.Emit(Mono.Cecil.Cil.OpCodes.Call, declType.Module.ImportReference(method));
        

    }
}