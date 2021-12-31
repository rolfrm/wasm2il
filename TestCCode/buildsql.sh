#/bin/zsh
#wat2wasm import.wat
clang --target=wasm32-wasi -DSQLITE_OMIT_LOAD_EXTENSION -c -o sqlite3.bc -Oz\
             --sysroot /Users/bruger/code/wasi-libc/sysroot  -fdeclspec \
             -DSQLITE_THREADSAFE=0 -DSQLITE_TEST -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_TEST  -DSQLITE_DEBUG -DSQLITE_THREADSAFE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_FORCE_OS_TRACE  sqlite3.c