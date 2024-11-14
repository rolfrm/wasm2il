(module
   (import "console" "log" (func $log (param i32)))
  (import "env" "strlen" (func $strlen (param i32) (result i32)))	
  (import "env" "memcpy" (func $memcpy (param i32)(param i32)(param i32) (result i32)))	
 (import "env" "memfill" (func $memfill (param i32)(param i32)(param i32)))	
	(func $multiply (param $lhs i32) (param $rhs i32) (result i32)
    local.get $lhs
    local.get $rhs
    i32.mul)
     (func $multiply_vec (param $lhs v128) (param $rhs v128) (result v128)
        ;; Perform element-wise multiplication on the vectors
        local.get $lhs
        local.get $rhs
        f32x4.mul
      )
      
           (func $try_vec (result v128)
              (local $vec v128)
              ;; Perform element-wise multiplication on the vectors
              (v128.const i8x16 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16)
              (local.set $vec)
              (local.get $vec)
              (local.get $vec)
              (i8x16.shuffle 15 31 14 30 13 29 9 8 7 6 5 4 3 2 1 0)
            )
      
  (func $incf (result i32)
     global.get $a
	 i64.const 1
	 f32.const 2.0
 	 f64.const 3.0
	 drop
	 drop
	 drop
	 i32.const 1
	 i32.add
	 global.set $a
	 global.get $a
	 )
  (func $testLog (param $a i32) (result i32)
     local.get $a
     local.get $a
     call $log)
  
  (func $get_strlen (param $a i32) (result i32)
    local.get $a
    call $strlen
  )   
  
  (func $malloc (param $s i32) (result i32)
     (local $offset i32)
     global.get $heap_ptr
     local.set $offset
     local.get $offset
     local.get $s 
     i32.add
     global.set $heap_ptr
     local.get $offset
  )
  
    (func $test_memcpy (param $dst i32) (param $src i32) (param $n i32) (result i32)
       (local.get $dst)
       (local.get $src)
       (local.get $n)
       (call $memcpy)
    )
  
  (func $test_memfill (param $count i32) (param $value i32) (result i32)
         (local $p1 i32)
         (local.get $count)
         (call $malloc)
         (local.set $p1)
         (local.get $p1)
         (local.get $value)
         (local.get $count)
         (call $memfill)
         (local.get $p1)
      )
      
        (func $test_memfill2 (param $count i32) (param $value i32) (result i32)
        (local $p i32)
        (local $p2 i32)
        (local.get $count)
        (call $malloc)
        (local.set $p)
        (local.get $count)
        (call $malloc)
        (local.set $p2)
        (local.get $p)
               
        (local.get $value)    ;; Value to fill
        (local.get $count)    ;; Number of bytes to fill
        memory.fill
            
        (local.get $p2) ;;dst
        (local.get $p)     ;;src
        (local.get $count)   
        memory.copy
        
        (local.get $p2) )
            
  
  (func $free (param $ptr i32)
    ;; just leak it :)
  )
  
  (export "multiply" (func $multiply))
  (export "multiply_vec" (func $multiply_vec))
  (export "incf" (func $incf))
  (export "testLog" (func $testLog))
  (export "try_vec" (func $try_vec))
  (export "get_strlen" (func $get_strlen))
  (export "malloc" (func $malloc))
  (export "free" (func $free))
  (export "test_memcpy" (func $test_memcpy))
  (export "test_memfill" (func $test_memfill))
  (export "test_memfill2" (func $test_memfill2))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  (global $heap_ptr (mut i32) (i32.const 1024))
  (memory $mem 4) 
  (data (i32.const 0) "Hello, World!")
  )
