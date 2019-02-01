using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Aop.Cache.Unit.Tests
{
    public interface IForTestingPurposes
    {
        string DoStuff(int arg1, string arg2);
    }

    public class ForTestingPurposes : IForTestingPurposes
    {
        public Stack<string> InvocationHistory { get; } = new Stack<string>();

        public string DoStuff(int arg1, string arg2)
        {
            LogInvocation();

            return arg1 + arg2;
        }

        private void LogInvocation([CallerMemberName] string callerName = "")
        {
            InvocationHistory.Push(callerName);
        }
    }
 }