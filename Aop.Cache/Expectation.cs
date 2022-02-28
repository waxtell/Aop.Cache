using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Aop.Cache
{
    public class Expectation
    {
        public Guid Identifier { get; }

        private readonly string _methodName;
        private readonly Type _returnType;
        private readonly IEnumerable<Parameter> _parameters;
        public MemoryCacheEntryOptions Options { get; }

        private Expectation(string methodName, Type returnType, IEnumerable<Parameter> parameters, MemoryCacheEntryOptions options)
        {
            Identifier = Guid.NewGuid();

            _methodName = methodName;
            _returnType = returnType;
            _parameters = parameters;
            Options = options;
        }

        private static Parameter ToParameter(Expression element)
        {
            if (element is ConstantExpression expression)
            {
                return Parameter.MatchExact(expression.Value);
            }

            if (element is MethodCallExpression methodCall)
            {
                if (methodCall.Method.DeclaringType == typeof(It))
                {
                    switch (methodCall.Method.Name)
                    {
                        case nameof(It.IsAny):
                            return Parameter.MatchAny();
                        case nameof(It.IsNotNull):
                            return Parameter.MatchNotNull();
                    }
                }
            }

            return 
                Parameter
                    .MatchExact
                    (
                        Expression
                            .Lambda(Expression.Convert(element, element.Type))
                            .Compile()
                            .DynamicInvoke()
                    );
        }

        public static Expectation FromMethodCallExpression(MethodCallExpression expression, MemoryCacheEntryOptions options)
        {
            return new Expectation
            (
                expression.Method.Name,
                expression.Method.ReturnType,
                expression.Arguments.Select(ToParameter).ToArray(),
                options
            );
        }

        public static Expectation FromMemberAccessExpression(MemberExpression expression, MemoryCacheEntryOptions options)
        {
            var propertyInfo = (PropertyInfo) expression.Member;

            return new Expectation
            (
                propertyInfo.GetMethod.Name,
                propertyInfo.PropertyType,
                new List<Parameter>(), 
                options
            );
        }

        public static Expectation FromInvocation(IInvocation invocation, MemoryCacheEntryOptions options)
        {
            return new Expectation
            (
                invocation.MethodInvocationTarget.Name,
                invocation.MethodInvocationTarget.ReturnType,
                invocation.Arguments.Select(Parameter.MatchExact),
                options
            );
        }

        public bool IsExpired(object cachedValue)
        {
            return 
                Options
                    .ExpirationTokens
                    .OfType<ICacheChangeToken>()
                    .Aggregate(false, (current, item) => current || item.IsExpired(cachedValue));
        }

        public bool IsHit(IInvocation invocation)
        {
            return
                IsHit
                (
                    invocation.Method.Name,
                    invocation.Method.ReturnType,
                    invocation.Arguments
                );
        }

        public bool IsHit(string methodName, Type returnType, object[] arguments)
        {
            if (methodName != _methodName || returnType != _returnType)
            {
                return false;
            }

            if (arguments.Length != _parameters.Count())
            {
                return false;
            }

            for (var i = 0; i < arguments.Length; i++)
            {
                if (!_parameters.ElementAt(i).IsMatch(arguments[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public interface ICacheChangeToken : IChangeToken
    {
        bool IsExpired(object instance);
    }

    public class CacheStuff<T> : ICacheChangeToken
    {
        private CancellationTokenSource _source;
        private CancellationChangeToken _token;
        private readonly Func<T, bool> _monitor;

        public CacheStuff(Func<T, bool> monitor)
        {
            _source = new CancellationTokenSource();
            _token = new CancellationChangeToken(_source.Token);
            _monitor = monitor;
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return _token.RegisterChangeCallback(callback, state);
        }

        public bool IsExpired(object instance)
        {
            if (_monitor.Invoke((T) instance))
            {
                _source.Cancel();
                return true;
            }

            return false;
        }

        public bool HasChanged => _token.HasChanged;

        public bool ActiveChangeCallbacks => _token.ActiveChangeCallbacks;
    }
}
