# wasm2il
WebAssembly to .NET IL bytecode compiler

**License: MIT**

## Status: Experiment

This is currently an experiment and not a finished application.

It is capable of converting most instructions from WASM32 1.0 to IL.

Implementing a WASI-compliant set of functions is something that needs to be solved.

## How to Run

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

Note: Some tests require additional test WASM files. To build `callback_test.wasm`:

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
clang --target=wasm32 -nostdlib -Wl,--no-entry -Wl,--export-dynamic \
    -o mathlib.wasm mathlib.c

# With optimizations
clang --target=wasm32 -O2 -nostdlib -Wl,--no-entry -Wl,--export-dynamic \
    -o mathlib.wasm mathlib.c
```

**Key compiler flags:**
- `--target=wasm32` - Target WebAssembly 32-bit
- `-nostdlib` - Don't link standard library (or link a WASM-compatible one)
- `-Wl,--no-entry` - No `_start` entry point required
- `-Wl,--export-dynamic` - Export functions marked with visibility("default")

If you need libc functions (malloc, strlen, etc.), link against a WASM libc:

```bash
# Download a minimal libc
curl -L -o libc.wasm https://github.com/rolfrm/minlibc/releases/download/prototype1/libc.wasm

# Compile with libc
clang --target=wasm32 -O2 -Wl,--no-entry -Wl,--export-dynamic \
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

**Note:** The `CString` type is provided by Wasm2IL for null-terminated string parameters.

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

**Note:** `Malloc` and `Free` require your WASM module to export `malloc` and `free` functions.
This is typically done by linking a libc (e.g., the `libc.wasm` shown in Step 2).

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
