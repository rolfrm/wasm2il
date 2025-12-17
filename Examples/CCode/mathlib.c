// Compile: clang --target=wasm32 -nostdlib -Wl,--no-entry -Wl,--export-dynamic -o mathlib.wasm mathlib.c

// Import a logging function from the "env" module (implemented in C#)
__attribute__((import_module("env"), import_name("log_value")))
void log_value(int value);

// Basic addition function
__attribute__((visibility("default")))
int add(int a, int b) {
    return a + b;
}

// Basic multiplication function
__attribute__((visibility("default")))
int multiply(int a, int b) {
    return a * b;
}

// Function that uses the imported log_value
__attribute__((visibility("default")))
int compute_and_log(int a, int b) {
    int result = add(a, b) * multiply(a, b);
    log_value(result);
    return result;
}
