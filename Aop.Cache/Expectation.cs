using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Aop.Cache.ExpirationManagement;
using Castle.DynamicProxy;

namespace Aop.Cache
{
    internal class Expectation
    {
        public Guid Identifier { get; }

        private readonly string _methodName;
        private readonly Type _returnType;
        private readonly IEnumerable<Parameter> _parameters;
        private readonly IExpirationDelegate _expiration;

        private Expectation(string methodName, Type returnType, IEnumerable<Parameter> parameters, IExpirationDelegate expiration)
        {
            Identifier = Guid.NewGuid();

            _methodName = methodName;
            _returnType = returnType;
            _parameters = parameters;
            _expiration = expiration;
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
                    if (methodCall.Method.Name == nameof(It.IsAny))
                    {
                        return Parameter.MatchAny();
                    }

                    if (methodCall.Method.Name == nameof(It.IsNotNull))
                    {
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

        public static Expectation FromMethodCallExpression(MethodCallExpression expression, IExpirationDelegate expirationDelegate)
        {
            return new Expectation
            (
                expression.Method.Name,
                expression.Method.ReturnType,
                expression.Arguments.Select(ToParameter).ToArray(),
                expirationDelegate
            );
        }

        public static Expectation FromMemberAccessExpression(MemberExpression expression, IExpirationDelegate expirationDelegate)
        {
            var propertyInfo = (PropertyInfo) expression.Member;

            return new Expectation
            (
                propertyInfo.GetMethod.Name,
                propertyInfo.PropertyType,
                new List<Parameter>(), 
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
}
