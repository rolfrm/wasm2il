using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Wasm2IL.Utils;

public static class IlExtensions
{
    public static void EmitCall(this ILProcessor gen, Expression expr)
    {
        var lambda = (LambdaExpression)expr;
        var unary = (UnaryExpression)lambda.Body;
        var methodCall = (MethodCallExpression)unary.Operand;
        var constant = (ConstantExpression)methodCall.Object!;
        var method = (MethodInfo)constant.Value!;
        var declType = gen.Body.Method.DeclaringType!;
        gen.Emit(OpCodes.Call, declType.Module.ImportReference(method));
    }
}
