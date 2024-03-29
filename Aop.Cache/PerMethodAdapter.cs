﻿using System;
using System.Collections.Generic;
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
        : base(cacheImplementation, _ => { })
    {
    }

    public PerMethodAdapter(ICacheImplementation cacheImplementation, Action<CacheOptions> withOptions)
        : base(cacheImplementation, withOptions)
    {
    }

    public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<CacheEntryOptions> optionsFactory, params Func<Task<TReturn>, bool>[] exclusions)
    {
        return 
            Cache
            (
                target, 
                optionsFactory,
                BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>(),
                BuildMarshallCacheResultDelegateForAsynchronousFunc<TReturn>(),
                exclusions.Select(ExclusionWrapper).ToList()
            );
    }

    private static Func<object, bool> ExclusionWrapper<TReturn>(Func<TReturn, bool> func) => returnValue => func((TReturn)returnValue);

    public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<CacheEntryOptions> optionsFactory, params Func<TReturn, bool>[] exclusions)
    {
        return
            Cache
            (
                target, 
                optionsFactory,
                BuildDefaultAddOrUpdateDelegate<TReturn>(),
                BuildDefaultMarshallCacheResultDelegate(),
                exclusions.Select(ExclusionWrapper).ToList()
            );
    }

    private void Cache
    (
        MethodCallExpression expression,
        Func<CacheEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate,
        List<Func<object,bool>> exclusions
    )
    {
        Expectations
            .Add
            ((
                Expectation
                    .FromMethodCallExpression
                    (
                        expression
                    ),
                addOrUpdateCacheDelegate,
                marshallResultDelegate,
                optionsFactory,
                exclusions
            ));
    }

    private void Cache
    (
        MemberExpression expression,
        Func<CacheEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate,
        List<Func<object, bool>> exclusions
    )
    {
        Expectations
            .Add
            ((
                Expectation.FromMemberAccessExpression(expression),
                addOrUpdateCacheDelegate,
                marshallResultDelegate,
                optionsFactory,
                exclusions
            ));
    }

    private IPerMethodAdapter<T> Cache<TReturn>
    (
        Expression<Func<T, TReturn>> target,
        Func<CacheEntryOptions> optionsFactory,
        AddOrUpdateDelegate addOrUpdateCacheDelegate,
        MarshallCacheResultDelegate marshallResultDelegate,
        List<Func<object,bool>> exclusions)
    {
        MethodCallExpression expression = null;

        switch (target.Body)
        {
            case MemberExpression memberExpression:
                Cache(memberExpression, optionsFactory, addOrUpdateCacheDelegate, marshallResultDelegate,exclusions);
                return this;

            case UnaryExpression unaryExpression:
                expression = unaryExpression.Operand as MethodCallExpression;
                break;
        }

        expression ??= target.Body as MethodCallExpression;

        Cache
        (
            expression, 
            optionsFactory, 
            addOrUpdateCacheDelegate,
            marshallResultDelegate,
            exclusions
        );

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
            optionsFactory,
            exclusions
        ) = Expectations
        .FirstOrDefault(x => x.expectation.IsHit(invocation));

        if (expectation != null)
        {
            var cacheKey = expectation.GetCacheKey(invocation);

            if (CacheImplementation.TryGetValue(cacheKey, invocation.MethodInvocationTarget.ReturnType, out var cachedValue))
            {
                if (cachedValue is Exception returnException)
                {
                    throw returnException;
                }
                
                invocation.ReturnValue = getFromCache.Invoke(cachedValue);
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
                            optionsFactory.Invoke(),
                            exclusions
                        );
                }
                catch (Exception e)
                {
                    addOrUpdateCache
                        .Invoke
                        (
                            cacheKey,
                            e,
                            optionsFactory.Invoke(),
                            exclusions
                        );

                    throw;
                }
            }
        }
        else
        {
            invocation.Proceed();
        }
    }
}