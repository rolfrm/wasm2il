(module
  (import "console" "log" (func $log (param i32)))	
	(func $multiply (param $lhs i32) (param $rhs i32) (result i32)
    local.get $lhs
    local.get $rhs
    i32.mul)
    
  (func $incf (result i32)
     global.get $a
	 i64.const 1
	 f32.const 2.0
 	 f64.const 6.0
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
     i32.const 5
     i32.add
     local.set $a
     local.get $a
     local.get $a
     call $log)
     
     
  (export "multiply" (func $multiply))
  (export "incf" (func $incf))
  (export "testLog" (func $testLog))

  (global $a (mut i32) (i32.const 75600))
  (global $a2 (mut i32) (i32.const -64))
  (global $a3 (mut i32) (i32.const -75600))
  )
