using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Aop.Cache
{
    using AddOrUpdateDelegate = Action<object, MemoryCache, string, MemoryCacheEntryOptions>;
    using GetCachedResultDelegate = Func<object, object>;

    public class PerMethodAdapter<T> : BaseAdapter<T>, IPerMethodAdapter<T> where T : class
    {
        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, MemoryCacheEntryOptions options)
        {
            return 
                Cache
                (
                    target, 
                    options, 
                    BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>(),
                    BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
                );
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, MemoryCacheEntryOptions options)
        {
            return 
                Cache
                (
                    target, 
                    options,
                    BuildDefaultAddOrUpdateDelegate(),
                    BuildDefaultGetFromCacheDelegate()
                );
        }

        private void Cache
            (
                MethodCallExpression expression, 
                MemoryCacheEntryOptions options,
                AddOrUpdateDelegate addOrUpdateCacheDelegate,
                GetCachedResultDelegate getFromCacheDelegate
            )
        {
            Expectations
                .Add
                (
                    (
                        Expectation
                            .FromMethodCallExpression
                            (
                                expression, 
                                options
                            ),
                        addOrUpdateCacheDelegate,
                        getFromCacheDelegate
                    )
                );
        }

        private void Cache
            (
                MemberExpression expression,
                MemoryCacheEntryOptions options,
                AddOrUpdateDelegate addOrUpdateCacheDelegate,
                GetCachedResultDelegate getFromCacheDelegate
            )
        {
            Expectations
                .Add
                (
                    (
                        Expectation
                            .FromMemberAccessExpression
                            (
                                expression, 
                                options
                            ),
                        addOrUpdateCacheDelegate,
                        getFromCacheDelegate
                    )
                );
        }

        private IPerMethodAdapter<T> Cache<TReturn>
            (
                Expression<Func<T, TReturn>> target,
                MemoryCacheEntryOptions options,
                AddOrUpdateDelegate addOrUpdateCacheDelegate,
                GetCachedResultDelegate getFromCacheDelegate
            )
        {
            MethodCallExpression expression = null;

            switch (target.Body)
            {
                case MemberExpression memberExpression:
                    Cache(memberExpression, options, addOrUpdateCacheDelegate, getFromCacheDelegate);
                    return this;

                case UnaryExpression unaryExpression:
                    expression = unaryExpression.Operand as MethodCallExpression;
                    break;
            }

            expression = expression ?? target.Body as MethodCallExpression;

            Cache(expression, options, addOrUpdateCacheDelegate,getFromCacheDelegate);

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
                var cacheKey = JsonConvert.SerializeObject(invocation.Arguments);

                if (MemCache.TryGetValue(cacheKey, out var cachedValue))
                {
                    var val = getFromCache.Invoke(cachedValue);

                    if (expectation.IsExpired(val))
                    {
                        invocation.Proceed();

                        addOrUpdateCache
                            .Invoke
                            (
                                invocation.ReturnValue,
                                MemCache,
                                cacheKey,
                                expectation.Options
                            );
                    }
                    else
                    {
                        invocation.ReturnValue = val;
                    }
                }
                else
                {
                    invocation.Proceed();

                    addOrUpdateCache
                        .Invoke
                        (
                            invocation.ReturnValue, 
                            MemCache, 
                            cacheKey,
                            expectation.Options
                        );
                }
            }
            else
            {
                invocation.Proceed();
            }
        }
    }
}
