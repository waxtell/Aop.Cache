using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Aop.Cache.Extensions;

namespace Aop.Cache
{
    using AddOrUpdateDelegate = Action<object, IMemoryCache, string, MemoryCacheEntryOptions>;
    using GetCachedResultDelegate = Func<object, object>;

    public sealed class PerMethodAdapter<T> : BaseAdapter<T>, IPerMethodAdapter<T> where T : class
    {
        public PerMethodAdapter(IMemoryCache memCache)
            : base(memCache)
        {
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
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

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
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
                Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory,
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
                Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory,
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
                                optionsFactory
                            ),
                        addOrUpdateCacheDelegate,
                        getFromCacheDelegate
                    )
                );
        }

        private IPerMethodAdapter<T> Cache<TReturn>
            (
                Expression<Func<T, TReturn>> target,
                Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory,
                AddOrUpdateDelegate addOrUpdateCacheDelegate,
                GetCachedResultDelegate getFromCacheDelegate
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

                if (MemCache.TryGetValue(cacheKey, out var cachedValue))
                {
                    invocation.ReturnValue = getFromCache.Invoke(cachedValue);
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
                            expectation.OptionsFactory.Invoke(MemCache, cacheKey)
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
