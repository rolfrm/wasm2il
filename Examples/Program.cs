using Wasm2IL;

// =============================================================================
// Examples demonstrating wasm2il usage (from README.md)
// This file serves as both documentation and test - it will fail if any
// assertion fails, making it suitable for CI testing.
// =============================================================================

Console.WriteLine("=== wasm2il Examples ===\n");

var testsPassed = 0;
var testsFailed = 0;

void Assert(bool condition, string testName)
{
    if (condition)
    {
        Console.WriteLine($"  PASS: {testName}");
        testsPassed++;
    }
    else
    {
        Console.WriteLine($"  FAIL: {testName}");
        testsFailed++;
    }
}

// -----------------------------------------------------------------------------
// Setup: Load WASM and register import modules
// -----------------------------------------------------------------------------

Console.WriteLine("--- Setup ---");

// Create transformer and register import implementations
var transformer = new Transformer();
transformer.LoadImportModule("env", typeof(EnvModule));

// Load and convert WASM to .NET assembly
// The WASM file is built from CCode/mathlib.c during build
var wasmPath = Path.Combine(AppContext.BaseDirectory, "mathlib.wasm");
Console.WriteLine($"Loading WASM from: {wasmPath}");
var asm = transformer.LoadWasmAssembly(wasmPath, "MathLib");
Console.WriteLine($"Loaded assembly: {asm.Name}\n");

// -----------------------------------------------------------------------------
// Example 1: Direct Invocation with Invoke()
// The simplest way to call WASM functions
// -----------------------------------------------------------------------------

Console.WriteLine("--- Example: Direct Invocation with Invoke() ---");

// Call WASM functions using Invoke (handles basic marshaling)
int sum = (int)asm.Invoke("add", 5, 3)!;
Console.WriteLine($"add(5, 3) = {sum}");
Assert(sum == 8, "add(5, 3) == 8");

int product = (int)asm.Invoke("multiply", 4, 7)!;
Console.WriteLine($"multiply(4, 7) = {product}");
Assert(product == 28, "multiply(4, 7) == 28");

// This calls log_value which is implemented in EnvModule
int computed = (int)asm.Invoke("compute_and_log", 2, 3)!;
Console.WriteLine($"compute_and_log(2, 3) = {computed}");
// compute_and_log: (a+b) * (a*b) = (2+3) * (2*3) = 5 * 6 = 30
Assert(computed == 30, "compute_and_log(2, 3) == 30");
Assert(EnvModule.GetLastLoggedValue() == 30, "log_value was called with 30");

Console.WriteLine();

// -----------------------------------------------------------------------------
// Example 2: Typed Interface with AsImplementation<T>
// A cleaner API using strongly-typed interfaces
// -----------------------------------------------------------------------------

Console.WriteLine("--- Example: Typed Interface with AsImplementation<T> ---");

// Create a typed wrapper - much cleaner to use!
var mathLib = asm.AsImplementation<IMathLib>();

// Use like normal C# methods
int typedSum = mathLib.Add(10, 20);
Console.WriteLine($"mathLib.Add(10, 20) = {typedSum}");
Assert(typedSum == 30, "mathLib.Add(10, 20) == 30");

int typedProduct = mathLib.Multiply(5, 6);
Console.WriteLine($"mathLib.Multiply(5, 6) = {typedProduct}");
Assert(typedProduct == 30, "mathLib.Multiply(5, 6) == 30");

int typedComputed = mathLib.ComputeAndLog(3, 4);
Console.WriteLine($"mathLib.ComputeAndLog(3, 4) = {typedComputed}");
// (3+4) * (3*4) = 7 * 12 = 84
Assert(typedComputed == 84, "mathLib.ComputeAndLog(3, 4) == 84");

Console.WriteLine();

// =============================================================================
// Summary
// =============================================================================

Console.WriteLine("=== Summary ===");
Console.WriteLine($"Tests passed: {testsPassed}");
Console.WriteLine($"Tests failed: {testsFailed}");

if (testsFailed > 0)
{
    Console.WriteLine("\nSome tests failed!");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("\nAll examples completed successfully!");
    Environment.Exit(0);
}

// =============================================================================
// Type Declarations (must come after top-level statements in C#)
// =============================================================================

/// <summary>
/// Implementation for "env" module imports.
/// The method names must match the import names in the WASM module.
/// </summary>
public static class EnvModule
{
    private static int _lastLoggedValue;

    public static void log_value(int value)
    {
        Console.WriteLine($"[WASM] Logged value: {value}");
        _lastLoggedValue = value;
    }

    public static int GetLastLoggedValue() => _lastLoggedValue;
}

/// <summary>
/// Typed interface for the mathlib WASM module.
/// Each method maps to a WASM export via the [Wasm] attribute.
/// </summary>
public interface IMathLib
{
    [Wasm("add")]
    int Add(int a, int b);

    [Wasm("multiply")]
    int Multiply(int a, int b);

    [Wasm("compute_and_log")]
    int ComputeAndLog(int a, int b);
}
