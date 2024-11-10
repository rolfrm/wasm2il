SYSROOT=/Users/bruger/code/work/wasi-libc/sysroot
SYSROOT_LIB=$(SYSROOT)/lib/wasm32-wasi
#MUSL_INCLUDE=/Users/bruger/code/work/musl/include
MUSL_INCLUDE=/Users/bruger/code/minlibc/include
all: ../bin/Debug/net6.0/code.wasm

sqlite3.wasm: sqlite3.c sqlite3.h
	clang --target=wasm32-unknown-unknown  -nostdlib -DSQLITE_OMIT_LOAD_EXTENSION -o sqlite3.wasm -Os\
             -I$(MUSL_INCLUDE) -fdeclspec \
             -DSQLITE_THREADSAFE=0 -DSQLITE_TEST -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_THREADSAFE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_FORCE_OS_TRACE -D_WASI_EMULATED_GETPID \
             ./libc.wasm sqlite3.c -Wl,--allow-undefined -v -Wl,--no-entry 

sqlite3.wat: sqlite3.wasm
	wasm2wat sqlite3.wasm > sqlite3.wat
    
code1.bc: Code1.c
	clang --target=wasm32-wasi -c -o code1.bc -Oz Code1.c -fdeclspec\
             --sysroot $(SYSROOT)

#code.wasm: code1.bc sqlite3.bc
#	wasm-ld -export-all --allow-undefined -error-limit=0 -L$(SYSROOT)/lib/wasm32-wasi/ $(SYSROOT_LIB)/libc.a  $^  $(SYSROOT_LIB)/crt1.o  -o code.wasm -no-entry 

../bin/Debug/net6.0/code.wasm: code.wasm
	cp code.wasm ../bin/Debug/net6.0/code.wasm