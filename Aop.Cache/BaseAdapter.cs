global using MarshallCacheResultDelegate = System.Func<object, object>;

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

    protected readonly List<(Expectation expectation, AddOrUpdateDelegate addOrUpdateCacheDelegate, MarshallCacheResultDelegate marshallResultDelegate, Func<CacheEntryOptions> optionsFactory)> Expectations = new();
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
                        i =>
                        {
                            if (i.IsFaulted)
                            {
                                if (Options.CacheExceptions)
                                {
                                    AddOrUpdate(cacheKey, i.Exception, memoryCacheEntryOptions);
                                }
                            }
                            else
                            {
                                AddOrUpdate(cacheKey, i.Result, memoryCacheEntryOptions);
                            }
                        }, 
                        TaskContinuationOptions.ExecuteSynchronously
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

        if (returnType is { IsGenericType: true })
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
        var mi = typeof(BaseAdapter<T>)
            .GetMethod
            (
                nameof(BuildMarshallCacheResultDelegateForAsynchronousFunc), 
                BindingFlags.NonPublic | BindingFlags.Static
            );

        var miConstructed = mi?.MakeGenericMethod(tReturn);

        return (MarshallCacheResultDelegate)miConstructed?.Invoke(null, null);
    }

    protected AddOrUpdateDelegate BuildAddOrUpdateDelegateForType(Type tReturn)
    {
        var returnType = tReturn?.GetTypeInfo();

        if (returnType is { IsGenericType: true })
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
        var mi = typeof(BaseAdapter<T>)
                    .GetMethod
                    (
                        nameof(BuildAddOrUpdateDelegateForAsynchronousFunc), 
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );

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