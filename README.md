# wasm2il
WebAssembly to .NET IL bytecode compiler

**License: MIT**

## Status: Experiment

This is currently an experiment and not a finished application.

It is capable of converting most instructions from WASM32 1.0 to IL.

Implementing a WASI-compliant set of functions is something that needs to be solved. 

## Compiling SQLite to IL
I have successfully gotten SQLite to work in .NET, but only in the ":MEMORY:". WASI-compliant system calls needs to be supported.
