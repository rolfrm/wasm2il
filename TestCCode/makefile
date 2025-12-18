SYSROOT=/Users/bruger/code/work/wasi-libc/sysroot
SYSROOT_LIB=$(SYSROOT)/lib/wasm32-wasi
#MUSL_INCLUDE=/Users/bruger/code/work/musl/include
MUSL_INCLUDE=/Users/bruger/code/minlibc/include
SSL_PATH=/Users/bruger/code/extern/openssl/include
all: sqlite3.wasm callback_test.wasm
# -msimd128  -mtail-call ./libc.wasm-mtail-call  -msimd128 -mbulk-memory -msimd128 

sqlite3.wasm: sqlite3.c sqlite3.h
	clang --target=wasm32-unknown-unknown -mrelaxed-simd -msimd128 -mtail-call -mbulk-memory -mbulk-memory-opt -flto=full -fbuiltin -nostdlib -Wl,--gc-sections -DSQLITE_OMIT_LOAD_EXTENSION -o sqlite3.wasm -O3 -g0\
	         -ffast-math -fno-math-errno -funsafe-math-optimizations -fstrict-aliasing -funroll-loops -fomit-frame-pointer\
             -I$(MUSL_INCLUDE) \
             -DSQLITE_THREADSAFE=0 -DSQLITE_OMIT_WAL -DHAVE_UTIME -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_OMIT_UTF16 -DSQLITE_OMIT_JSON -DSQLITE_OMIT_RTREE -DSQLITE_OMIT_GEOPOLY -DSQLITE_OMIT_FTS5 -DSQLITE_OMIT_DEPRECATED   \
              ./libc.wasm sqlite3.c -Wl,--allow-undefined -Wl,--no-entry\
              -Wl,--export=sqlite3_open -Wl,--export=sqlite3_open_v2 -Wl,--export=sqlite3_close -Wl,--export=sqlite3_exec -Wl,--export=sqlite3_prepare_v2 \
              -Wl,--export=sqlite3_step -Wl,--export=sqlite3_finalize -Wl,--export=sqlite3_errmsg -Wl,--export=sqlite3_free \
              -Wl,--export=sqlite3_column_text -Wl,--export=sqlite3_column_int -Wl,--export=sqlite3_column_int64 -Wl,--export=sqlite3_column_double  -Wl,--export=sqlite3_column_count \
              -Wl,--export=sqlite3_bind_text -Wl,--export=sqlite3_bind_int -Wl,--export=sqlite3_bind_int64 -Wl,--export=sqlite3_bind_double -Wl,--export=sqlite3_reset -Wl,--export=sqlite3_changes\
              -Wl,--export=sqlite3_memory_used -Wl,--export=sqlite3_column_blob -Wl,--export=sqlite3_column_type -Wl,--export=sqlite3_bind_blob, -Wl,--export=sqlite3_column_bytes\
              -Wl,--export=malloc -Wl,--export=free
              
sqlite3.bc: sqlite3.c sqlite3.h
	clang --target=wasm32-unknown-unknown -c -msimd128  -mbulk-memory -mbulk-memory-opt -flto -fbuiltin -mllvm -inline-threshold=1000 -nostdlib -Wl,--gc-sections -DSQLITE_OMIT_LOAD_EXTENSION -o sqlite3.bc -O3 -g0\
             -I$(MUSL_INCLUDE)  \
             -DSQLITE_THREADSAFE=0 -DHAVE_UTIME -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_OMIT_UTF16 -DSQLITE_OMIT_JSON -DSQLITE_OMIT_RTREE -DSQLITE_OMIT_GEOPOLY -DSQLITE_OMIT_FTS5 -DSQLITE_OMIT_DEPRECATED   \
              sqlite3.c -Wl,--allow-undefined 
              
sqlite3.wat: sqlite3.wasm libc.wasm
	wasm2wat sqlite3.wasm > sqlite3.wat
    
simple_call.wasm: simple_call.c
	clang --target=wasm32-unknown-unknown -nostdlib -O3  -Wl,--export=main -Wl,--export=__provide_overrides__ -Wl,--allow-undefined -Wl,--no-entry -ffreestanding simple_call.c -o simple_call.wasm
     
simple_call2.wasm: simple_call2.c
	clang --std=c23 --target=wasm32-unknown-unknown -nostdlib -O3 -mmultimemory  -Wl,--export=main -Wl,--export=__provide_overrides2__ -Wl,--export=__provide_overrides__ -Wl,--export=__caller_heap__ -Wl,--allow-undefined -Wl,--no-entry -ffreestanding simple_call2.c -o simple_call2.wasm
	 
callback_test.wasm: callback_test.c
	clang --std=c23 --target=wasm32-unknown-unknown -nostdlib -O3 -mmultimemory  -Wl,--export=main-Wl,--export=callback_test -Wl,--allow-undefined -Wl,--no-entry -ffreestanding callback_test.c -o callback_test.wasm 
  
code1.bc: code1.c
	clang --target=wasm32-wasi -v -c -o code1.bc -Oz code1.c -fdeclspec\
             --sysroot $(SYSROOT) 

code.wasm: code1.bc sqlite3.bc
	wasm-ld -export-all --allow-undefined -error-limit=0 -L$(SYSROOT)/lib/wasm32-wasi/ $(SYSROOT_LIB)/libc.a  $^  $(SYSROOT_LIB)/crt1.o  -o code.wasm --no-entry 