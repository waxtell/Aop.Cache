using System;
using System.Linq;
using Aop.Cache.ExpirationManagement;
using Castle.DynamicProxy;

namespace Aop.Cache;

public class PerInstanceAdapter<T> : BaseAdapter<T>, IPerInstanceAdapter<T> where T : class
{
    private readonly Func<CacheEntryOptions> _optionsFactory;

    public PerInstanceAdapter(ICacheImplementation cacheImplementation, Func<CacheEntryOptions> optionsFactory)
        : base(cacheImplementation, _ => { })
    {
        _optionsFactory = optionsFactory;
    }

    public PerInstanceAdapter(ICacheImplementation cacheImplementation, Func<CacheEntryOptions> optionsFactory, Action<CacheOptions> withOptions)
        : base(cacheImplementation, withOptions)
    {
        _optionsFactory = optionsFactory;
    }

    public override void Intercept(IInvocation invocation)
    {
        if (invocation.IsAction())
        {
            invocation.Proceed();
            return;
        }

        var 
        (
            expectation, 
            addOrUpdateCache, 
            getFromCache,
            exceptionDelegate,
            optionsFactory
        ) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

        if (expectation != null)
        {
            var cacheKey = expectation.GetCacheKey(invocation);

            if (CacheImplementation.TryGetValue(cacheKey, invocation.MethodInvocationTarget.ReturnType, out var cachedValue))
            {
                if (cachedValue is Exception returnException)
                {
                    invocation.ReturnValue = exceptionDelegate.Invoke(returnException);
                }
                else
                {
                    invocation.ReturnValue = getFromCache.Invoke(cachedValue);
                }
            }
            else
            {
                try
                {
                    invocation.Proceed();

                    addOrUpdateCache
                        .Invoke
                        (
                            cacheKey,
                            invocation.ReturnValue,
                            optionsFactory.Invoke()
                        );
                }
                catch (Exception e)
                {
                    if (Options.CacheExceptions)
                    {
                        CacheImplementation
                            .Set
                            (
                                cacheKey,
                                e,
                                optionsFactory.Invoke()
                            );
                    }

                    throw;
                }
            }
        }
        else
        {
            var returnType = invocation.Method.ReturnType;

            expectation = Expectation.FromInvocation(invocation);
            addOrUpdateCache = BuildAddOrUpdateDelegateForType(returnType);

            Expectations
                .Add
                ((
                    expectation,
                    addOrUpdateCache,
                    BuildGetFromCacheDelegateForType(returnType),
                    BuildExceptionDelegateForType(returnType),
                    _optionsFactory
                ));

            var cacheKey = expectation.GetCacheKey(invocation);

            try
            {
                invocation.Proceed();

                addOrUpdateCache
                    .Invoke
                    (
                        cacheKey,
                        invocation.ReturnValue,
                        _optionsFactory.Invoke()
                    );
            }
            catch (Exception e)
            {
                if (Options.CacheExceptions)
                {
                    CacheImplementation
                        .Set
                        (
                            cacheKey,
                            e,
                            _optionsFactory.Invoke()
                        );
                }

                throw;
            }
        }
    }
}