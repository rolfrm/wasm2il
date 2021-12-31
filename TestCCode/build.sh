#/bin/zsh
#wat2wasm import.wat
clang --target=wasm32-wasi -Wl,--no-entry, -OMIT_LOAD_EXTENSION -o Code1.wasm -Os    -flto\
             --sysroot /Users/bruger/code/wasi-libc/sysroot -Wl,--export-all -D_WASI_EMULATED_MMAN -fdeclspec \
             -DSQLITE_THREADSAFE=0 -DSQLITE_TEST sqlite3.bc Code1.c -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_TEST  -DSQLITE_DEBUG -DSQLITE_THREADSAFE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_FORCE_OS_TRACE 
              
wasm2wat Code1.wasm > Code1.wat
cp Code1.wasm ../bin/Debug/net6.0/