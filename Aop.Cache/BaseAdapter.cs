using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Aop.Cache
{
    using AddOrUpdateDelegate = Action<object, ConcurrentDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string>;
    using GetCachedResultDelegate = Func<object, object>;

    public abstract class BaseAdapter<T> : IInterceptor where T : class
    {
        protected readonly List<(
                                    Expectation expectation,
                                    AddOrUpdateDelegate addOrUpdateCacheDelegate,
                                    GetCachedResultDelegate getFromCacheDelegate
                                )
                            >
                            Expectations = new List
                            <
                                (
                                    Expectation,
                                    AddOrUpdateDelegate,
                                    GetCachedResultDelegate
                                )
                            >();

        protected readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, (object invocationResult, DateTime invocationDateTime)>> CachedInvocations = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, (object invocationResult, DateTime invocationDateTime)>>();

        protected static void AddOrUpdate(ConcurrentDictionary<string, (object invocationResult, DateTime invocationDateTime)> cache, string cacheKey, object result)
        {
            cache[cacheKey] = (result, DateTime.UtcNow);
        }

        protected static GetCachedResultDelegate BuildDefaultGetFromCacheDelegate()
        {
            Expression<GetCachedResultDelegate> expr = (returnValue) => returnValue;

            return expr.Compile();
        }

        protected static GetCachedResultDelegate BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
        {
            Expression<GetCachedResultDelegate> expr =
                (returnValue) => Task.FromResult((TReturn)returnValue);

            return expr.Compile();
        }

        protected static AddOrUpdateDelegate BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>()
        {
            Expression<AddOrUpdateDelegate> expr =
                (returnValue, cache, cacheKey) => (returnValue as Task<TReturn>)
                                                    .ContinueWith
                                                    (
                                                        i => AddOrUpdate(cache, cacheKey, i.Result)
                                                    );

            return expr.Compile();
        }

        protected static AddOrUpdateDelegate BuildDefaultAddOrUpdateDelegate()
        {
            Expression<AddOrUpdateDelegate> expr =
                (returnValue, cache, cacheKey) => AddOrUpdate(cache, cacheKey, returnValue);

            return expr.Compile();
        }

        protected static GetCachedResultDelegate BuildGetFromCacheDelegateForType(Type tReturn)
        {
            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null && returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildGetFromCacheDelegateForAsynchronousFuncForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildDefaultGetFromCacheDelegate();
        }

        protected static GetCachedResultDelegate BuildGetFromCacheDelegateForAsynchronousFuncForType(Type tReturn)
        {
            var mi = typeof(BaseAdapter<T>)
                        .GetMethod
                        (
                            nameof(BuildGetFromCacheDelegateForAsynchronousFunc), 
                            BindingFlags.NonPublic | BindingFlags.Static
                        );

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (GetCachedResultDelegate)miConstructed?.Invoke(null, null);
        }

        protected static AddOrUpdateDelegate BuildAddOrUpdateDelegateForType(Type tReturn)
        {
            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null && returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildAddOrUpdateDelegateForAsynchronousFuncForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildDefaultAddOrUpdateDelegate();
        }

        private static AddOrUpdateDelegate BuildAddOrUpdateDelegateForAsynchronousFuncForType(Type tReturn)
        {
            var mi = typeof(BaseAdapter<T>)
                        .GetMethod
                        (
                            nameof(BuildAddOrUpdateDelegateForAsynchronousFunc), 
                            BindingFlags.NonPublic | BindingFlags.Static
                        );

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (AddOrUpdateDelegate)miConstructed?.Invoke(null, null);
        }

        public T Adapt(T instance)
        {
            return typeof(T).GetTypeInfo().IsInterface
                ? new ProxyGenerator().CreateInterfaceProxyWithTarget(instance, this)
                : new ProxyGenerator().CreateClassProxyWithTarget(instance, this);
        }

        public abstract void Intercept(IInvocation invocation);
    }
}
