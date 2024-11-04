(module
  (import "console" "log" (func $log (param i32)))	
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
              ;; Perform element-wise multiplication on the vectors
              (v128.const i8x16 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16)
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
  (export "multiply" (func $multiply))
  (export "multiply_vec" (func $multiply_vec))
  (export "incf" (func $incf))
  (export "testLog" (func $testLog))
  (export "try_vec" (func $try_vec))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  )
