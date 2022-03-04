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
                BuildMarshallCacheResultDelegateForAsynchronousFunc<TReturn>()
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
                BuildDefaultMarshallCacheResultDelegate()
            );
    }

    private void Cache
    (
        MethodCallExpression expression,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate
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
                    marshallResultDelegate
                )
            );
    }

    private void Cache
    (
        MemberExpression expression,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate
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
                    marshallResultDelegate
                )
            );
    }

    private IPerMethodAdapter<T, TEntryOptions> Cache<TReturn>
    (
        Expression<Func<T, TReturn>> target,
        Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate
    )
    {
        MethodCallExpression expression = null;

        switch (target.Body)
        {
            case MemberExpression memberExpression:
                Cache(memberExpression, optionsFactory, addOrUpdateCacheDelegate, marshallResultDelegate);
                return this;

            case UnaryExpression unaryExpression:
                expression = unaryExpression.Operand as MethodCallExpression;
                break;
        }

        expression ??= target.Body as MethodCallExpression;

        Cache(expression, optionsFactory, addOrUpdateCacheDelegate,marshallResultDelegate);

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

            if (MemCache.TryGetValue(cacheKey, invocation.MethodInvocationTarget.ReturnType, out var cachedValue))
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