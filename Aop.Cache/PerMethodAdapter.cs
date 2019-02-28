using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache
{
    using AddOrUpdateDelegate = Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string>;
    using GetCachedResultDelegate = Func<object, object>;

    public class PerMethodAdapter<T> : BaseAdapter<T>, IPerMethodAdapter<T> where T : class
    {
        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target,
            Func<TReturn, DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate((TReturn)i, d);

            return 
                Cache
                (
                    target, 
                    expr.Compile(), 
                    BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>(),
                    BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
                );
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);

            return
                Cache
                (
                    target,
                    expr.Compile(),
                    BuildAddOrUpdateDelegateForAsynchronousFunc<TReturn>(),
                    BuildGetFromCacheDelegateForAsynchronousFunc<TReturn>()
                );
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<TReturn, DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate((TReturn)i, d);

            return 
                Cache
                (
                    target, 
                    expr.Compile(),
                    BuildDefaultAddOrUpdateDelegate(),
                    BuildDefaultGetFromCacheDelegate()
                );
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);

            return 
                Cache
                (
                    target, 
                    expr.Compile(), 
                    BuildDefaultAddOrUpdateDelegate(),
                    BuildDefaultGetFromCacheDelegate()
                );
        }

        private void Cache
            (
                MethodCallExpression expression, 
                Func<object, DateTime, bool> expirationDelegate,
                AddOrUpdateDelegate marshaller,
                GetCachedResultDelegate unMarshaller
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
                                expirationDelegate
                            ),
                        marshaller,
                        unMarshaller
                    )
                );
        }

        private void Cache
            (
                MemberExpression expression, 
                Func<object, DateTime, bool> expirationDelegate,
                AddOrUpdateDelegate marshaller,
                GetCachedResultDelegate unMarshaller
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
                                expirationDelegate
                            ),
                        marshaller,
                        unMarshaller
                    )
                );
        }

        private IPerMethodAdapter<T> Cache<TReturn>
            (
                Expression<Func<T, TReturn>> target, 
                Func<object,DateTime,bool> expirationDelegate,
                AddOrUpdateDelegate marshaller,
                GetCachedResultDelegate unMarshaller
            )
        {
            MethodCallExpression expression = null;

            switch (target.Body)
            {
                case MemberExpression memberExpression:
                    Cache(memberExpression, expirationDelegate,marshaller,unMarshaller);
                    return this;

                case UnaryExpression unaryExpression:
                    expression = unaryExpression.Operand as MethodCallExpression;
                    break;
            }

            expression = expression ?? target.Body as MethodCallExpression;

            Cache(expression, expirationDelegate, marshaller,unMarshaller);

            return this;
        }

        public override void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType == typeof(void))
            {
                invocation.Proceed();
                return;
            }

            var (expectation, addOrUpdateCache, unMarshallFromCache) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            if (expectation != null)
            {
                var cacheKey = JsonConvert.SerializeObject(invocation.Arguments);

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
                            invocation.ReturnValue = unMarshallFromCache.Invoke(cachedValue.invocationResult);
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
                invocation.Proceed();
            }
        }
    }
}
