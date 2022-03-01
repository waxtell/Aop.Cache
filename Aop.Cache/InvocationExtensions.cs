using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Aop.Cache;

public static class InvocationExtensions
{
    public static bool IsAction(this IInvocation invocation)
    {
        return invocation.Method.ReturnType == typeof(void) || invocation.Method.ReturnType == typeof(Task);
    }
}