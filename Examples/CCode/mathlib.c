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

// Simple factorial function (iterative)
__attribute__((visibility("default")))
int factorial(int n) {
    int result = 1;
    for (int i = 1; i <= n; i++) {
        result *= i;
    }
    return result;
}

// Simple heap pointer for malloc
static int heap_ptr = 1024;

// Simple malloc implementation for demo
__attribute__((visibility("default")))
int malloc(int size) {
    int ptr = heap_ptr;
    heap_ptr += size;
    return ptr;
}

// Simple free (just a no-op for demo)
__attribute__((visibility("default")))
void free(int ptr) {
    // no-op for simplicity
}

// Function to get string length (for testing string marshaling)
__attribute__((visibility("default")))
int string_length(const char* ptr) {
    int len = 0;
    while (ptr[len] != '\0') {
        len++;
    }
    return len;
}
