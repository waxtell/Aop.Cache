using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache
{
    public class PerInstanceAdapter<T> : BaseAdapter<T>, IPerInstanceAdapter<T> where T : class
    {
        private readonly Func<object, DateTime, bool> _expirationDelegate;

        public override void Intercept(IInvocation invocation)
        {
            if (invocation.IsAction())
            {
                invocation.Proceed();
                return;
            }

            var (expectation, addOrUpdateCache, getFromCache) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));
            var cacheKey = JsonConvert.SerializeObject(invocation.Arguments);

            if (expectation != null)
            {
                if (CachedInvocations.TryGetValue(expectation.Identifier, out var cachedInvocation))
                {
                    if (cachedInvocation.TryGetValue(cacheKey, out var cachedValue))
                    {
                        if (expectation.IsExpired(cachedValue.invocationResult, cachedValue.invocationDateTime))
                        {
                            invocation.Proceed();

                            addOrUpdateCache
                                .Invoke
                                (
                                    invocation.ReturnValue,
                                    cachedInvocation,
                                    cacheKey
                                );
                        }
                        else
                        {
                            invocation.ReturnValue = getFromCache.Invoke(cachedValue.invocationResult);
                        }
                    }
                    else
                    {
                        invocation.Proceed();

                        addOrUpdateCache
                            .Invoke
                            (
                                invocation.ReturnValue,
                                cachedInvocation,
                                cacheKey
                            );
                    }
                }
                else
                {
                    invocation.Proceed();

                    var cache = new Dictionary<string, (object invocationResult, DateTime invocationDateTime)>();

                    addOrUpdateCache
                        .Invoke
                        (
                            invocation.ReturnValue,
                            cache,
                            cacheKey
                        );

                    CachedInvocations
                        .Add
                        (
                            expectation.Identifier,
                            cache
                        );
                }
            }
            else
            {
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation, _expirationDelegate);
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

                var cache = new Dictionary<string, (object invocationResult, DateTime invocationDateTime)>();
                CachedInvocations.Add(expectation.Identifier, cache);

                invocation.Proceed();

                addOrUpdateCache
                    .Invoke
                    (
                        invocation.ReturnValue,
                        cache,
                        cacheKey
                    );
            }
        }

        public PerInstanceAdapter(Func<DateTime,bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);
            _expirationDelegate = expr.Compile();
        }
    }
}
