using System;
using System.Linq;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Aop.Cache
{
    public class PerInstanceAdapter<T> : BaseAdapter<T>, IPerInstanceAdapter<T> where T : class
    {
        private readonly Func<IMemoryCache, string, MemoryCacheEntryOptions> _optionsFactory;

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
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation, _optionsFactory);
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
                        invocation.ReturnValue,
                        MemCache,
                        cacheKey,
                        expectation.OptionsFactory.Invoke(MemCache, cacheKey)
                    );
            }
        }

        public PerInstanceAdapter(IMemoryCache memCache, Func<IMemoryCache,string, MemoryCacheEntryOptions> optionsFactory)
        : base(memCache)
        {
            _optionsFactory = optionsFactory;
        }
    }
}
