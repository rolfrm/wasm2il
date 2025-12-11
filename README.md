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
