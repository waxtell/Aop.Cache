using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache
{
    public class PerMethodAdapter<T> : IInterceptor, IPerMethodAdapter<T> where T : class
    {
        public T Object { get; }

        private readonly List<Expectation> _expectations = new List<Expectation>();
        private readonly Dictionary<Guid, Dictionary<string,(object invocationResult,DateTime invocationDateTime)>> _cachedInvocations = new Dictionary<Guid, Dictionary<string,(object invocationResult, DateTime invocationDateTime)>>();

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<TReturn, DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate((TReturn)i, d);
            return Cache(target, expr.Compile());
        }

        public IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<DateTime, bool> expirationDelegate)
        {
            Expression<Func<object, DateTime, bool>> expr = (i, d) => expirationDelegate(d);
            return Cache(target, expr.Compile());
        }

        private void Cache(MethodCallExpression expression, Func<object, DateTime, bool> expirationDelegate)
        {
            _expectations.Add(Expectation.FromMethodCallExpression(expression, expirationDelegate));
        }

        private void Cache(MemberExpression expression, Func<object, DateTime, bool> expirationDelegate)
        {
            _expectations.Add(Expectation.FromMemberAccessExpression(expression, expirationDelegate));
        }

        private IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<object,DateTime,bool> expirationDelegate)
        {
            MethodCallExpression expression = null;

            switch (target.Body)
            {
                case MemberExpression memberExpression:
                    Cache(memberExpression, expirationDelegate);
                    return this;

                case UnaryExpression unaryExpression:
                    expression = unaryExpression.Operand as MethodCallExpression;
                    break;
            }

            expression = expression ?? target.Body as MethodCallExpression;
            
            Cache(expression, expirationDelegate);

            return this;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType == typeof(void))
            {
                invocation.Proceed();
                return;
            }

            var expectation = _expectations.FirstOrDefault(x => x.IsHit(invocation));

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
                invocation.Proceed();
            }
        }

        public PerMethodAdapter(T instance)
        {
            Object = new ProxyGenerator()
                        .CreateInterfaceProxyWithTarget(instance, this);
        }
    }
}
