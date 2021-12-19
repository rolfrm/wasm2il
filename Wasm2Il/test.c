//clang --target=wasm32 -nostdlib -Wl,--no-entry, -Wl,--export-all ./test.c -o bin/Debug/net6.0/test.wasm
//clang --target=wasm32-wasi -Wl,--no-entry, -Wl,--export-all -o bin/Debug/net6.0/test.wasm -O1 --sysroot /Users/bruger/code/wasi-libc/sysroot ./test.c 
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <stdio.h>
#include <stdarg.h>
#include <unistd.h>
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

typedef struct {
    float x;
    float y;
}vec2;

vec2 vec2_new(float x, float y){
   return (vec2){.x = x, .y = y};
}
   
int main(int argc, const char ** argv){
   return 0;
}

unsigned int crc32b(const  char * message) {
   int i, j;
   unsigned int byte, crc, mask;

   i = 0;
   crc = 0xFFFFFFFF;
   while (message[i] != 0) {
      byte = message[i];            // Get next byte.
      crc = crc ^ byte;
      for (j = 7; j >= 0; j--) {    // Do eight times.
         mask = -(crc & 1);
         crc = (crc >> 1) ^ (0xEDB88320 & mask);
      }
      i = i + 1;
   }
   return ~crc;
}


int test1(){
     return crc32b("asdasdasdasd");
}
int strlen2(char* ptr){
    int len = 0;
    while(*ptr){
       len++;
       ptr++;
    }   
    return len;
}
int test3(char* ptr){
    int total = 0;
    unsigned int l = (unsigned int)strlen2(ptr);
    for(; l > 1; l--){
        total = (ptr[l] + total * 3145131) * 174321532;
         for (int j = 7; j >= 0; j--) {    // Do eight times.
             total = (total * 2) ^ (0xEDB88320 & j);
         }
    }
    return total;
}

char * getString1(){
    return "asdasdasd";
}
char * getString2(){
     return "asdasdasd1";
 }

char * getString3(){
     return "asdasdasdasd";
 }

int runTest2(){
    char buffer[100];
    char * str = getString3();
    int crc =  test3(buffer);;

    sprintf(buffer, "%i", crc);
    return strlen(buffer);
}


int 
addingNumbers( int nHowMany, ... )
{
  int              nSum =0;
  va_list       intArgumentPointer;
  va_start( intArgumentPointer, nHowMany );
  for( int i = 0; i < nHowMany; i++ )
    nSum += va_arg( intArgumentPointer, int );
  va_end( intArgumentPointer );
  
  return nSum;
} 

int testAddingNumbers(int n){
    return addingNumbers(n, 1,2,3,4,5,6,7,8);
}
float fmod2(float a, float b){
    return fmod(a, b);
}

float fabs2(float a){
    return fabsf(a);
}

double fabsd(double a){
    return fabs(a);
}

 int runTest(){
      char buffer[100];
      int x = 0;
      char * str = getString3();
      memcpy(buffer, str, strlen(str) + 1);
      for(int i = 0; i < 10; i++){
        x = x * 328104 + test3(buffer);
      }
      
      if(x == 0)
         return 0;
      if(atoi("1000") != 1000)
         return 1;
       
      
      if(fabs(atof("1.55") - 1.55) > 0.0001)
         return 3; 
       if(fabs(fmod(1.55, 1.5) - 0.05) > 0.01);
         return 2;  
      return 5;
      
      //sprintf(buffer, "%s %s %i", getString2(), getString1(), x);
      //return strlen2(buffer);
 }
 
 void testWrite(){
     char * test = "1234567890";
     for(int i = 0; i < 1000; i++)
         fwrite(test, 1, 10, stdout);
 
 }
