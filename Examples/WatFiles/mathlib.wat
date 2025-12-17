(module
    ;; Import a logging function from the "env" module (implemented in C#)
    (import "env" "log_value" (func $log_value (param i32)))

    ;; Memory for heap operations (1 page = 64KB)
    (memory $mem 1)

    ;; Heap pointer for simple allocation
    (global $heap_ptr (mut i32) (i32.const 1024))

    ;; Basic addition function
    (func $add (param $a i32) (param $b i32) (result i32)
        local.get $a
        local.get $b
        i32.add
    )

    ;; Basic multiplication function
    (func $multiply (param $a i32) (param $b i32) (result i32)
        local.get $a
        local.get $b
        i32.mul
    )

    ;; Function that uses the imported log_value
    (func $compute_and_log (param $a i32) (param $b i32) (result i32)
        (local $result i32)
        ;; result = (a + b) * (a * b)
        local.get $a
        local.get $b
        call $add
        local.get $a
        local.get $b
        call $multiply
        i32.mul
        local.tee $result
        call $log_value
        local.get $result
    )

    ;; Simple factorial function (iterative)
    (func $factorial (param $n i32) (result i32)
        (local $result i32)
        (local $i i32)

        i32.const 1
        local.set $result
        i32.const 1
        local.set $i

        block $done
            loop $loop
                local.get $i
                local.get $n
                i32.gt_s
                br_if $done

                local.get $result
                local.get $i
                i32.mul
                local.set $result

                local.get $i
                i32.const 1
                i32.add
                local.set $i

                br $loop
            end
        end

        local.get $result
    )

    ;; Simple malloc implementation for demo
    (func $malloc (param $size i32) (result i32)
        (local $ptr i32)
        global.get $heap_ptr
        local.set $ptr
        global.get $heap_ptr
        local.get $size
        i32.add
        global.set $heap_ptr
        local.get $ptr
    )

    ;; Simple free (just a no-op for demo)
    (func $free (param $ptr i32)
        ;; no-op for simplicity
    )

    ;; Function to get string length (for testing string marshaling)
    (func $string_length (param $ptr i32) (result i32)
        (local $len i32)
        i32.const 0
        local.set $len

        block $done
            loop $loop
                local.get $ptr
                local.get $len
                i32.add
                i32.load8_u
                i32.eqz
                br_if $done

                local.get $len
                i32.const 1
                i32.add
                local.set $len

                br $loop
            end
        end

        local.get $len
    )

    ;; Export all functions
    (export "add" (func $add))
    (export "multiply" (func $multiply))
    (export "compute_and_log" (func $compute_and_log))
    (export "factorial" (func $factorial))
    (export "malloc" (func $malloc))
    (export "free" (func $free))
    (export "string_length" (func $string_length))
)
