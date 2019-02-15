using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache
{
    public class PerMethodAdapter<T> : IInterceptor, IPerMethodAdapter<T> where T : class
    {
        public T Object { get; }

        private readonly List
                            <
                                (
                                    Expectation expectation, 
                                    Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> marshaller,
                                    Func<object,object> unmarshaller
                                )
                            > 
                            _expectations = new List
                            <
                                (
                                    Expectation, 
                                    Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string>,
                                    Func<object, object> unmarshaller
                                )
                            >();

        private readonly Dictionary<Guid, Dictionary<string,(object invocationResult,DateTime invocationDateTime)>> _cachedInvocations = new Dictionary<Guid, Dictionary<string,(object invocationResult, DateTime invocationDateTime)>>();

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target,
            Func<TReturn, DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate((TReturn)i, d);

            return 
                Cache
                (
                    target, 
                    expr.Compile(), 
                    BuildAsyncResultMarshaller<TReturn>(),
                    BuildAsyncUnMarshaller<TReturn>()
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
                    BuildAsyncResultMarshaller<TReturn>(),
                    BuildAsyncUnMarshaller<TReturn>()
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
                    BuildDefaultResultMarshaller(),
                    BuildDefaultUnMarshaller()
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
                    BuildDefaultResultMarshaller(),
                    BuildDefaultUnMarshaller()
                );
        }

        private void Cache
            (
                MethodCallExpression expression, 
                Func<object, DateTime, bool> expirationDelegate, 
                Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> marshaller,
                Func<object,object> unMarshaller
            )
        {
            _expectations
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
                Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> marshaller,
                Func<object,object> unMarshaller
            )
        {
            _expectations
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
                Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> marshaller,
                Func<object, object> unMarshaller
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

        private static void AddOrUpdate(IDictionary<string, (object invocationResult, DateTime invocationDateTime)> cache, string cacheKey, object result)
        {
            cache[cacheKey] = (result, DateTime.UtcNow);
        }

        private static Func<object, object> BuildDefaultUnMarshaller()
        {
            Expression<Func<object,object>> expr = (returnValue) => returnValue;

            return expr.Compile();
        }

        private static Func<object, object> BuildAsyncUnMarshaller<TReturn>()
        {
            Expression<Func<object, object>> expr = 
                (returnValue) => Task.FromResult((TReturn) returnValue);

            return expr.Compile();
        }

        private static Action<object,IDictionary<string, (object invocationResult, DateTime invocationDateTime)>,string> BuildAsyncResultMarshaller<TReturn>()
        {
            Expression
            <
                Action
                <
                    object,
                    IDictionary<string, (object invocationResult, DateTime invocationDateTime)>,
                    string
                >
            > 
            expr =
                (returnValue, cache, cacheKey) => (returnValue as Task<TReturn>)
                                                    .ContinueWith
                                                    (
                                                        i => AddOrUpdate(cache, cacheKey, i.Result)
                                                    );

            return expr.Compile();
        }

        private static Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> BuildDefaultResultMarshaller()
        {
            Expression
            <
                Action
                <
                    object,
                    IDictionary<string, (object invocationResult, DateTime invocationDateTime)>,
                    string
                >
            >
            expr =
                (returnValue, cache, cacheKey) => AddOrUpdate(cache, cacheKey, returnValue);

            return expr.Compile();
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType == typeof(void))
            {
                invocation.Proceed();
                return;
            }

            var (expectation, addOrUpdateCache, unMarshallFromCache) = _expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            if (expectation != null)
            {
                var cacheKey = JsonConvert.SerializeObject(invocation.Arguments);

                if (_cachedInvocations.TryGetValue(expectation.Identifier, out var cachedInvocation))
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

                    _cachedInvocations
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

        public PerMethodAdapter(T instance)
        {
            if (typeof(T).GetTypeInfo().IsInterface)
            {
                Object = new ProxyGenerator()
                            .CreateInterfaceProxyWithTarget(instance, this);
            }
            else
            {
                Object = new ProxyGenerator()
                            .CreateClassProxyWithTarget(instance,this);
            }
        }
    }
}
