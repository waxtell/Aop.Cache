using System;
using System.Linq;
using Aop.Cache.Extensions;
using Castle.DynamicProxy;

namespace Aop.Cache;

public abstract class PerInstanceBaseAdapter<T,TEntityOptions> : BaseAdapter<T, TEntityOptions>, IPerInstanceAdapter<T> where T : class
{
    private readonly Func<ICacheImplementation<TEntityOptions>, string, TEntityOptions> _optionsFactory;

    public override void Intercept(IInvocation invocation)
    {
        if (invocation.IsAction())
        {
            invocation.Proceed();
            return;
        }

        var (expectation, addOrUpdateCache, getFromCache) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));
        var cacheKey = invocation.ToKey();

        if (expectation != null)
        {
            if (CacheImplementation.TryGetValue(cacheKey, invocation.MethodInvocationTarget.ReturnType, out var cachedValue))
            {
                invocation.ReturnValue = getFromCache.Invoke(cachedValue);
            }
            else
            {
                invocation.Proceed();

                addOrUpdateCache
                    .Invoke
                    (
                        cacheKey,
                        invocation.ReturnValue,
                        expectation.GetCacheEntryOptions(CacheImplementation, cacheKey)
                    );
            }
        }
        else
        {
            var returnType = invocation.Method.ReturnType;

            expectation = Expectation<TEntityOptions>.FromInvocation(invocation, _optionsFactory);
            addOrUpdateCache = BuildAddOrUpdateDelegateForType(returnType);
            getFromCache = BuildGetFromCacheDelegateForType(returnType);

            Expectations
                .Add
                (
                    (
                        expectation,
                        addOrUpdateCache,
                        getFromCache
                    )
                );

            invocation.Proceed();

            addOrUpdateCache
                .Invoke
                (
                    cacheKey,
                    invocation.ReturnValue,
                    expectation.GetCacheEntryOptions(CacheImplementation, cacheKey)
                );
        }
    }

    protected PerInstanceBaseAdapter(ICacheImplementation<TEntityOptions> cacheImplementation, Func<ICacheImplementation<TEntityOptions>,string, TEntityOptions> optionsFactory)
        : base(cacheImplementation)
    {
        _optionsFactory = optionsFactory;
    }
}