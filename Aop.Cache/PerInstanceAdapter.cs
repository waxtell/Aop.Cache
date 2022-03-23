using System;
using System.Linq;
using Aop.Cache.ExpirationManagement;
using Aop.Cache.Extensions;
using Castle.DynamicProxy;

namespace Aop.Cache;

public class PerInstanceAdapter<T> : BaseAdapter<T>, IPerInstanceAdapter<T> where T : class
{
    private readonly Func<CacheEntryOptions> _optionsFactory;

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
            optionsFactory
        ) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

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
                        optionsFactory.Invoke()
                    );
            }
        }
        else
        {
            var returnType = invocation.Method.ReturnType;

            expectation = Expectation.FromInvocation(invocation);
            addOrUpdateCache = BuildAddOrUpdateDelegateForType(returnType);
            getFromCache = BuildGetFromCacheDelegateForType(returnType);

            Expectations
                .Add
                (
                    (
                        expectation,
                        addOrUpdateCache,
                        getFromCache,
                        _optionsFactory
                    )
                );

            invocation.Proceed();

            addOrUpdateCache
                .Invoke
                (
                    cacheKey,
                    invocation.ReturnValue,
                    _optionsFactory.Invoke()
                );
        }
    }

    public PerInstanceAdapter(ICacheImplementation cacheImplementation, Func<CacheEntryOptions> optionsFactory)
        : base(cacheImplementation)
    {
        _optionsFactory = optionsFactory;
    }
}