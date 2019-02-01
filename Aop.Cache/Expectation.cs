using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Aop.Cache.ExpirationManagement;
using Castle.DynamicProxy;

namespace Aop.Cache
{
    internal class Expectation
    {
        public Guid Identifier { get; }

        private readonly string _methodName;
        private readonly Type _returnType;
        private readonly IEnumerable<int> _argumentHashCodes;
        private readonly IExpirationDelegate _expiration;

        private Expectation(string methodName, Type returnType, IEnumerable<object> arguments, IExpirationDelegate expiration)
        {
            Identifier = Guid.NewGuid();

            _methodName = methodName;
            _returnType = returnType;
            _argumentHashCodes = arguments.Select(x => x.GetHashCode());
            _expiration = expiration;
        }

        private static object GetArgumentValue(Expression element)
        {
            if (element is ConstantExpression expression)
            {
                return expression.Value;
            }

            return 
                Expression
                    .Lambda(Expression.Convert(element, element.Type))
                    .Compile()
                    .DynamicInvoke();
        }

        public static Expectation FromMethodCallExpression(MethodCallExpression expression, IExpirationDelegate expirationDelegate)
        {
            return new Expectation
            (
                expression.Method.Name,
                expression.Method.ReturnType,
                expression.Arguments.Select(GetArgumentValue),
                expirationDelegate
            );
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

        public bool IsExpired(object cachedValue, DateTime executionDateTimeUtc)
        {
            return _expiration.HasExpired(cachedValue, executionDateTimeUtc);
        }

        public bool IsHit(string methodName, Type returnType, object[] arguments)
        {
            return
            (
                _methodName == methodName &&
                _returnType == returnType &&
                arguments.Select(x => x.GetHashCode()).SequenceEqual(_argumentHashCodes)
            );
        }
    }
}
