namespace Wasm2Il;

public class Imports
{
    public static int fd_fdstat_get(int a, int b)
    {
        return 1;
    }

    public static int fd_write(int a, int b, int c, int d)
    {
        return c;
    }
}