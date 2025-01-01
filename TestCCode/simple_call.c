
__attribute__((import_module("sys")))
int system2(char * app, char * arg1);

int main(int argc, char ** argv)
{
    return system2("cat", "test.txt");
}