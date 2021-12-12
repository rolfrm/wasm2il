//clang --target=wasm32 -nostdlib -Wl,--no-entry, -Wl,--export-all ./test.c -o bin/Debug/net6.0/test.wasm
int AddNumbers(int a, int b){
    return a + b;
}

void DoNothing(){

}
int Get5(){
   return 5;
}
int GetX(int x){
    return x;
}

float GetX2(float x){
    return x;
}
float GetX3(float x){
    return x * x;
}
float GetX4(float x){
    return x + x;
}
float GetX5(float x){
    return GetX2(x);
}
float GetX6(float x){
    return GetX5(x) + GetX5(x)+ GetX5(x);
}

int TestCond(int x){
    if(x > 0)
        return TestCond(x - 1) + 1;
    return 1;
}
int DivInt(int x, int y){
    return x / y;
}

int TestFib(int x){
    if(x < 2) return x;
    return TestFib(x - 1) + TestFib(x - 2);
}