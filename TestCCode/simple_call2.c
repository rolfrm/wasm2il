#include "libc.h"

__attribute__((import_module("sys")))
int system2(char * app, char * arg1);

__attribute__((import_module("log")))
__attribute__((import_name("debug")))
void log_debug(char * str);

__attribute__((import_module("simple_call")))
__attribute__((import_name("callback")))
int callback(int x, int y);

__attribute__((import_module("simple_call")))
__attribute__((import_name("callback_heap")))
int callback_heap(int x, int * y);

int main(int argc, char ** argv)
{
    if (argc == 1)
    {
        return system2("simple_call2", "direct");
    }else if (argc == 2)
    {
        if (strcmp(argv[1], "test1") == 0)
        {
            return system2("simple_call2", "direct");
        }
        else if (strcmp(argv[1], "test2") == 0)
        {
            return system2("simple_call2", "heap");
        }
        else if (strcmp(argv[1], "heap") == 0)
        {
            int eight = 8;
            return callback_heap(3, &eight);
        }
        return callback(3,7);
    }
    return -1;
}


int callback_impl(int x, int y)
{
    return x * y;
}

int callback_heap_impl(int x, int * y)
{
    return x * *y;
}

void * __provide_overrides__(const char * module, const char * name)
{
    if (strcmp(module, "simple_call") == 0 && strcmp(name, "callback") == 0)
    {
        return callback_impl;
    }
    if (strcmp(module, "simple_call") == 0 && strcmp(name, "callback_heap") == 0)
    {
        return callback_heap_impl;
    }
    return 0;
    
}

argument_definition callback_heap_api_arguments[] = {
    { .type = IMMEDIATE, .fixed_size = -1 },
    { .type = POINTER, .fixed_size = 4 } };

api_definition callback_heap_api = {
    .function = callback_heap_impl,
    .name = "callback_heap",
    .module = "simple_call",
    .arg_count = 2,
    .arguments = callback_heap_api_arguments
};

argument_definition callback_api_arguments[] = {
    { .type = IMMEDIATE, .fixed_size = -1 },
    { .type = IMMEDIATE, .fixed_size = -1 } };

api_definition callback_api = {
    .function = callback_impl,
    .name = "callback",
    .module = "simple_call",
    .arg_count = 2,
    .arguments = callback_api_arguments
};

api_definition * callback_apis[] = {
&callback_heap_api, &callback_api };

override_definitions __overrides = {
.count = COUNT(callback_apis),
.apis = callback_apis
};

override_definitions * __provide_overrides2__()
{
    return &__overrides;
}