//clang --target=wasm32 -nostdlib -Wl,--no-entry, -Wl,--export-all ./test.c -o bin/Debug/net6.0/test.wasm
int AddNumbers(int a, int b){
    return a + b;
}
int MulInt(int a){
    return a * a;
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

int Tformx(int x){
    switch(x){
       case 0: return 5;
       case 1: return 111;
       case 2: return 313;
       case 4: return -1000;
       case 5: return -1000000;
       default: return -1;
    }
}
int selectTest(int x){
   if(x == 0) return 1;
   return 2;
}

typedef int ( * fptr)(int x);
fptr selectTestPtr(){
    return selectTest;
}

fptr MulIntPtr(){
    return MulInt;
}

int callPtr(fptr f, int x){
    return f(x);
} 


void writeData(int * ptr, int * ptr2, int cnt){
    for(int i = 0; i < cnt; i++)
       ptr2[i] = ptr[i];
}
int * getXPointer(){
    static int x[1024];
    return x;
}
void SetValue(int * ptr, int value){
    ptr[0] = value;
}
int GetValue(int * ptr){
    return ptr[0];
}
