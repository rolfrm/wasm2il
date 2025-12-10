namespace Wasm2IL;

public class TestCode
{
    static int TestFib(int x){
        if(x == 0) return 0;
        if(x == 1) return 1;
        return TestFib(x - 1) + TestFib(x - 2);
    }


    static int Tformx(int x){
            switch(x){
                case 0: return 5;
                case 1: return 111;
                case 2: return 313;
                case 4: return -1000;
                case 5: return -1000000;
                default: return -1;
            }
        }
}