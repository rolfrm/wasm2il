int main(int argc, char ** argv)
{
    return 0;
}

void callback_test(int arg, void (* f)(int value))
{
    f(arg);
}
