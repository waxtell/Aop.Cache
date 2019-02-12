using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache
{
    public class PerInstanceAdapter<T> : IInterceptor, IPerInstanceAdapter<T> where T : class
    {
        public T Object { get; }
        private readonly Func<object,DateTime,bool> _expirationDelegate;
        private readonly List<Expectation> _expectations = new List<Expectation>();
        private readonly Dictionary<Guid, Dictionary<string,(object invocationResult,DateTime invocationDateTime)>> _cachedInvocations = new Dictionary<Guid, Dictionary<string,(object invocationResult, DateTime invocationDateTime)>>();

        public void Intercept(IInvocation invocation)
        {
            if(invocation.Method.ReturnType == typeof(void))
            {
                invocation.Proceed();
                return;
            }

            var expectation = _expectations.FirstOrDefault(x => x.IsHit(invocation));
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

                            cachedInvocation[cacheKey] = (invocation.ReturnValue, DateTime.UtcNow);
                        }
                        else
                        {
                            invocation.ReturnValue = cachedValue.invocationResult;
                        }
                    }
                    else
                    {
                        invocation.Proceed();

                        cachedInvocation
                            .Add
                            (
                                cacheKey,
                                (invocation.ReturnValue, DateTime.UtcNow)
                            );
                    }
                }
                else
                {
                    invocation.Proceed();

                    _cachedInvocations
                        .Add
                        (
                            expectation.Identifier,
                            new Dictionary<string, (object invocationResult, DateTime invocationDateTime)>
                                {
                                    {
                                        cacheKey,
                                        (invocation.ReturnValue, DateTime.UtcNow)
                                    }
                                }
                        );
                }
            }
            else
            {
                expectation = Expectation.FromInvocation(invocation, _expirationDelegate);
                _expectations.Add(expectation);

                invocation.Proceed();

                _cachedInvocations
                    .Add
                    (
                        expectation.Identifier,
                        new Dictionary<string, (object invocationResult, DateTime invocationDateTime)>
                        {
                            {
                                cacheKey,
                                (invocation.ReturnValue, DateTime.UtcNow)
                            }
                        }
                    );
            }
        }

        public PerInstanceAdapter(T instance, Func<DateTime,bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);
            _expirationDelegate = expr.Compile();

            Object = new ProxyGenerator()
                .CreateInterfaceProxyWithTarget(instance, this);
        }
    }
}
