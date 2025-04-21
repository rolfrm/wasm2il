#include "libc.h"
typedef int ssize_t;
typedef int size_t;

__attribute__((import_module("sys")))
int system2(char * app, char * arg1);

__attribute__((import_module("fs")))
ssize_t write(int fd, const void *buffer, size_t length);

__attribute__((import_module("log")))
__attribute__((import_name("debug")))
void log_debug(char * str);

int main(int argc, char ** argv)
{
    return system2("cat", "test.txt");
}

ssize_t fake_write(int fd, const void *buffer, size_t length)
{
    log_debug("Calling fake write");
    return write(fd, buffer, length);
}


void * __provide_overrides__(const char * module, const char * name)
{
    if (strcmp(module, "fs") == 0 && strcmp(name, "write") == 0)
    {
        return fake_write;
    }
    return 0;
    
}