<div align="center">

# wasm2il

**A WebAssembly to .NET IL bytecode compiler**

*Seamlessly convert WASM modules into native .NET assemblies*

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Wasm2IL?color=004880&logo=nuget)](https://www.nuget.org/packages/Wasm2IL)
[![WebAssembly](https://img.shields.io/badge/WebAssembly-1.0-654FF0?logo=webassembly)](https://webassembly.org/)
[![Status](https://img.shields.io/badge/Status-⚠️_Experimental-orange)](https://github.com/rolfrm/Wasm2IL)

[Features](#features) • [Quick Start](#quick-start) • [Installation](#installation) • [Compiling C to WASM](#compiling-c-to-webassembly-with-clang) • [Usage](#complete-example-wrapping-a-c-library) • [API Reference](#step-7-working-with-wasm-memory)

</div>

---

## What is wasm2il?

wasm2il bridges the gap between WebAssembly and the .NET ecosystem. It takes WebAssembly binary modules (`.wasm` files) and compiles them directly into .NET assemblies (`.dll` files), enabling code originally written in C, C++, Rust, or any language that compiles to WebAssembly to run natively on the .NET runtime.

### Core Idea

Instead of interpreting WebAssembly bytecode at runtime, wasm2il performs **ahead-of-time compilation** by translating each WASM instruction into equivalent .NET IL opcodes. The generated assembly can then be JIT-compiled by the .NET runtime, benefiting from all of .NET's runtime optimizations.

```
┌─────────────────┐      ┌─────────────┐      ┌─────────────────┐
│   C / C++ /     │      │             │      │                 │
│   Rust / etc.   │ ───► │  .wasm      │ ───► │  .NET Assembly  │
│                 │      │             │      │     (.dll)      │
└─────────────────┘      └─────────────┘      └─────────────────┘
    Source Code          WebAssembly           Native .NET
```

**Example translation:**

| WASM Instruction | .NET IL Equivalent |
|------------------|-------------------|
| `i32.add` | `IL.Add` |
| `i32.load` | `IL.Ldind_I4` (pointer dereference) |
| `call` | `IL.Call` to the appropriate method |

WASM linear memory is represented as a native `byte*` pointer, and function tables enable indirect calls for things like callbacks.

---

## Features

| Feature | Description |
|---------|-------------|
| **Direct IL Generation** | Compiles WASM directly to .NET IL using Mono.Cecil, producing standard .NET assemblies |
| **WASM 1.0 Support** | Handles most WASM32 1.0 instructions including arithmetic, memory, control flow, and type conversions |
| **SIMD Operations** | Vector128 support for WASM SIMD instructions |
| **C# Interop** | Import C# methods into WASM and export WASM functions for C# to call |
| **Typed Wrappers** | Use `AsImplementation<T>` to create clean, strongly-typed C# interfaces over WASM exports |
| **Automatic Marshaling** | Strings and `Span<byte>` are automatically copied to/from WASM heap memory |
| **Memory Access** | Direct heap access via spans and pointers for advanced scenarios |
| **Optimization** | Constant folding and peephole optimization for cleaner IL output |

---

## ⚠️ Status: Experimental

> [!WARNING]
> This project is an experiment and not production-ready.

It is capable of converting most instructions from WASM32 1.0 to IL. WASI (WebAssembly System Interface) is not implemented—you must provide your own C# implementations for system APIs via the import module mechanism.

---

## Quick Start

```csharp
using Wasm2IL;

// Load and convert WASM to .NET assembly
var transformer = new Transformer();
var asm = transformer.LoadWasmAssembly("mymodule.wasm", "MyModule");

// Call WASM functions directly
int result = (int)asm.Invoke("add", 5, 3);

// Or use typed interfaces for a cleaner API
public interface IMyModule
{
    [Wasm("add")]
    int Add(int a, int b);
}

var module = asm.AsImplementation<IMyModule>();
Console.WriteLine(module.Add(5, 3)); // Output: 8
```

---

## Installation

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [wabt](https://github.com/WebAssembly/wabt) (WebAssembly Binary Toolkit) - provides `wat2wasm` for compiling `.wat` files
- (Optional) clang with WebAssembly target support - only needed for building test WASM files from C

#### Installing Prerequisites

**Ubuntu/Debian:**
```bash
sudo apt-get update
sudo apt-get install -y wabt clang lld
```

**macOS (Homebrew):**
```bash
brew install wabt llvm
```

**Windows:**
Download wabt binaries from the [releases page](https://github.com/WebAssembly/wabt/releases).

### Building

```bash
# Clone the repository
git clone https://github.com/rolfrm/Wasm2IL.git
cd Wasm2IL

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Running Tests

```bash
# Run unit tests
dotnet run --project Wasm2IL.UnitTests/Wasm2IL.UnitTests.csproj
```

> [!NOTE]
> Some tests require additional test WASM files. To build `callback_test.wasm`:

```bash
# Download libc.wasm dependency
curl -L -o TestCCode/libc.wasm https://github.com/rolfrm/minlibc/releases/download/prototype1/libc.wasm

# Build test wasm file
cd TestCCode
make callback_test.wasm
```

### Using as a Library

Add the NuGet package to your project:

```bash
dotnet add package Wasm2IL --version 0.1.0-alpha.10
```

Or add it to your `.csproj`:

```xml
<PackageReference Include="Wasm2IL" Version="0.1.0-alpha.10" />
```

---

## Compiling C to WebAssembly with Clang

This section provides a comprehensive guide to compiling C code to WebAssembly using Clang, including optimization levels and recommended flags.

### Basic Compilation Command

```bash
clang --target=wasm32-unknown-unknown -nostdlib -Wl,--no-entry -Wl,--export-dynamic \
    -o output.wasm input.c
```

### Essential Compiler Flags

| Flag | Description |
|------|-------------|
| `--target=wasm32-unknown-unknown` | Target 32-bit WebAssembly (required) |
| `--target=wasm64-unknown-unknown` | Target 64-bit WebAssembly (experimental) |
| `-nostdlib` | Don't link the standard library |
| `-Wl,--no-entry` | No `_start` entry point required |
| `-Wl,--export-dynamic` | Export functions with `visibility("default")` |
| `-Wl,--export-all` | Export all symbols (use sparingly) |
| `-Wl,--allow-undefined` | Allow unresolved imports (for runtime linking) |
| `-Wl,--import-memory` | Import memory from host environment |
| `-Wl,--initial-memory=N` | Set initial memory size in bytes (must be multiple of 65536) |
| `-Wl,--max-memory=N` | Set maximum memory size in bytes |

### Optimization Levels

Clang supports several optimization levels, all of which work with the WebAssembly target:

| Level | Description | Use Case |
|-------|-------------|----------|
| `-O0` | No optimization | Debugging, fastest compile time |
| `-O1` | Basic optimizations | Quick builds with some optimization |
| `-O2` | Standard optimizations | **Recommended for most use cases** |
| `-O3` | Aggressive optimizations | Maximum performance, larger code size |
| `-Os` | Optimize for size | Small binary, good performance |
| `-Oz` | Aggressive size optimization | Smallest binary, may sacrifice performance |

#### What Each Optimization Level Does

**`-O0` (No Optimization)**
- No optimization passes applied
- Fastest compilation
- Generates larger, slower code
- Best for debugging since code maps directly to source

**`-O1` (Basic Optimization)**
- Enables basic optimizations that don't significantly increase compile time
- Dead code elimination
- Constant folding and propagation
- Simple loop optimizations

**`-O2` (Standard Optimization)** ✓ Recommended
- All `-O1` optimizations plus:
- Function inlining for small functions
- Loop unrolling (moderate)
- Vectorization where beneficial
- Better register allocation
- Best balance of performance and code size

**`-O3` (Aggressive Optimization)**
- All `-O2` optimizations plus:
- Aggressive function inlining
- Loop vectorization
- Unrolling more loops
- May significantly increase code size
- Can sometimes be slower due to cache effects

**`-Os` (Size Optimization)**
- Similar to `-O2` but prioritizes smaller code
- Disables optimizations that increase code size
- Useful when binary size matters (web delivery)
- Generally good performance

**`-Oz` (Minimum Size)**
- Most aggressive size reduction
- May disable inlining entirely
- Useful for very constrained environments
- Performance may suffer

### WebAssembly-Specific Optimizations

#### SIMD Support

Enable WebAssembly SIMD for vectorized operations:

```bash
clang --target=wasm32-unknown-unknown -msimd128 -O2 -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

| Flag | Description |
|------|-------------|
| `-msimd128` | Enable 128-bit SIMD operations |
| `-mrelaxed-simd` | Enable relaxed SIMD (non-deterministic, faster) |

> [!NOTE]
> wasm2il supports SIMD instructions and maps them to .NET's `Vector128<T>` operations.

#### Bulk Memory Operations

Enable efficient memory operations:

```bash
clang --target=wasm32-unknown-unknown -mbulk-memory -O2 -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

| Flag | Description |
|------|-------------|
| `-mbulk-memory` | Enable bulk memory operations (memory.copy, memory.fill) |
| `-mbulk-memory-opt` | Additional bulk memory optimizations |

### Link-Time Optimization (LTO)

LTO provides additional optimization by analyzing the entire program at link time:

```bash
clang --target=wasm32-unknown-unknown -O2 -flto -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

Benefits of LTO:
- Cross-file inlining
- Better dead code elimination
- Whole program constant propagation
- Can significantly reduce binary size

### Recommended Compilation Commands

**For Development/Debugging:**
```bash
clang --target=wasm32-unknown-unknown -O0 -g -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

**For Production (balanced):**
```bash
clang --target=wasm32-unknown-unknown -O2 -flto -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

**For Maximum Performance:**
```bash
clang --target=wasm32-unknown-unknown -O3 -flto -msimd128 -mbulk-memory -nostdlib \
    -Wl,--no-entry -Wl,--export-dynamic -o output.wasm input.c
```

**For Minimum Size (web delivery):**
```bash
clang --target=wasm32-unknown-unknown -Oz -flto -nostdlib -Wl,--no-entry \
    -Wl,--export-dynamic -o output.wasm input.c
```

### Using wasm-opt for Additional Optimization

After compiling with Clang, you can use `wasm-opt` from [Binaryen](https://github.com/WebAssembly/binaryen) for further optimization:

```bash
# Install Binaryen
# Ubuntu/Debian: sudo apt-get install binaryen
# macOS: brew install binaryen

# Optimize the WASM file
wasm-opt -O3 -o output-optimized.wasm output.wasm

# Optimize for size
wasm-opt -Oz -o output-small.wasm output.wasm
```

`wasm-opt` provides WebAssembly-specific optimizations beyond what Clang offers:
- Dead code elimination at the WASM level
- Local variable coalescing
- Stack slot optimization
- Memory access optimization
- Control flow simplification

---

## Complete Example: Wrapping a C Library

This section demonstrates how to compile C code to WASM, convert it to a .NET DLL, and use it from C#.

### Step 1: Write C Code with Exports and Imports

Create a file `mathlib.c`:

```c
// Define exported functions with visibility attribute
__attribute__((visibility("default")))
int add(int a, int b) {
    return a + b;
}

__attribute__((visibility("default")))
int multiply(int a, int b) {
    return a * b;
}

// Import a function from an external module ("env")
// This will be implemented in C#
__attribute__((import_module("env"), import_name("log_value")))
void log_value(int value);

__attribute__((visibility("default")))
int compute_and_log(int a, int b) {
    int result = add(a, b) * multiply(a, b);
    log_value(result);  // Calls into C#
    return result;
}
```

For functions that need filesystem or other system APIs:

```c
// Import filesystem functions from "fs" module
__attribute__((import_module("fs"), import_name("open")))
int fs_open(const char *path, int flags);

__attribute__((import_module("fs"), import_name("read")))
int fs_read(int fd, char *buf, int count);

__attribute__((import_module("fs"), import_name("close")))
int fs_close(int fd);

__attribute__((visibility("default")))
int read_file_size(const char *path) {
    int fd = fs_open(path, 0);
    if (fd < 0) return -1;

    char buf[4096];
    int total = 0;
    int n;
    while ((n = fs_read(fd, buf, sizeof(buf))) > 0) {
        total += n;
    }
    fs_close(fd);
    return total;
}
```

### Step 2: Compile C to WebAssembly

Use clang to compile your C code to WASM:

```bash
# Basic compilation
clang --target=wasm32-unknown-unknown -nostdlib -Wl,--no-entry -Wl,--export-dynamic \
    -o mathlib.wasm mathlib.c

# With optimizations
clang --target=wasm32-unknown-unknown -O2 -nostdlib -Wl,--no-entry -Wl,--export-dynamic \
    -o mathlib.wasm mathlib.c
```

**Key compiler flags:**
- `--target=wasm32-unknown-unknown` - Target WebAssembly 32-bit
- `-nostdlib` - Don't link standard library (or link a WASM-compatible one)
- `-Wl,--no-entry` - No `_start` entry point required
- `-Wl,--export-dynamic` - Export functions marked with visibility("default")

If you need libc functions (malloc, strlen, etc.), link against a WASM libc:

```bash
# Download a minimal libc
curl -L -o libc.wasm https://github.com/rolfrm/minlibc/releases/download/prototype1/libc.wasm

# Compile with libc
clang --target=wasm32-unknown-unknown -O2 -Wl,--no-entry -Wl,--export-dynamic \
    libc.wasm -o mathlib.wasm mathlib.c
```

### Step 3: Implement Imported APIs in C#

Create a static class with methods matching the imported function signatures:

```csharp
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Implementation for "env" module imports
public static class EnvModule
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void log_value(int value)
    {
        Console.WriteLine($"[WASM] Value: {value}");
    }
}

// Implementation for "fs" module imports
public static class FsModule
{
    private static readonly Dictionary<int, FileStream> _handles = new();
    private static int _nextHandle = 3; // 0,1,2 reserved for stdin/out/err

    public static int open(CString path, int flags)
    {
        try
        {
            var pathStr = path.ToString();
            var mode = flags == 0 ? FileMode.Open : FileMode.OpenOrCreate;
            var access = flags == 0 ? FileAccess.Read : FileAccess.ReadWrite;
            var stream = new FileStream(pathStr, mode, access);
            var handle = _nextHandle++;
            _handles[handle] = stream;
            return handle;
        }
        catch
        {
            return -1;
        }
    }

    public static unsafe int read(int fd, byte* buf, int count)
    {
        if (!_handles.TryGetValue(fd, out var stream))
            return -1;

        var span = new Span<byte>(buf, count);
        return stream.Read(span);
    }

    public static int close(int fd)
    {
        if (!_handles.TryGetValue(fd, out var stream))
            return -1;

        stream.Close();
        _handles.Remove(fd);
        return 0;
    }
}
```

> [!TIP]
> The `CString` type is provided by Wasm2IL for null-terminated string parameters.

### Step 4: Load WASM and Register Import Modules

```csharp
using Wasm2IL;

// Create transformer and register import implementations
var transformer = new Transformer();
transformer.LoadImportModule("env", typeof(EnvModule));
transformer.LoadImportModule("fs", typeof(FsModule));

// Load and convert WASM to .NET assembly
var asm = transformer.LoadWasmAssembly(
    "mathlib.wasm",    // Input WASM file
    "MathLib"          // Assembly name
);

// Optionally save the DLL for later use
transformer.Transform(
    File.OpenRead("mathlib.wasm"),
    "MathLib",
    File.Create("MathLib.dll")
);
```

> [!TIP]
> You can also load a pre-compiled DLL directly without re-transforming:

```csharp
// Load an existing DLL created by wasm2il
var asm = new WasmAssembly(Assembly.LoadFile("MathLib.dll"));
```

### Step 5: Call WASM Functions Directly

You can invoke WASM functions using reflection or the helper methods:

```csharp
// Using the Invoke helper (handles marshaling automatically)
int sum = (int)asm.Invoke("add", 5, 3);           // Returns 8
int product = (int)asm.Invoke("multiply", 4, 7);   // Returns 28
int result = (int)asm.Invoke("compute_and_log", 2, 3);  // Logs and returns 30

// Using reflection directly
var moduleType = asm.Assembly.ExportedTypes.First();
var addMethod = moduleType.GetMethod("add");
int sum2 = (int)addMethod.Invoke(null, new object[] { 10, 20 });
```

### Step 6: Wrap as a Typed C# Interface

For a cleaner API, define an interface and use `AsImplementation<T>`:

```csharp
using Wasm2IL;

// Define your API interface
public interface IMathLib
{
    [Wasm("add")]
    int Add(int a, int b);

    [Wasm("multiply")]
    int Multiply(int a, int b);

    [Wasm("compute_and_log")]
    int ComputeAndLog(int a, int b);
}

// For APIs that work with strings or byte arrays
public interface IFileLib
{
    // String parameters are automatically marshaled to WASM heap
    [Wasm("read_file_size")]
    int ReadFileSize(string path);
}

// Create the typed wrapper
var mathLib = asm.AsImplementation<IMathLib>();
var fileLib = asm.AsImplementation<IFileLib>();

// Use like normal C# code
int sum = mathLib.Add(10, 20);
int product = mathLib.Multiply(5, 6);
int size = fileLib.ReadFileSize("/path/to/file.txt");
```

**Supported parameter types for `AsImplementation<T>`:**
- Primitive types (`int`, `long`, `float`, `double`)
- `string` - Automatically copied to WASM heap, freed after call
- `Span<byte>` - Copied to heap, changes copied back after call
- `ReadOnlySpan<byte>` - Copied to heap (read-only)
- Pointer types (`byte*`, `int*`) - Direct memory access

### Step 7: Working with WASM Memory

For advanced scenarios, you can directly access and manipulate WASM linear memory.

> [!IMPORTANT]
> `Malloc` and `Free` require your WASM module to export `malloc` and `free` functions.
> This is typically done by linking a libc (e.g., the `libc.wasm` shown in Step 2).

```csharp
// Allocate memory in WASM heap (requires exported malloc)
int ptr = asm.Malloc(100);

// Get a span view of the heap
Span<byte> heap = asm.GetHeap();
Span<byte> slice = asm.GetHeapSpan(ptr, 100);

// Write data to heap
slice[0] = 0x42;
slice[1] = 0x43;

// Copy a string to WASM heap
int strPtr = asm.StringToHeap("Hello, WASM!");

// Read a string from WASM heap
string str = asm.GetHeapString(strPtr);

// Read a struct from heap
var value = asm.GetHeapObject<int>(ptr);

// Free allocated memory
asm.Free(ptr);
asm.Free(strPtr);
```

---

### Complete Working Example

```csharp
using Wasm2IL;

public interface IMyWasm
{
    [Wasm("add")]
    int Add(int a, int b);

    [Wasm]  // Uses method name as export name
    int multiply(int a, int b);
}

public static class EnvModule
{
    public static void log_value(int value) => Console.WriteLine($"Value: {value}");
}

class Program
{
    static void Main()
    {
        // Setup
        var transformer = new Transformer();
        transformer.LoadImportModule("env", typeof(EnvModule));

        // Load WASM
        var asm = transformer.LoadWasmAssembly("mathlib.wasm", "MathLib");

        // Get typed interface
        var math = asm.AsImplementation<IMyWasm>();

        // Use it
        Console.WriteLine($"5 + 3 = {math.Add(5, 3)}");
        Console.WriteLine($"4 * 7 = {math.multiply(4, 7)}");
    }
}
```

---

## Contributing

Contributions are welcome! Feel free to:

- Report bugs by opening an issue
- Suggest features or improvements
- Submit pull requests

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Made for the .NET and WebAssembly communities**

</div>
