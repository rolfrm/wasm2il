typedef int ssize_t;
typedef int size_t;

__attribute__((import_module("fs")))
int open(const char *path);
__attribute__((import_module("fs")))
ssize_t read(int fd, void *buffer, size_t length);
__attribute__((import_module("fs")))
ssize_t write(int fd, const void *buffer, size_t length);
__attribute__((import_module("fs")))
int close(int fd);

const int stdin = 0;
const int stdout = 1;
const int stderr = 2;

// this is cat, but implement sh
int main(int argc, char ** argv)
{
    char buffer[4096];
    for (int i = 1; i < argc; i++)
    {
        int fd = open(argv[i]);
        if (fd == -1)
        {
            return -1;
        }
        while (1)
        {
            int toWrite = read(fd, buffer, 4096);
            if (!toWrite)
                break;
            write(stdout, buffer, toWrite);
        }
        close(fd);
    }
    return 0;
}