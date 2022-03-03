using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Aop.Cache.Extensions;

namespace Aop.Cache;

public abstract class PerMethodBaseAdapter<T,TEntryOptions> : BaseAdapter<T, TEntryOptions>, IPerMethodAdapter<T, TEntryOptions> where T : class
{
    protected PerMethodBaseAdapter(ICacheImplementation<TEntryOptions> memCache)
        : base(memCache)
    {
    }

    public IPerMethodAdapter<T,TEntryOptions> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<ICacheImplementation<TEntryOptions>,string, TEntryOptions> optionsFactory)
    {
        return 
            Cache
            (
                target, 
                optionsFactory, 
                BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>(),
                BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
            );
    }

    public IPerMethodAdapter<T, TEntryOptions> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory)
    {
        return 
            Cache
            (
                target, 
                optionsFactory,
                BuildDefaultAddOrUpdateDelegate(),
                BuildDefaultGetFromCacheDelegate()
            );
    }

    private void Cache
    (
        MethodCallExpression expression,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultsDelegate getFromCacheDelegate
    )
    {
        Expectations
            .Add
            (
                (
                    Expectation<TEntryOptions>
                        .FromMethodCallExpression
                        (
                            expression, 
                            optionsFactory
                        ),
                    addOrUpdateCacheDelegate,
                    getFromCacheDelegate
                )
            );
    }

    private void Cache
    (
        MemberExpression expression,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultsDelegate getFromCacheDelegate
    )
    {
        Expectations
            .Add
            (
                (
                    Expectation<TEntryOptions>
                        .FromMemberAccessExpression
                        (
                            expression, 
                            optionsFactory
                        ),
                    addOrUpdateCacheDelegate,
                    getFromCacheDelegate
                )
            );
    }

    private IPerMethodAdapter<T, TEntryOptions> Cache<TReturn>
    (
        Expression<Func<T, TReturn>> target,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultsDelegate getFromCacheDelegate
    )
    {
        MethodCallExpression expression = null;

        switch (target.Body)
        {
            case MemberExpression memberExpression:
                Cache(memberExpression, optionsFactory, addOrUpdateCacheDelegate, getFromCacheDelegate);
                return this;

            case UnaryExpression unaryExpression:
                expression = unaryExpression.Operand as MethodCallExpression;
                break;
        }

        expression ??= target.Body as MethodCallExpression;

        Cache(expression, optionsFactory, addOrUpdateCacheDelegate,getFromCacheDelegate);

        return this;
    }

    public override void Intercept(IInvocation invocation)
    {
        if (invocation.IsAction())
        {
            invocation.Proceed();
            return;
        }

        var (expectation, addOrUpdateCache, getFromCache) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

        if (expectation != null)
        {
            var cacheKey = invocation.ToKey();

            if (MemCache.TryGetValue(cacheKey, invocation.TargetType, out var cachedValue))
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
                        expectation.GetCacheEntryOptions(MemCache, cacheKey)
                    );
            }
        }
        else
        {
            invocation.Proceed();
        }
    }
}