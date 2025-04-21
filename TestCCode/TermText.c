#include "libc.h"

__attribute__((import_module("log")))
__attribute__((import_name("debug")))
void log_debug(char * str);

__attribute__((import_module("time")))
__attribute__((import_name("sleep")))
void sleep(int ms);

__attribute__((import_module("console")))
__attribute__((import_name("height")))
int console_height();

__attribute__((import_module("console")))
__attribute__((import_name("width")))
int console_width();

__attribute__((import_module("console")))
__attribute__((import_name("write")))
void console_write(const char * str);

__attribute__((import_module("console")))
__attribute__((import_name("cursor")))
void console_cursor(int x, int y);

__attribute__((import_module("sys")))
__attribute__((import_name("process_events")))
void process_events();

static bool exited = false;

void write_text(int x, int y, char * buffer, size_t length)
{
    console_cursor(x,y);
    console_write(buffer);
}

#define MAX_SIZE 500 * 500
int buffer[MAX_SIZE];
int main(int argc, char ** argv)
{
    int height = console_height();
    int width = console_width();
    for (int i = 0; i < height; i++)
    {
        console_write("\n");
    }

	 write_text(5, 2, "****************", 10);
	 write_text(10, 3, "Hello World!", 10);
	 write_text(5, 4, "****************", 10);
    
    while (!exited)
    {
		process_events();
		
    }
  
     return 0;
}

argument_definition callback_heap_api_arguments[] = {
  { .type = IMMEDIATE, .fixed_size = -1 },
  { .type = IMMEDIATE, .fixed_size = -1 },
  { .type = BUFFER, .fixed_size = 3 } ,
  { .type = IMMEDIATE, .fixed_size = -1 }  };

api_definition callback_heap_api = {
    .function = write_text,
    .name = "write_text",
    .module = "term_text",
    .arg_count = 4,
    .arguments = callback_heap_api_arguments
};


api_definition * callback_apis[] = {
&callback_heap_api };

override_definitions __overrides = {
.count = COUNT(callback_apis),
.apis = callback_apis
};

override_definitions * __provide_overrides2__()
{
    return &__overrides;
}
