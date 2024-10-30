(module
  (import "console" "log" (func $log (param i32)))	
	(func $multiply (param $lhs i32) (param $rhs i32) (result i32)
    get_local $lhs
    get_local $rhs
    i32.mul)
  (func $incf (result i32)
    get_global $a
	 i64.const 1
	 f32.const 2.0
 	 f64.const 3.0
	 drop
	 drop
	 drop
	 i32.const 1
	 i32.add
	 set_global $a
	 get_global $a
	 )
  (func $testLog (param $a i32)
     get_local $a
     call $log)
  (export "multiply" (func $multiply))
  (export "incf" (func $incf))
  (export "testLog" (func $testLog))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  )
