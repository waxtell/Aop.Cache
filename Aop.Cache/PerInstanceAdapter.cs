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
    public class PerInstanceAdapter<T> : IInterceptor, IPerInstanceAdapter<T> where T : class
    {
        public T Object { get; }
        private readonly Func<object, DateTime, bool> _expirationDelegate;

        private readonly List
                            <
                                (
                                    Expectation expectation,
                                    Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> marshaller,
                                    Func<object, object> unmarshaller
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

        private readonly Dictionary<Guid, Dictionary<string, (object invocationResult, DateTime invocationDateTime)>> _cachedInvocations = new Dictionary<Guid, Dictionary<string, (object invocationResult, DateTime invocationDateTime)>>();

        private static void AddOrUpdate(IDictionary<string, (object invocationResult, DateTime invocationDateTime)> cache, string cacheKey, object result)
        {
            cache[cacheKey] = (result, DateTime.UtcNow);
        }

        private static Func<object, object> BuildDefaultUnMarshaller()
        {
            Expression<Func<object, object>> expr = (returnValue) => returnValue;

            return expr.Compile();
        }

        private static Func<object, object> BuildAsyncUnMarshaller<TReturn>()
        {
            Expression<Func<object, object>> expr =
                (returnValue) => Task.FromResult((TReturn)returnValue);

            return expr.Compile();
        }
        private static Func<object, object> BuildAsyncUnMarshallerForType(Type tReturn)
        {
            var mi = typeof(PerInstanceAdapter<T>)
                .GetMethod(nameof(BuildAsyncUnMarshaller), BindingFlags.NonPublic | BindingFlags.Static);

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (Func<object, object>) miConstructed?.Invoke(null, null);
        }

        private static Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> BuildAsyncResultMarshaller<TReturn>()
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
            var cacheKey = JsonConvert.SerializeObject(invocation.Arguments);

            if (expectation != null)
            {
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
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation, _expirationDelegate);
                addOrUpdateCache = GetMarshallerForType(returnType);
                unMarshallFromCache = GetUnMarshallerForType(returnType);

                _expectations
                    .Add
                    (
                        (
                            expectation,
                            addOrUpdateCache,
                            unMarshallFromCache
                        )
                    );

                var cache = new Dictionary<string, (object invocationResult, DateTime invocationDateTime)>();
                _cachedInvocations.Add(expectation.Identifier, cache);

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

        private static Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string>
            GetMarshallerForType(Type tReturn)
        {
            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null && returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildAsyncResultMarshallerForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildDefaultResultMarshaller();
        }

        private static Func<object, object> GetUnMarshallerForType(Type tReturn)
        {
            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null && returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildAsyncUnMarshallerForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildDefaultUnMarshaller();
        }

        private static Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string> BuildAsyncResultMarshallerForType(Type tReturn)
        {
            var mi = typeof(PerInstanceAdapter<T>)
                        .GetMethod(nameof(BuildAsyncResultMarshaller), BindingFlags.NonPublic | BindingFlags.Static);

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (Action<object, IDictionary<string, (object invocationResult, DateTime invocationDateTime)>, string>) miConstructed?.Invoke(null,null);
        }

        public PerInstanceAdapter(T instance, Func<DateTime,bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);
            _expirationDelegate = expr.Compile();

            if (typeof(T).GetTypeInfo().IsInterface)
            {
                Object = new ProxyGenerator()
                    .CreateInterfaceProxyWithTarget(instance, this);
            }
            else
            {
                Object = new ProxyGenerator()
                    .CreateClassProxyWithTarget(instance, this);
            }
        }
    }
}
