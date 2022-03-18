namespace Aop.Cache.TestConsole;

public class LengthyOperation : ILengthyOperation
{
    public int Fibonacci(int number)
    {
        if (number is 0 or 1)
        {
            return number;
        }
 
        return (Fibonacci(number - 1) + Fibonacci(number - 2));
    }
}