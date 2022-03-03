//global using AddOrUpdateDelegate = System.Action<object, string, Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions>;
global using MarshallCacheResultsDelegate = System.Func<object, object>;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Aop.Cache;

public abstract class BaseAdapter<T,TEntryOptions> : IInterceptor where T : class
{
    public delegate void AddOrUpdateDelegate(string cacheKey, object result, TEntryOptions entryOptions);

    protected readonly List<(Expectation<TEntryOptions> expectation, AddOrUpdateDelegate addOrUpdateCacheDelegate, MarshallCacheResultsDelegate getFromCacheDelegate)> Expectations = new();

    protected BaseAdapter(ICacheImplementation<TEntryOptions> memCache)
    {
        MemCache = memCache;
    }

    protected ICacheImplementation<TEntryOptions> MemCache;

    protected void AddOrUpdate(string cacheKey, object result, TEntryOptions options)
    {
        MemCache.Set(cacheKey, result, options);
    }

    protected static MarshallCacheResultsDelegate BuildDefaultGetFromCacheDelegate()
    {
        Expression<MarshallCacheResultsDelegate> expr = (returnValue) => returnValue;

        return expr.Compile();
    }

    protected static MarshallCacheResultsDelegate BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
    {
        Expression<MarshallCacheResultsDelegate> expr =
            (returnValue) => Task.FromResult((TReturn)returnValue);

        return expr.Compile();
    }

    protected AddOrUpdateDelegate BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>()
    {
        Expression<AddOrUpdateDelegate> expr =
            (cacheKey, returnValue, memoryCacheEntryOptions) => (returnValue as Task<TReturn>)
                .ContinueWith
                (
                    i => AddOrUpdate(cacheKey, i.Result, memoryCacheEntryOptions)
                );

        return expr.Compile();
    }

    protected AddOrUpdateDelegate BuildDefaultAddOrUpdateDelegate()
    {
        Expression<AddOrUpdateDelegate> expr =
            (cacheKey, returnValue, memoryCacheEntryOptions) => AddOrUpdate(cacheKey, returnValue, memoryCacheEntryOptions);

        return expr.Compile();
    }

    protected static MarshallCacheResultsDelegate BuildGetFromCacheDelegateForType(Type tReturn)
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

    protected static MarshallCacheResultsDelegate BuildGetFromCacheDelegateForAsynchronousFuncForType(Type tReturn)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var mi = typeof(BaseAdapter<T,TEntryOptions>)
            .GetMethod
            (
                nameof(BuildGetFromCacheDelegateForAsynchronousFunc), 
                BindingFlags.NonPublic | BindingFlags.Static
            );
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

        var miConstructed = mi?.MakeGenericMethod(tReturn);

        return (MarshallCacheResultsDelegate)miConstructed?.Invoke(null, null);
    }

    protected AddOrUpdateDelegate BuildAddOrUpdateDelegateForType(Type tReturn)
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

    private AddOrUpdateDelegate BuildAddOrUpdateDelegateForAsynchronousFuncForType(Type tReturn)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var mi = typeof(BaseAdapter<T,TEntryOptions>)
                    .GetMethod
                    (
                        nameof(BuildAddOrUpdateDelegateForAsynchronousFunc), 
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

        var miConstructed = mi?.MakeGenericMethod(tReturn);

        return (AddOrUpdateDelegate) miConstructed?.Invoke(this, null);
    }

    public T Adapt(T instance)
    {
        return typeof(T).GetTypeInfo().IsInterface
            ? new ProxyGenerator().CreateInterfaceProxyWithTarget(instance, this)
            : new ProxyGenerator().CreateClassProxyWithTarget(instance, this);
    }

    public abstract void Intercept(IInvocation invocation);
}