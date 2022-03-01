using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache.Extensions
{
    internal static class InvocationExtensions
    {
        public static string ToKey(this IInvocation invocation)
        {
            return
                JsonConvert
                    .SerializeObject
                    (
                        new
                        {
                            TypeName = invocation.InvocationTarget.GetType().Name,
                            MethodName = invocation.Method.Name,
                            ReturnType = invocation.Method.ReturnType.Name,
                            invocation.Arguments
                        }
                    );
        }
    }
}
