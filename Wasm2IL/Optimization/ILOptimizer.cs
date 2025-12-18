using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Wasm2IL.Optimization;

/// <summary>
/// Orchestrates IL optimization passes on generated methods.
/// Runs passes in a fixed-point loop until no more changes occur.
/// </summary>
public class ILOptimizer
{
    readonly MethodBody _body;
    readonly List<IOptimizationPass> _passes = new();

    public ILOptimizer(MethodBody body)
    {
        _body = body;

        // Register default optimization passes
        _passes.Add(new ConstantFolder());
        _passes.Add(new AlgebraicSimplifier());
    }

    /// <summary>
    /// Add a custom optimization pass.
    /// </summary>
    public void AddPass(IOptimizationPass pass)
    {
        _passes.Add(pass);
    }

    /// <summary>
    /// Run all optimization passes until a fixed point is reached.
    /// </summary>
    public void Optimize()
    {
        bool changed;
        int iterations = 0;
        const int maxIterations = 100; // Safety limit

        do
        {
            changed = false;
            foreach (var pass in _passes)
            {
                changed |= pass.Run(_body);
            }

            iterations++;
        } while (changed && iterations < maxIterations);
    }

    /// <summary>
    /// Optimize a single method.
    /// </summary>
    public static void OptimizeMethod(MethodDefinition method)
    {
        if (method.Body == null || method.Body.Instructions.Count == 0)
            return;

        var optimizer = new ILOptimizer(method.Body);
        optimizer.Optimize();
    }

    /// <summary>
    /// Optimize all methods in a type.
    /// </summary>
    public static void OptimizeType(TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            OptimizeMethod(method);
        }
    }
}

/// <summary>
/// Interface for optimization passes.
/// </summary>
public interface IOptimizationPass
{
    /// <summary>
    /// Run the optimization pass on the method body.
    /// </summary>
    /// <returns>True if any changes were made.</returns>
    bool Run(MethodBody body);
}