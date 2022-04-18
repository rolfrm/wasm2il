//clang --target=wasm32 -nostdlib -Wl,--no-entry, -Wl,--export-all ./test.c -o bin/Debug/net6.0/test.wasm
//clang --target=wasm32-wasi -Wl,--no-entry, -Wl,--export-all -o bin/Debug/net6.0/test.wasm -O1 --sysroot /Users/bruger/code/wasi-libc/sysroot ./test.c
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <stdio.h>
#include <stdarg.h>
#include <unistd.h>
#include <sys/stat.h>
#include "sqlite3.h"
#include <fcntl.h>
#define F_RDLCK 0
#define F_WRLCK 1
#define F_UNLCK 2
#define F_GETLK  5
#define F_SETLK 6
int AddNumbers(int a, int b)
{
    return a + b;
}
int MulInt(int a)
{
    return a * a;
}
void DoNothing()
{
}
int Get5()
{
    return 5;
}
int GetX(int x)
{
    return x;
}

float GetX2(float x)
{
    return x;
}
float GetX3(float x)
{
    return x * x;
}
float GetX4(float x)
{
    return x + x;
}
float GetX5(float x)
{
    return GetX2(x);
}
float GetX6(float x)
{
    return GetX5(x) + GetX5(x) + GetX5(x);
}

int TestCond(int x)
{
    if (x > 0)
        return TestCond(x - 1) + 1;
    return 1;
}
int DivInt(int x, int y)
{
    return x / y;
}

int TestFib(int x)
{
    if (x < 2)
        return x;
    return TestFib(x - 1) + TestFib(x - 2);
}

int Tformx(int x)
{
    switch (x)
    {
    case 0:
        return 5;
    case 1:
        return 111;
    case 2:
        return 313;
    case 4:
        return -1000;
    case 5:
        return -1000000;
    default:
        return -1;
    }
}
int selectTest(int x)
{
    if (x == 0)
        return 1;
    return 2;
}

typedef int (*fptr)(int x);
fptr selectTestPtr()
{
    return selectTest;
}

fptr MulIntPtr()
{
    return MulInt;
}

int callPtr(fptr f, int x)
{
    return f(x);
}

void writeData(int *ptr, int *ptr2, int cnt)
{
    for (int i = 0; i < cnt; i++)
        ptr2[i] = ptr[i];
}
int *getXPointer()
{
    static int x[1024];
    return x;
}
void SetValue(int *ptr, int value)
{
    ptr[0] = value;
}
int GetValue(int *ptr)
{
    return ptr[0];
}

typedef struct
{
    float x;
    float y;
} vec2;

vec2 vec2_new(float x, float y)
{
    return (vec2){.x = x, .y = y};
}


unsigned int crc32b(const char *message)
{
    int i, j;
    unsigned int byte, crc, mask;

    i = 0;
    crc = 0xFFFFFFFF;
    while (message[i] != 0)
    {
        byte = message[i]; // Get next byte.
        crc = crc ^ byte;
        for (j = 7; j >= 0; j--)
        { // Do eight times.
            mask = -(crc & 1);
            crc = (crc >> 1) ^ (0xEDB88320 & mask);
        }
        i = i + 1;
    }
    return ~crc;
}

int test1()
{
    return crc32b("asdasdasdasd");
}
int strlen2(char *ptr)
{
    int len = 0;
    while (*ptr)
    {
        len++;
        ptr++;
    }
    return len;
}
int test3(char *ptr)
{
    int total = 0;
    unsigned int l = (unsigned int)strlen2(ptr);
    for (; l > 1; l--)
    {
        total = (ptr[l] + total * 3145131) * 174321532;
        for (int j = 7; j >= 0; j--)
        { // Do eight times.
            total = (total * 2) ^ (0xEDB88320 & j);
        }
    }
    return total;
}

char *getString1()
{
    return "asdasdasd";
}
char *getString2()
{
    return "asdasdasd1";
}

char *getString3()
{
    return "asdasdasdasd";
}

int runTest2()
{
    char buffer[100];
    char *str = getString3();
    int crc = test3(buffer);
    ;

    sprintf(buffer, "%i", crc);
    return strlen(buffer);
}

int addingNumbers(int nHowMany, ...)
{
    int nSum = 0;
    va_list intArgumentPointer;
    va_start(intArgumentPointer, nHowMany);
    for (int i = 0; i < nHowMany; i++)
        nSum += va_arg(intArgumentPointer, int);
    va_end(intArgumentPointer);

    return nSum;
}

int testAddingNumbers(int n)
{
    return addingNumbers(n, 1, 2, 3, 4, 5, 6, 7, 8);
}
float fmod2(float a, float b)
{
    return fmod(a, b);
}

float fabs2(float a)
{
    return fabsf(a);
}

double fabsd(double a)
{
    return fabs(a);
}

int runTest()
{
    char buffer[100];
    int x = 0;
    char *str = getString3();
    memcpy(buffer, str, strlen(str) + 1);
    for (int i = 0; i < 10; i++)
    {
        x = x * 328104 + test3(buffer);
    }

    if (x == 0)
        return 0;
    if (atoi("1000") != 1000)
        return 1;

    if (fabs(atof("1.55") - 1.55) > 0.0001)
        return 3;
    if (fabs(fmod(1.55, 1.5) - 0.05) > 0.01)
        return 2;
    return 5;

    //sprintf(buffer, "%s %s %i", getString2(), getString1(), x);
    //return strlen2(buffer);
}

void testWrite()
{
    char *test = "1234567890";
    for (int i = 0; i < 10; i++)
        fwrite(test, 1, 10, stdout);
    printf("\n");
}

void helloWorld()
{
    printf("hello world4\n");
    printf("hello world3\n");
}
int __wasilibc_register_preopened_fd(int fd, const char *prefix); /*{

}*/

int openWriteRead()
{
    printf("OpenWriteRead\n");
    __wasilibc_register_preopened_fd(4, "/tmp/")    ;
    FILE *f = fopen("/tmp/test.txt", "w+");
    if (f == NULL)
        return 1;
    printf("file opened: %i\n", (int)f);
    fwrite("Hello worle!\n", 1, 13, f);
    fflush(f);
    fseek(f, 0, SEEK_SET);
    char buf[100];
    fread(buf, 100, 1, f);
    fclose(f);
    printf("File contet: %s\n", buf);
    printf("Finished\n");

    f = fopen("/tmp/test2.txt", "w+");
    if (f == NULL)
        return 2;
    fwrite("Hello world!\n", 1, 13, f);
    fclose(f);
    fflush(stdout);
    return 0;
}
void doAbort(){
    printf("aborted \n");
    fflush(stdout);
    abort();
}//sqlite3_io_error_trap
void assert(int test){
    if(test == 0){
        printf("Assertion failed.\n");
        fflush(stdout);
        doAbort();
    }
}
void basicNumbersTest(){
    {
        int i = 1;
        i = i << 16;
        assert(i == 0x10000);
        i |= 0x1000;
        assert(i == 0x11000);
        i |= 0x100;
        assert(i == 0x11100);
    }
    {
        unsigned int i = 1;
        i = i << 16;
        assert(i == 0x10000);
        i |= 0x1000;    
        assert(i == 0x11000);
        i |= 0x100;
        assert(i == 0x11100);
    }
    {
        int i = 0x11001;
        i = i ^ 0x10101;
        printf("I: %x", i);
        assert(i == 0x01100);
    }
    
}

int preMallocTest(){

    void * a = sbrk(0);
    void * b = sbrk(1 << 16);
    if(a != b) return 2;
    void * c = sbrk(2 << 16);
    if(b >= c) return 2;
    void * d = sbrk(0);
    if(c >= d) return 2;
    void * e = sbrk(0);
    if(d != e) return 2;
    
    printf("OK! %p %p %p %p %p\n", a, b, c, d, e);
    return 0;
}
int preMallocTest3(){
    for(int i = 0; i < 10; i++){
        int * ptrs[2];
        for(int j = 0; j < 2; j++){
            ptrs[j] = malloc(10);  
            printf("it: %i %i\n", i, j);
            if(j > 0)
                assert(ptrs[j] != ptrs[j - 1]);
        }
        for(int j = 0; j < 2; j++){
        
            free(ptrs[j]);
        }

    }
    printf("Done with pree mnalloc test 2\n");
    return 0;
}

__declspec(noinline) 
void * falloc(int size){
    return malloc(size);
}

int preMallocTest2(){
    for(int i = 0; i < 10; i++){
        int * a = falloc(10);
        int * b = falloc(10);
        printf("%p != %p?\n", a, b);
        free(a);
        printf("free2\n");
        free(b);
        assert(a != b);
    }
    printf("Done with pree mnalloc test 2\n");
    return 0;
}

int mallocTest0()
{
    int *ptrs[2];
    for (int k = 0; k < 2; k++)
    {
        for (int i = 0; i < 2; i++)
        {
            int size = (i + 1) * 50;
            ptrs[i] = malloc(size);
            //int cnt = size / 4;
            printf("Write> %i %i %p\n", i, size, ptrs[i]);
            if(i > 0){
                assert(ptrs[i] != ptrs[i - 1]);
            }
            /*for (int j = 0; j < cnt; j++)
            {
                ptrs[i][j] = j + (i * 50);
            }*/
        }
        for (int i = 0; i < 2; i++)
        {

            //int size = (i + 1) * 50;
            //int cnt = size / 4;
            //printf("OK? %i %i %i %p\n", i, cnt, size, ptrs[i]);
            //for (int j = 0; j < cnt; j++)
            {
                /*int exp = j + (i * 50);
                int got = ptrs[i][j];
                if (got != exp)
                {
                    printf("Expected %i, got %i\n", exp, got);
                    return 5;
                }*/
            }
            free(ptrs[i]);
        }
    }
    return 0;
}
int mallocTest()
{
    int *ptrs[10];
    for (int k = 0; k < 10; k++)
    {
        for (int i = 0; i < 10; i++)
        {
            int size = (i + 1) * 50;
            ptrs[i] = malloc(size);
            int cnt = size / 4;
            for (int j = 0; j < cnt; j++)
            {
                ptrs[i][j] = j + (i * 50);
            }
        }
        for (int i = 0; i < 10; i++)
        {
            int size = (i + 1) * 50;
            ptrs[i] = realloc(ptrs[i], size * 2);
        }
        for (int i = 0; i < 10; i++)
        {

            int size = (i + 1) * 50;
            int cnt = size / 4;
            printf("OK? %i %i %i %p\n", i, cnt, size, ptrs[i]);
            for (int j = 0; j < cnt; j++)
            {
                int exp = j + (i * 50);
                int got = ptrs[i][j];
                if (got != exp)
                {
                    printf("Expected %i, got %i\n", exp, got);
                    return 5;
                }
            }
            free(ptrs[i]);
        }
    }
    return 0;
}

void sqlitedolog(void* ctx,int x,const char* msg){
    printf("SQL: %s\n", msg);
    fflush(stdout);
    //abort();
}
int sqlAssert(sqlite3 *db, int rc){
    switch(rc){
       case SQLITE_OK:
       case SQLITE_DONE:
       case SQLITE_ROW:
           return rc; // ok error codes.
       default:
          printf("Error: %s\n", sqlite3_errmsg(db));
          abort();
          return rc;
    }
}

int TestSqlite(const char * connectionString)
{
    sqlite3_config(SQLITE_CONFIG_LOG, sqlitedolog, NULL);
    printf("%s\n", sqlite3_libversion());

    sqlite3 *db;
    sqlite3_stmt *res; 
    int rc;
    sqlAssert(db, sqlite3_open(connectionString, &db));
    sqlAssert(db, sqlite3_prepare_v2(db, "SELECT SQLITE_VERSION()", -1, &res, 0));
    
    rc = sqlite3_step(res);
    if (rc == SQLITE_ROW)
        printf("Got row: %s\n", sqlite3_column_text(res, 0));
    sqlAssert(db, sqlite3_finalize(res));
    printf("Lets try create\n"); fflush(stdout);
    char *err_msg = 0;
    sqlAssert(db, sqlite3_exec(db, "CREATE TABLE Animals(Id INT, Name TEXT, Price INT, HitPoints REAL);", 0, 0, &err_msg));
    printf("Lets try populating it.\n"); fflush(stdout);
    sqlAssert(db, sqlite3_exec(db, "INSERT INTO Animals VALUES (1, \"Tiger\", 10, 100.0);", 0, 0,  &err_msg));
    sqlAssert(db, sqlite3_exec(db, "INSERT INTO Animals VALUES (2, \"Lion\", 15, 100.0);", 0, 0,  &err_msg));
    sqlAssert(db, sqlite3_prepare_v2(db, "SELECT * FROM Animals", -1, &res, 0));
    
    while (SQLITE_ROW == (rc = sqlAssert(db, sqlite3_step(res))))
    {
        printf("Got Animal row: %s hp=%f id = %i, price = %i\n", sqlite3_column_text(res, 1), sqlite3_column_double(res, 3) , sqlite3_column_int(res, 0), sqlite3_column_int(res, 2));
    }
    sqlAssert(db, rc);
    sqlAssert(db, sqlite3_finalize(res));
    
    sqlAssert(db, sqlite3_prepare_v2(db, "SELECT * FROM (SELECT lower(hex(RANDOMBLOB(16)))) LIMIT 10", -1, &res, 0));
    while (SQLITE_ROW == (rc = sqlAssert(db, sqlite3_step(res))))
    {
        printf("Got Blob: %s \n", sqlite3_column_text(res, 0));
    }
    sqlAssert(db, sqlite3_finalize(res));
    sqlAssert(db, sqlite3_exec(db, "VACUUM;", 0, 0,  &err_msg));
    printf("Totally done.\n");

    sqlAssert(db, sqlite3_close(db));
    fflush(stdout);
    return 0;
}




int testFstat(){
    struct stat buf;

    //FILE * ftest = fopen("/tmp/test1", "w+");
    //fprintf(ftest, "hello world");
    //fclose(ftest);
    //int ok = stat("/tmp/test1", &buf);
    
    int fdtest = open("/tmp/test1", O_RDWR);
    int ok2 = fstat(fdtest, &buf);
    assert(ok2 == 0);

    struct flock fl = {0};
    
    fl.l_type = F_RDLCK;
    fl.l_whence = SEEK_SET;
    fcntl(fdtest, F_SETLK, &fl);


    close(fdtest);
    
     switch (buf.st_mode & S_IFMT) {
    case S_IFBLK:  printf("block device\n");            break;
    case S_IFCHR:  printf("character device\n");        break;
    case S_IFDIR:  printf("directory\n");               break;
    case S_IFIFO:  printf("FIFO/pipe\n");               break;
    case S_IFLNK:  printf("symlink\n");                 break;
    case S_IFREG:  printf("regular file\n");            break;
    //case S_IFSOCK: printf("socket\n");                  break;
    default:       printf("unknown?\n");                break;
    }
    
    //assert(ok == 0);
    printf("%lld %lld %lld\n", buf.st_atime, buf.st_mtime, buf.st_ctime);
    printf("%lld %lld %lo==%lo\n", buf.st_dev, buf.st_ino, buf.st_mode, (int)0777);
    printf("%i %i %lld\n", buf.st_uid, buf.st_gid, buf.st_rdev);
    printf("%lld %li %lld\n", buf.st_size, buf.st_blksize, buf.st_blocks);
    

    
    return 0;
}

__declspec(noinline)
volatile int testWrap(int x){
    return 5;
}

__declspec(noinline)
fptr getTestWrap(){
    return testWrap;
}

__declspec(noinline)
int testTestWrap(){
    // this method should be wrapped in something else.
    //assert(testWrap(3) != 5);
    assert(getTestWrap()(3) != 5);
    return 0;
}

int GoTest2()
{
   __wasilibc_register_preopened_fd(4, "/tmp");
   //__wasilibc_register_preopened_fd(5, "/tmp/sqlthing");
    /*
    helloWorld();
    basicNumbersTest();
    if (openWriteRead() != 0)
        return 2;
    if(preMallocTest2() != 0)
        return 6;
    if (mallocTest0() != 0)
        return 3;
    if (mallocTest() != 0)
        return 5;
   if(testTestWrap() != 0){
       return 5;    
   }*/
    //printf("IO: %i\n", SQLITE_IOERR_FSTAT);
    //if (TestSqlite(":memory:") != 0)
    //    return 4;
    //printf("Fib(40)\n");
    //TestFib(40);
    //printf("done\n");
    //return 0;
    //remove("/tmp/sqlthing");
    if (TestSqlite(":memory:") != 0)
        return 4;
    //if(testFstat() != 0)
    //    return 6;
    
    
    return 1;
}

int GoTest()
{
    int result = GoTest2();
    if (result != 1)
    {
        printf("Error: %i\n", result);
    }
    fflush(stdout);
    return result;
}
int main(){
    GoTest();
}