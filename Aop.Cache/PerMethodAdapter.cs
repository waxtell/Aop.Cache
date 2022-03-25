﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Aop.Cache.ExpirationManagement;

namespace Aop.Cache;

public class PerMethodAdapter<T> : BaseAdapter<T>, IPerMethodAdapter<T> 
    where T : class
{
    public PerMethodAdapter(ICacheImplementation cacheImplementation)
        : base(cacheImplementation)
    {
    }

    public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<CacheEntryOptions> optionsFactory)
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

    public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<CacheEntryOptions> optionsFactory)
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
        Func<CacheEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate
    )
    {
        Expectations
            .Add
            (
                (
                    Expectation
                        .FromMethodCallExpression
                        (
                            expression
                        ),
                    addOrUpdateCacheDelegate,
                    marshallResultDelegate,
                    optionsFactory
                )
            );
    }

    private void Cache
    (
        MemberExpression expression,
        Func<CacheEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate
    )
    {
        Expectations
            .Add
            (
                (
                    Expectation.FromMemberAccessExpression(expression),
                    addOrUpdateCacheDelegate,
                    marshallResultDelegate,
                    optionsFactory
                )
            );
    }

    private IPerMethodAdapter<T> Cache<TReturn>
    (
        Expression<Func<T, TReturn>> target,
        Func<CacheEntryOptions> optionsFactory,
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

        var 
        (
            expectation, 
            addOrUpdateCache, 
            getFromCache,
            optionsFactory
        ) = Expectations
        .FirstOrDefault(x => x.expectation.IsHit(invocation));

        if (expectation != null)
        {
            var cacheKey = expectation.GetCacheKey(invocation);

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
            invocation.Proceed();
        }
    }
}