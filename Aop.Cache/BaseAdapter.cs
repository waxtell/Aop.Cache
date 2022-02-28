using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Aop.Cache
{
    using AddOrUpdateDelegate = Action<object, IMemoryCache, string, MemoryCacheEntryOptions>;
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

        protected readonly IMemoryCache MemCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        protected static void AddOrUpdate(IMemoryCache cache, string cacheKey, object result, MemoryCacheEntryOptions options)
        {
            cache.Set(cacheKey, result, options);
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
                (returnValue, cache, cacheKey, memoryCacheEntryOptions) => (returnValue as Task<TReturn>)
                                                    .ContinueWith
                                                    (
                                                        i => AddOrUpdate(cache, cacheKey, i.Result, memoryCacheEntryOptions)
                                                    );

            return expr.Compile();
        }

        protected static AddOrUpdateDelegate BuildDefaultAddOrUpdateDelegate()
        {
            Expression<AddOrUpdateDelegate> expr =
                (returnValue, cache, cacheKey, memoryCacheEntryOptions) => AddOrUpdate(cache, cacheKey, returnValue, memoryCacheEntryOptions);

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
