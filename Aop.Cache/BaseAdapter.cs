global using MarshallCacheResultDelegate = System.Func<object, object>;
global using ExceptionDelegate = System.Func<System.Exception, object>;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aop.Cache.ExpirationManagement;
using Castle.DynamicProxy;

namespace Aop.Cache;

public abstract class BaseAdapter<T> : IInterceptor where T : class
{
    public delegate void AddOrUpdateDelegate(string cacheKey, object result, CacheEntryOptions entryOptions);

    protected readonly List<(Expectation expectation, AddOrUpdateDelegate addOrUpdateCacheDelegate, MarshallCacheResultDelegate marshallResultDelegate, ExceptionDelegate exceptionDelegate, Func<CacheEntryOptions> optionsFactory)> Expectations = new();
    protected readonly CacheOptions Options;

    protected BaseAdapter(ICacheImplementation cacheImplementation, Action<CacheOptions> withOptions)
    {
        CacheImplementation = cacheImplementation;

        Options = new CacheOptions();
        withOptions.Invoke(Options);
    }

    protected ICacheImplementation CacheImplementation;

    protected void AddOrUpdate(string cacheKey, object result, CacheEntryOptions options)
    {
        CacheImplementation.Set(cacheKey, result, options);
    }

    protected static MarshallCacheResultDelegate BuildDefaultMarshallCacheResultDelegate()
    {
        return 
            returnValue => returnValue;
    }

    protected static MarshallCacheResultDelegate BuildMarshallCacheResultDelegateForAsynchronousFunc<TReturn>()
    {
        return 
            returnValue => Task.FromResult((TReturn)returnValue);
    }

    protected AddOrUpdateDelegate BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>()
{
        return
            (cacheKey, returnValue, memoryCacheEntryOptions) => 
                (returnValue as Task<TReturn>)!
                    .ContinueWith
                    (
                        i => AddOrUpdate(cacheKey, i.IsFaulted ? i.Exception : i.Result, memoryCacheEntryOptions)
                    );
    }

    protected AddOrUpdateDelegate BuildDefaultAddOrUpdateDelegate()
    {
        return
            AddOrUpdate;
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

    protected static ExceptionDelegate BuildDefaultExceptionDelegate()
    {
        return 
            ex => throw ex;
    }

    protected static ExceptionDelegate BuildExceptionDelegateForAsynchronousFunc<TReturn>()
    {
        return
            Task.FromException<TReturn>;
    }

    protected static ExceptionDelegate BuildExceptionDelegateForAsynchronousFuncForType(Type tReturn)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var mi = typeof(BaseAdapter<T>)
            .GetMethod
            (
                nameof(BuildExceptionDelegateForAsynchronousFunc),
                BindingFlags.NonPublic | BindingFlags.Static
            );
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

        var miConstructed = mi?.MakeGenericMethod(tReturn);

        return (ExceptionDelegate)miConstructed?.Invoke(null, null);
    }

    protected ExceptionDelegate BuildExceptionDelegateForType(Type tReturn)
    {
        var returnType = tReturn?.GetTypeInfo();

        if (returnType != null && returnType.IsGenericType)
        {
            var gt = returnType.GetGenericTypeDefinition();

            if (gt == typeof(Task<>))
            {
                return BuildExceptionDelegateForAsynchronousFuncForType(tReturn);
            }
        }

        return BuildDefaultExceptionDelegate();
    }

    public T Adapt(T instance)
    {
        return typeof(T).GetTypeInfo().IsInterface
            ? new ProxyGenerator().CreateInterfaceProxyWithTarget(instance, this)
            : new ProxyGenerator().CreateClassProxyWithTarget(instance, this);
    }

    public abstract void Intercept(IInvocation invocation);
}