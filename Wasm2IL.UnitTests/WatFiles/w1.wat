(module
   (import "console" "log" (func $log (param i32)))
  (import "env" "strlen" (func $strlen (param i32) (result i32)))	
  (import "env" "memcpy" (func $memcpy (param i32)(param i32)(param i32) (result i32)))	
  (import "env" "assert" (func $assert (param i32)))
  (import "env" "assert2" (func $assert2 (param i32) (param i32)))
    
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
  
  (func $test_small_bits
      (i32.const 0) 
      (i32.load8_s)
      (i32.const 72)
      (i32.eq)
      (call $assert)
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
             (func $asserteqi32 (param $x i32) (param $y i32)
                    (call $assert (i32.eq (local.get $y) (local.get $x)))
                  )
      (func $asserteqi64 (param $x i64) (param $y i64)
        (call $assert (i64.eq (local.get $y) (local.get $x)))
      )
      (func $asserteqf32 (param $a f32) (param $b f32)
          (call $assert (f32.eq (local.get $a) (local.get $b))) )
          
      (func $asserteqf64 (param $a f64) (param $b f64)
          (call $assert (f64.eq (local.get $a) (local.get $b))) )
     ;; Test function for all truncation operations
      (func $test_trunc
        (param $f32 f32) (param $f64 f64) (param $i32 i32) (param $i64 i64)
        ;; f32 to i32 (signed/unsigned)
        (call $asserteqi32 (i32.trunc_sat_f32_s (local.get $f32)) (i32.const 42))  ;; f32 -> i32 (signed)
        (call $asserteqi32 (i32.trunc_sat_f32_u (local.get $f32)) (i32.const 42))  ;; f32 -> i32 (unsigned)
        ;; f64 to i32 (signed/unsigned)
        (call $asserteqi32 (i32.trunc_sat_f64_s (local.get $f64)) (i32.const 42))  ;; f64 -> i32 (signed)
        (call $asserteqi32 (i32.trunc_sat_f64_u (local.get $f64)) (i32.const 42))  ;; f64 -> i32 (unsigned)
        ;; f32 to i64 (signed/unsigned)
        (call $asserteqi64 (i64.trunc_sat_f32_s (local.get $f32)) (i64.const 42))  ;; f32 -> i64 (signed)
       (call $asserteqi64 (i64.trunc_sat_f32_u (local.get $f32)) (i64.const 42))  ;; f32 -> i64 (unsigned)
        ;; f64 to i64 (signed/unsigned)
        (call $asserteqi64 (i64.trunc_sat_f64_s (local.get $f64)) (i64.const 42))  ;; f64 -> i64 (signed)
        (call $asserteqi64 (i64.trunc_sat_f64_u (local.get $f64)) (i64.const 42))  ;; f64 -> i64 (unsigned)
        ;; i32 to f32 (signed/unsigned)
         (call $asserteqf32 (f32.convert_i32_s (local.get $i32)) (f32.const 42.0))  ;; i32 -> f32 (signed)
         (call $asserteqf32 (f32.convert_i32_u (local.get $i32)) (f32.const 42.0))  ;; i32 -> f32 (unsigned)
        ;; i64 to f32 (signed/unsigned)
        (call $asserteqf32 (f32.convert_i64_s (local.get $i64)) (f32.const 42.0))  ;; i64 -> f32 (signed)
        (call $asserteqf32 (f32.convert_i64_u (local.get $i64)) (f32.const 42.0))  ;; i64 -> f32 (unsigned)
        ;; i32 to f64 (signed/unsigned)
        (call $asserteqf64 (f64.convert_i32_s (local.get $i32)) (f64.const 42.0))  ;; i32 -> f64 (signed)
        (call $asserteqf64 (f64.convert_i32_u (local.get $i32)) (f64.const 42.0))  ;; i32 -> f64 (unsigned)
        ;; i64 to f64 (signed/unsigned)
        (call $asserteqf64 (f64.convert_i64_s (local.get $i64)) (f64.const 42.0))  ;; i64 -> f64 (signed)
        (call $asserteqf64 (f64.convert_i64_u (local.get $i64)) (f64.const 42.0))  ;; i64 -> f64 (unsigned)
      )
      
    
    
    (func $pointerOffset (param $pointer i32) (result i32)
        (local.get $pointer)
    )
    
    
       (func $fnv1a (param i32 i32 i32)
          (local i64)
          i64.const -3750763034362895579
          local.set 3
          block  ;; label = @1
            local.get 1
            i32.eqz
            br_if 0 (;@1;)
            loop  ;; label = @2
              local.get 3
              local.get 0
              i64.load8_u
              i64.xor
              i64.const 1099511628211
              i64.mul
              local.set 3
              local.get 0
              i32.const 1
              i32.add
              local.set 0
              local.get 1
              i32.const -1
              i32.add
              local.tee 1
              br_if 0 (;@2;)
            end
          end
          local.get 2
          local.get 3
          i64.store)
          
  (global $g1 (mut i32) (i32.const 100))
  (global $g2 i32 (i32.const 200))
   (func $helper (param $x i32) (result i32)
      local.get $x
      i32.const 10
      i32.mul
    )
(func $run_all_tests (export "run_all_tests")
    (local $result i32)
    (local $i i32)
    (local $a i32)
        (local $sum i32)
        (local $b i32)
            (local $x i32)
    ;; Integer Arithmetic - Add
    i32.const 42
    i32.const 8
    i32.add
    i32.const 50
    i32.eq
    i32.const 1
    call $assert2
    
    ;; Integer Arithmetic - Sub
    i32.const 100
    i32.const 42
    i32.sub
    i32.const 58
    i32.eq
    i32.const 2
    call $assert2
    
    ;; Integer Arithmetic - Mul
    i32.const 6
    i32.const 7
    i32.mul
    i32.const 42
    i32.eq
    i32.const 3
    call $assert2
    
    ;; Integer Arithmetic - Div signed
    i32.const -20
    i32.const 4
    i32.div_s
    i32.const -5
    i32.eq
    
    i32.const 4
    call $assert2
    
    ;; Integer Arithmetic - Div unsigned
    i32.const 20
    i32.const 4
    i32.div_u
    i32.const 5
    i32.eq
    
    i32.const 5
    call $assert2
    
    ;; Integer Arithmetic - Rem signed
    i32.const 23
    i32.const 5
    i32.rem_s
    i32.const 3
    i32.eq
    
    i32.const 6
    call $assert2
    
    ;; Integer Arithmetic - Rem unsigned
    i32.const 23
    i32.const 5
    i32.rem_u
    i32.const 3
    i32.eq
    
    i32.const 7
    call $assert2
    
    ;; Bitwise - AND
    i32.const 0xFF
    i32.const 0xF0
    i32.and
    i32.const 0xF0
    i32.eq
    
    i32.const 8
    call $assert2
    
    ;; Bitwise - OR
    i32.const 0x0F
    i32.const 0xF0
    i32.or
    i32.const 0xFF
    i32.eq
    
    i32.const 9
    call $assert2
    
    ;; Bitwise - XOR
    i32.const 0xFF
    i32.const 0x0F
    i32.xor
    i32.const 0xF0
    i32.eq
    
    i32.const 10
    call $assert2
    
    ;; Bitwise - SHL
    i32.const 1
    i32.const 8
    i32.shl
    i32.const 256
    i32.eq
    
    i32.const 11
    call $assert2
    
    ;; Bitwise - SHR signed
    i32.const -256
    i32.const 2
    i32.shr_s
    i32.const -64
    i32.eq
    
    i32.const 12
    call $assert2
    
    ;; Bitwise - SHR unsigned
    i32.const 256
    i32.const 2
    i32.shr_u
    i32.const 64
    i32.eq
    
    i32.const 13
    call $assert2
    
    ;; Bitwise - ROTL
    i32.const 0x80000001
    i32.const 1
    i32.rotl
    i32.const 0x00000003
    i32.eq
     
    i32.const 14
    call $assert2
    
    ;; Bitwise - ROTR
    i32.const 0x80000001
    i32.const 1
    i32.rotr
    i32.const 0xC0000000
    i32.eq
     
    i32.const 15
    call $assert2
    
    ;; Comparison - EQ
    i32.const 42
    i32.const 42
    i32.eq
    i32.const 1
    i32.eq
    
    i32.const 16
    call $assert2
    
    ;; Comparison - NE
    i32.const 42
    i32.const 43
    i32.ne
    i32.const 1
    i32.eq
    
    i32.const 17
    call $assert2
    
    ;; Comparison - LT signed
    i32.const -1
    i32.const 0
    i32.lt_s
    i32.const 1
    i32.eq
     
    i32.const 18
    call $assert2
    
    ;; Comparison - LT unsigned
    i32.const 10
    i32.const 20
    i32.lt_u
    i32.const 1
    i32.eq
    
    i32.const 19
    call $assert2
    
    ;; Comparison - GT signed
    i32.const 10
    i32.const 5
    i32.gt_s
    i32.const 1
    i32.eq
    
    i32.const 20
    call $assert2
    
    ;; Comparison - LE signed
    i32.const 10
    i32.const 10
    i32.le_s
    i32.const 1
    i32.eq
    
    i32.const 21
    call $assert2
    
    ;; Comparison - GE signed
    i32.const 10
    i32.const 5
    i32.ge_s
    i32.const 1
    i32.eq
    
    i32.const 22
    call $assert2
    
    ;; Unary - CLZ
    i32.const 0x00FF0000
    i32.clz
    i32.const 8
    i32.eq
    
    i32.const 23
    call $assert2
    
    ;; Unary - CTZ
    i32.const 0x00FF0000
    i32.ctz
    i32.const 16
    i32.eq
    
    i32.const 24
    call $assert2
    
    ;; Unary - POPCNT
    i32.const 0xFF
    i32.popcnt
    i32.const 8
    i32.eq
    
    i32.const 25
    call $assert2
    
    ;; Unary - EQZ
    i32.const 0
    i32.eqz
    i32.const 1
    i32.eq
    
    i32.const 26
    call $assert2
    
    ;; Control Flow - IF/THEN/ELSE
    i32.const 1
    if
      i32.const 100
      local.set $result
    else
      i32.const 200
      local.set $result
    end
    local.get $result
    i32.const 100
    i32.eq
    
    i32.const 27
    call $assert2
    
    ;; Control Flow - BLOCK/BR
    block $exit (result i32)
      i32.const 42
      br $exit
      i32.const 100
    end
    i32.const 42
    i32.eq
     
    i32.const 28
    call $assert2
    
    ;; Control Flow - LOOP
    
    i32.const 0
    local.set $sum
    i32.const 0
    local.set $i
    
    block $break
      loop $continue
        local.get $i
        i32.const 10
        i32.ge_s
        br_if $break
        
        local.get $sum
        local.get $i
        i32.add
        local.set $sum
        
        local.get $i
        i32.const 1
        i32.add
        local.set $i
        
        br $continue
      end
    end
    local.get $sum
    i32.const 45
    i32.eq
    
    i32.const 29
    call $assert2
    
    ;; Local Variables - GET/SET
    
    
    i32.const 10
    local.set $a
    i32.const 20
    local.set $b
    local.get $a
    local.get $b
    i32.add
    i32.const 30
    i32.eq
      
    i32.const 30
    call $assert2
    
    ;; Local Variables - TEE
    
    i32.const 42
    local.tee $x
    local.get $x
    i32.add
    i32.const 84
    i32.eq
    
    i32.const 31
    call $assert2
    
    ;; Memory - STORE/LOAD i32
    i32.const 0
    i32.const 0xDEADBEEF
    i32.store
    i32.const 0
    i32.load
    i32.const 0xDEADBEEF
    i32.eq
    
    i32.const 32
    call $assert2
    
    ;; Memory - STORE8/LOAD8_U
    i32.const 4
    i32.const 0xFF
    i32.store8
    i32.const 4
    i32.load8_u
    i32.const 0xFF
    i32.eq
    
    i32.const 33
    call $assert2
    
    ;; Memory - STORE16/LOAD16_U
    i32.const 8
    i32.const 0xABCD
    i32.store16
    i32.const 8
    i32.load16_u
    i32.const 0xABCD
    i32.eq
    
    i32.const 34
    call $assert2
    
    ;; i64 - ADD
    i64.const 0x100000000
    i64.const 0x200000000
    i64.add
    i64.const 0x300000000
    i64.eq
    
    i32.const 35
    call $assert2
    
    ;; i64 - MUL
    i64.const 1000000
    i64.const 1000000
    i64.mul
    i64.const 1000000000000
    i64.eq
    
    i32.const 36
    call $assert2
    
    ;; Type Conversion - WRAP
    i64.const 0x123456789ABCDEF0
    i32.wrap_i64
    i32.const 0x9ABCDEF0
    i32.eq
     
    i32.const 37
    call $assert2
    
    ;; Type Conversion - EXTEND signed
    i32.const -1
    i64.extend_i32_s
    i64.const -1
    i64.eq
    
    i32.const 38 ;; label
    call $assert2
    
    ;; Type Conversion - EXTEND unsigned
    i32.const 0xFFFFFFFF
    i64.extend_i32_u
    i64.const 0xFFFFFFFF
    i64.eq
    
    i32.const 39 ;; label
    call $assert2
    
    ;; SELECT
    i32.const 100
    i32.const 200
    i32.const 1
    select
    i32.const 100
    i32.eq
    
    i32.const 40
    call $assert2
    
    ;; DROP
    i32.const 999
    drop
    i32.const 42
    i32.const 42
    i32.eq
    
    i32.const 41
    call $assert2
    
    ;; NOP
    nop
    i32.const 42
    nop
    i32.const 42
    i32.eq
    
    i32.const 42
    call $assert2
    
    ;; CALL
    i32.const 5
    call $helper
    i32.const 50
    i32.eq
    
    i32.const 43
    call $assert2
    
    ;; Global - GET
    global.get $g2
    i32.const 200
    i32.eq
    
    i32.const 44
    call $assert2
    
    ;; Global - SET
    i32.const 42
    global.set $g1
    global.get $g1
    i32.const 42
    i32.eq
      
    i32.const 45
    call $assert2
    
    ;; Type Conversion - TRUNCATE signed
        i64.const -12345
        i32.wrap_i64
        i32.const -12345
        i32.eq
    
        i32.const 40 ;; label
        call $assert2
    
        ;; Type Conversion - TRUNCATE unsigned
        i64.const 0xFFFFFFFFFFFFFFFF
        i32.wrap_i64
        i32.const 0xFFFFFFFF
        i32.eq
    
        i32.const 41 ;; label
        call $assert2
    
    
        ;; Type Conversion - F64 -> F32
        f64.const 2.718281828459045
        f32.demote_f64
        f32.const 2.7182817
        f32.eq
    
        i32.const 44 ;; label
        call $assert2
    
        ;; Type Conversion - F64 -> F32
        
        f32.const 2.7182817
        f64.promote_f32
        f64.const 2.7182817
        f64.sub
        f64.abs
        f64.const 0.0001
        f64.lt 
        
        i32.const 44 ;; label
        call $assert2
    
        ;; Type Conversion - I32 -> F32
        i32.const 42
        f32.convert_i32_s
        f32.const 42.0
        f32.eq
    
        i32.const 45 ;; label
        call $assert2
    
        ;; Type Conversion - I64 -> F64
        i64.const 1234567890
        f64.convert_i64_s
        f64.const 1234567890.0
        f64.eq
    
        i32.const 46 ;; label
        call $assert2
        
        ;; Type Conversion - I32 -> F32 (Unsigned)
            ;; 0xFFFFFFFF interpreted unsigned is 4294967295
            i32.const -1
            f32.convert_i32_u
            f32.const 4294967295.0
            f32.eq
        
            i32.const 46 ;; label
            call $assert2
        
            ;; Type Conversion - I64 -> F64 (Signed)
            i64.const -9007199254740991 ;; Min safe integer
            f64.convert_i64_s
            f64.const -9007199254740991.0
            f64.eq
        
            i32.const 47 ;; label
            call $assert2
        
            ;; Type Conversion - I64 -> F32 (Signed with precision loss)
            ;; Large i64 will lose precision when moving to f32
            i64.const 16777217 ;; 2^24 + 1
            f32.convert_i64_s
            f32.const 16777216.0 ;; Rounds down to nearest representable f32 (2^24)
            f32.eq
        
            i32.const 48 ;; label
            call $assert2
        
            ;; Type Conversion - I32 -> I64 (Extend Signed)
            ;; -1 (0xFFFFFFFF) becomes -1 (0xFFFFFFFFFFFFFFFF)
            i32.const -1
            i64.extend_i32_s
            i64.const -1
            i64.eq
        
            i32.const 49 ;; label
            call $assert2
        
            ;; Type Conversion - I32 -> I64 (Extend Unsigned)
            ;; -1 (0xFFFFFFFF) becomes 4294967295 (0x00000000FFFFFFFF)
            i32.const -1
            i64.extend_i32_u
            i64.const 4294967295
            i64.eq
        
            i32.const 50 ;; label
            call $assert2
        
            ;; Type Conversion - F32 -> I32 (Truncate Signed)
            f32.const -123.9
            i32.trunc_f32_s
            i32.const -123 ;; Truncates toward zero
            i32.eq
        
            i32.const 51 ;; label
            call $assert2
        
            ;; Type Conversion - F32 -> I32 (Truncate Unsigned)
            f32.const 255.9
            i32.trunc_f32_u
            i32.const 255
            i32.eq
        
            i32.const 52 ;; label
            call $assert2
        
            ;; Type Conversion - F64 -> I64 (Truncate Signed)
            f64.const 123456789.987
            i64.trunc_f64_s
            i64.const 123456789
            i64.eq
        
            i32.const 53 ;; label
            call $assert2
        
            ;; Type Conversion - Reinterpret F32 -> I32
            ;; Checks raw bit preservation (1.0 float is 0x3f800000)
            f32.const 1.0
            i32.reinterpret_f32
            i32.const 1065353216 ;; 0x3f800000 decimal
            i32.eq
        
            i32.const 54 ;; label
            call $assert2
        
            ;; Type Conversion - Reinterpret I64 -> F64
            ;; 0x400921FB54442D18 is approx pi
            i64.const 4614256656552045848
            f64.reinterpret_i64
            f64.const 3.141592653589793
            f64.eq
        
            i32.const 55 ;; label
            call $assert2
            
            global.get $heap_ptr
            i32.const 5
            i32.const 8
            memory.fill
            global.get $heap_ptr
            i32.const 4
            i32.add
            i32.const 6
            i32.const 4
            memory.fill
            
            global.get $heap_ptr
            i32.const 8
            i32.add
            global.get $heap_ptr
            i32.const 8
            memory.copy 
            ;; now the content of the memory should be 5 5 5 5 6 6 6 6 5 5 5 5 6 6 6 6
            global.get $heap_ptr
            i32.const 3
            i32.add
            i32.load8_u 
            i32.const 5
            i32.eq
            i32.const 56 
            call $assert2
            global.get $heap_ptr
            i32.const 5
            i32.add
            i32.load8_u 
            i32.const 6
            i32.eq
            i32.const 57 
            call $assert2 
            global.get $heap_ptr
            i32.const 9
            i32.add
            i32.load8_u 
            i32.const 5
            i32.eq
            i32.const 58
            call $assert2
            global.get $heap_ptr
            i32.const 12
            i32.add
            i32.load8_u 
            i32.const 6
            i32.eq
            i32.const 59
            call $assert2             
             
  )
     (func $test
             call $test_v128_load32_lane
             call $test_v128_load8_splat
             call $test_i32x4_replace_lane
             call $test_v128_load64_zero
             call $test_i16x8_extend_low_i8x16_u
             call $test_v128_not
             call $test_small_bits 
             (call $test_trunc (f32.const 42.7) (f64.const 42.7) (i32.const 42) (i64.const 42))
             (call $run_all_tests)
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
  (export "fnv1a" (func $fnv1a))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  (global $test_value (mut i32) (i32.const 0x05050505))
  (global $heap_ptr (mut i32) (i32.const 1024))
  (memory $mem 256) 
  (data (i32.const 0) "Hello, World!")
  
  (data (i32.const 50) "\04\00\00\44\00\00\00\88\99\AA\BB\CC\DD\EE\FF\00")
  (data (i32.const 100) "\01\02\03\04\05\06\07\08\11\22\33\44\55\66\77\88")
  )
