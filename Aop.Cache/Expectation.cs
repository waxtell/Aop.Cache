﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache;

public class Expectation
{
    private readonly string _methodName;
    private readonly Type _returnType;
    private readonly Type _instanceType;
    private readonly IEnumerable<Parameter> _parameters;

    private readonly Func<IMemoryCache, string, MemoryCacheEntryOptions> _optionsFactory;

    private Expectation(Type instanceType, string methodName, Type returnType, IEnumerable<Parameter> parameters, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
    {
        _instanceType = instanceType;
        _methodName = methodName;
        _returnType = returnType;
        _parameters = parameters;
        _optionsFactory = optionsFactory;
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

    public static Expectation FromMethodCallExpression(MethodCallExpression expression, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
    {
        return new Expectation
        (
            expression!.Object!.Type,
            expression.Method.Name,
            expression.Method.ReturnType,
            expression.Arguments.Select(ToParameter).ToArray(),
            optionsFactory
        );
    }

    public static Expectation FromMemberAccessExpression(MemberExpression expression, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
    {
        var propertyInfo = (PropertyInfo) expression.Member;

        return new Expectation
        (
            expression.Expression.Type,
            propertyInfo.GetMethod.Name,
            propertyInfo.PropertyType,
            new List<Parameter>(), 
            optionsFactory
        );
    }

    public static Expectation FromInvocation(IInvocation invocation, Func<IMemoryCache,string,MemoryCacheEntryOptions> optionsFactory)
    {
        return new Expectation
        (
            invocation.TargetType,
            invocation.MethodInvocationTarget.Name,
            invocation.MethodInvocationTarget.ReturnType,
            invocation.Arguments.Select(Parameter.MatchExact),
            optionsFactory
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

    public MemoryCacheEntryOptions GetCacheEntryOptions(IMemoryCache memoryCache, string cacheKey)
    {
        return
            _optionsFactory
                .Invoke(memoryCache, cacheKey);
    }
}