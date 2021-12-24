#/bin/zsh
clang --target=wasm32-wasi -Wl,--no-entry, -Wl,--export-all -D_WASI_EMULATED_MMAN -fdeclspec \
             -DSQLITE_THREADSAFE=0 -DSQLITE_OMIT_LOAD_EXTENSION -o Code1.wasm -Os    -flto\
             --sysroot /Users/bruger/code/wasi-libc/sysroot Code1.c -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_THREADSAFE=0 sqlite3.c 
wasm2wat Code1.wasm > Code1.wat
cp Code1.wasm ../bin/Debug/net6.0/