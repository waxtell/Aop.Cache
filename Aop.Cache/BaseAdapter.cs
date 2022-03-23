global using MarshallCacheResultDelegate = System.Func<object, object>;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Aop.Cache.ExpirationManagement;
using Castle.DynamicProxy;

namespace Aop.Cache;

public abstract class BaseAdapter<T> : IInterceptor where T : class
{
    public delegate void AddOrUpdateDelegate(string cacheKey, object result, CacheEntryOptions entryOptions);

    protected readonly List<(Expectation expectation, AddOrUpdateDelegate addOrUpdateCacheDelegate, MarshallCacheResultDelegate marshallResultDelegate, Func<CacheEntryOptions> optionsFactory)> Expectations = new();

    protected BaseAdapter(ICacheImplementation cacheImplementation)
    {
        CacheImplementation = cacheImplementation;
    }

    protected ICacheImplementation CacheImplementation;

    protected void AddOrUpdate(string cacheKey, object result, CacheEntryOptions options)
    {
        CacheImplementation.Set(cacheKey, result, options);
    }

    protected static MarshallCacheResultDelegate BuildDefaultMarshallCacheResultDelegate()
    {
        Expression<MarshallCacheResultDelegate> expr = returnValue => returnValue;

        return expr.Compile();
    }

    protected static MarshallCacheResultDelegate BuildMarshallCacheResultDelegateForAsynchronousFunc<TReturn>()
    {
        Expression<MarshallCacheResultDelegate> expr =
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

    protected static MarshallCacheResultDelegate BuildGetFromCacheDelegateForType(Type tReturn)
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

        return BuildDefaultMarshallCacheResultDelegate();
    }

    protected static MarshallCacheResultDelegate BuildGetFromCacheDelegateForAsynchronousFuncForType(Type tReturn)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var mi = typeof(BaseAdapter<T>)
            .GetMethod
            (
                nameof(BuildMarshallCacheResultDelegateForAsynchronousFunc), 
                BindingFlags.NonPublic | BindingFlags.Static
            );
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

        var miConstructed = mi?.MakeGenericMethod(tReturn);

        return (MarshallCacheResultDelegate)miConstructed?.Invoke(null, null);
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
        var mi = typeof(BaseAdapter<T>)
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