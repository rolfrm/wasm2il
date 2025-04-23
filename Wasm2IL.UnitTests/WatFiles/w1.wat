(module
   (import "console" "log" (func $log (param i32)))
  (import "env" "strlen" (func $strlen (param i32) (result i32)))	
  (import "env" "memcpy" (func $memcpy (param i32)(param i32)(param i32) (result i32)))	
  (import "env" "assert" (func $assert (param i32)))
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
  
  (func $test_v128_load32_lane
      (local $v v128)
      
      (local.set $v (v128.const i32x4 3 3 3 3))
      (v128.load32_lane 2 (i32.const 53) (local.get $v))
      (local.set $v)
         
      (call $assert
        (i32.eq
          (i32x4.extract_lane 2 (local.get $v))
          (i32.const 0x44)))
      
      (local.set $v (v128.const i32x4 1 2 3 43))
      (v128.store32_lane 3 (i32.const 53) (local.get $v))
      
      (call $assert
        (i32.eq
          (i32x4.extract_lane 3 (local.get $v))
          (i32.const 43)))
             
    )
    
     (func $test_v128_load8_splat
         (local $v v128)
         (local.set $v (v128.load8_splat (i32.const 50)))
         (call $assert
               (i64.eq
                 (i64x2.extract_lane 1 (local.get $v))
                 (i64.const 0x0404040404040404)
               ))
          (call $assert
             (i8x16.all_true
             (i8x16.eq 
                (i8x16.splat (i32.const 4))
                        (local.get $v)
                        )))
     )
     
     (func $test_i32x4_replace_lane
         (local $v v128)
        
         (local.set $v (v128.const i32x4 1 2 3 4))
         (local.set $v (i32x4.replace_lane 2 (local.get $v) (i32.const 42)))
         (call $assert
               (i32.eq
                 (i32x4.bitmask
                   (i32x4.eq (local.get $v) (v128.const i32x4 1 2 42 4))
                 )
                 (i32.const 15) 
               )
             ))
    (func $test_v128_load64_zero
        (local $v v128)
    
        (local.set $v (v128.load64_zero (i32.const 100)))
    
        (call $assert
          (i64.eq
            (i64x2.extract_lane 0 (local.get $v))
            (i64.const 0x0807060504030201)
          )
          
        )
        (call $assert
          (i64.eq
            (i64x2.extract_lane 1 (local.get $v))
            (i64.const 0)
          )
        )
      )
      
      (func $test_i16x8_extend_low_i8x16_u
          (local $v v128)
              (local $result v128)
          
              ;; Initialize the vector with 16 bytes
              (local.set $v (v128.const i8x16 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16))
          
              ;; Extend the lower 8 bytes to 16-bit unsigned integers
              (local.set $result (i16x8.extend_low_i8x16_u (local.get $v)))
          
              ;; Check the lanes of the result
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 0 (local.get $result))
                  (i32.const 0x00020001)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 1 (local.get $result))
                  (i32.const 0x00040003)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 2 (local.get $result))
                  (i32.const 0x00060005)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 3 (local.get $result))
                  (i32.const 0x00080007)
                )
              )
          )
          
           (func $test_v128_not
              (local $v v128)
              (local $result v128)
          
              ;; Initialize the vector
              (local.set $v (v128.const i32x4 0xFFFFFFFF 0x00000000 0x12345678 0x87654321))
          
              ;; Perform bitwise NOT
              (local.set $result (v128.not (local.get $v)))
          
              ;; Assert results
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 0 (local.get $result))
                  (i32.const 0x00000000)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 1 (local.get $result))
                  (i32.const 0xFFFFFFFF)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 2 (local.get $result))
                  (i32.const 0xEDCBA987)
                )
              )
              (call $assert
                (i32.eq
                  (i32x4.extract_lane 3 (local.get $result))
                  (i32.const 0x789ABCDE)
                )
              )
            )
    
    (func $test
        call $test_v128_load32_lane
        call $test_v128_load8_splat
        call $test_i32x4_replace_lane
        call $test_v128_load64_zero
        call $test_i16x8_extend_low_i8x16_u
        call $test_v128_not
    )
    
    (func $pointerOffset (param $pointer i32) (result i32)
        (local.get $pointer)
    )
  
     
  
  (export "multiply" (func $multiply))
  (export "test_v128_load32_lane" (func $test_v128_load32_lane))
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
  (export "test" (func $test))
  (export "pointerOffset" (func $pointerOffset))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  (global $test_value (mut i32) (i32.const 0x05050505))
  (global $heap_ptr (mut i32) (i32.const 1024))
  (memory $mem 4) 
  (data (i32.const 0) "Hello, World!")
  
  (data (i32.const 50) "\04\00\00\44\00\00\00\88\99\AA\BB\CC\DD\EE\FF\00")
  (data (i32.const 100) "\01\02\03\04\05\06\07\08\11\22\33\44\55\66\77\88")
  )
