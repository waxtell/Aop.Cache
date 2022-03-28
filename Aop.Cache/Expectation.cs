using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Cache;

public class Expectation
{
    private readonly string _methodName;
    private readonly Type _returnType;
    private readonly Type _instanceType;
    private readonly IEnumerable<Parameter> _parameters;

    private Expectation(Type instanceType, string methodName, Type returnType, IEnumerable<Parameter> parameters)
    {
        _instanceType = instanceType;
        _methodName = methodName;
        _returnType = returnType;
        _parameters = parameters;
    }

    private static Parameter ToParameter(Expression element)
    {
        if (element is ConstantExpression expression)
        {
            return Parameter.MatchExact(expression.Value);
        }

        if (element is MethodCallExpression methodCall && methodCall.Method.DeclaringType == typeof(It))
        {
            switch (methodCall.Method.Name)
            {
                case nameof(It.IsIgnored):
                    return Parameter.Ignore();
                case nameof(It.IsAny):
                    return Parameter.MatchAny();
                case nameof(It.IsNotNull):
                    return Parameter.MatchNotNull();
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

    public static Expectation FromMethodCallExpression(MethodCallExpression expression)
    {
        return new Expectation
        (
            expression!.Object!.Type,
            expression.Method.Name,
            expression.Method.ReturnType,
            expression.Arguments.Select(ToParameter).ToArray()
        );
    }

    public static Expectation FromMemberAccessExpression(MemberExpression expression)
    {
        var propertyInfo = (PropertyInfo) expression.Member;

        return new Expectation
        (
            expression.Expression.Type,
            propertyInfo.GetMethod.Name,
            propertyInfo.PropertyType,
            new List<Parameter>()
        );
    }

    public static Expectation FromInvocation(IInvocation invocation)
    {
        return new Expectation
        (
            invocation.TargetType,
            invocation.MethodInvocationTarget.Name,
            invocation.MethodInvocationTarget.ReturnType,
            invocation.Arguments.Select(Parameter.MatchExact)
        );
    }

    public bool IsHit(IInvocation invocation)
    {
        return
            IsHit
            (
                invocation.TargetType,
                invocation.Method.Name,
                invocation.Method.ReturnType,
                invocation.Arguments
            );
    }

    public bool IsHit(Type targetType, string methodName, Type returnType, object[] arguments)
    {
        if (!_instanceType.IsAssignableFrom(targetType) || methodName != _methodName || returnType != _returnType)
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

    public string GetCacheKey(IInvocation invocation)
    {
        return
            JsonConvert
                .SerializeObject
                (
                    new
                    {
                        TypeName = invocation.InvocationTarget.GetType().AssemblyQualifiedName,
                        MethodName = invocation.Method.Name,
                        ReturnType = invocation.Method.ReturnType.Name,
                        Arguments = GetArguments()
                    }
                );

        IEnumerable<object> GetArguments()
        {
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                if (_parameters.ElementAt(i).IsEvaluated())
                {
                    yield return invocation.Arguments[i];
                }
            }
        }
    }
}