SYSROOT=/Users/bruger/code/wasi-libc/sysroot
SYSROOT_LIB=$(SYSROOT)/lib/wasm32-wasi
all: ../bin/Debug/net6.0/code.wasm

sqlite3.bc: sqlite3.c sqlite3.h
	clang --target=wasm32-wasi -DSQLITE_OMIT_LOAD_EXTENSION -c -o sqlite3.bc -Os\
             --sysroot $(SYSROOT)  -fdeclspec \
             -DSQLITE_THREADSAFE=0 -DSQLITE_TEST -DSQLITE_OMIT_RANDOMNESS -DHAVE_UTIME -DSQLITE_OMIT_WAL -DSQLITE_MAX_MMAP_SIZE=0 -DSQLITE_TEST  -DSQLITE_DEBUG -DSQLITE_THREADSAFE=0 -DSQLITE_ENABLE_SETLK_TIMEOUT -DSQLITE_FORCE_OS_TRACE  sqlite3.c
code1.bc: Code1.c
	clang --target=wasm32-wasi -c -o code1.bc -Oz Code1.c -fdeclspec\
             --sysroot $(SYSROOT)

code.wasm: code1.bc sqlite3.bc
	wasm-ld -export-all --allow-undefined -error-limit=0 -L$(SYSROOT)/lib/wasm32-wasi/ $(SYSROOT_LIB)/libc.a  $^  $(SYSROOT_LIB)/crt1.o  -o code.wasm -no-entry 

../bin/Debug/net6.0/code.wasm: code.wasm
	cp code.wasm ../bin/Debug/net6.0/code.wasm