SYSROOT=/Users/bruger/code/work/wasi-libc/sysroot
SYSROOT_LIB=$(SYSROOT)/lib/wasm32-wasi
#MUSL_INCLUDE=/Users/bruger/code/work/musl/include
MUSL_INCLUDE=/Users/bruger/code/minlibc/include
all: ../bin/Debug/net6.0/code.wasm
# -msimd128  -mtail-call ./libc.wasm-mtail-call  -msimd128 -mbulk-memory -msimd128 
sqlite3.wasm: sqlite3.c sqlite3.h
	clang --target=wasm32-unknown-unknown -msimd128  -mbulk-memory -flto -fbuiltin -mllvm -inline-threshold=1000 -nostdlib -Wl,--gc-sections -DSQLITE_OMIT_LOAD_EXTENSION -o sqlite3.wasm -O3\
             -I$(MUSL_INCLUDE)  -fvisibility=hidden \
             -DSQLITE_THREADSAFE=0 -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_THREADSAFE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT \
              ./libc.wasm sqlite3.c -Wl,--allow-undefined -Wl,--no-entry\
              -Wl,--export=sqlite3_open -Wl,--export=sqlite3_open_v2 -Wl,--export=sqlite3_close -Wl,--export=sqlite3_exec -Wl,--export=sqlite3_prepare_v2 \
              -Wl,--export=sqlite3_step -Wl,--export=sqlite3_finalize -Wl,--export=sqlite3_errmsg -Wl,--export=sqlite3_free \
              -Wl,--export=sqlite3_column_text -Wl,--export=sqlite3_column_int -Wl,--export=sqlite3_column_count \
              -Wl,--export=sqlite3_bind_text -Wl,--export=sqlite3_bind_int -Wl,--export=sqlite3_reset -Wl,--export=sqlite3_changes\
              -Wl,--export=sqlite3_memory_used
              

sqlite3.wat: sqlite3.wasm
	wasm2wat sqlite3.wasm > sqlite3.wat
    
code1.bc: Code1.c
	clang --target=wasm32-wasi -c -o code1.bc -Oz Code1.c -fdeclspec\
             --sysroot $(SYSROOT)

#code.wasm: code1.bc sqlite3.bc
#	wasm-ld -export-all --allow-undefined -error-limit=0 -L$(SYSROOT)/lib/wasm32-wasi/ $(SYSROOT_LIB)/libc.a  $^  $(SYSROOT_LIB)/crt1.o  -o code.wasm -no-entry 

../bin/Debug/net6.0/code.wasm: code.wasm
	cp code.wasm ../bin/Debug/net6.0/code.wasm