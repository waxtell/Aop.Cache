using System;
using System.Linq.Expressions;
using System.Reflection;
using Moq;
using Xunit;

namespace Aop.Cache.Unit.Tests;

public class ExpectationTests
{
    [Fact]
    public void SignatureMatchYieldsHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

        Assert.True(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void FuzzyMatchYieldsHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(It.IsAny<int>(), "zero");

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));
        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 42, "zero" });

        Assert.True(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void NotNullExpectationNotNullValueYieldsHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, It.IsNotNull<string>());

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

        Assert.True(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void NotNullExpectationNullParameterYieldsNoHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, It.IsNotNull<string>());

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 42, null });

        Assert.False(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void ReturnTypeMismatchYieldsNoHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(int));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

        Assert.False(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void ParameterMismatchYieldsNoHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("MethodCall");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 1, "zero" });

        Assert.False(expectation.IsHit(invocation.Object));
    }

    [Fact]
    public void MethodNameMismatchYieldsNoHit()
    {
        Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");

        var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression);

        var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
        var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

        methodInfo.Setup(x => x.Name).Returns("DoOtherStuff");
        methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

        invocation.Setup(x => x.TargetType).Returns(typeof(ForTestingPurposes));
        invocation.Setup(x => x.Method).Returns(methodInfo.Object);
        invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

        Assert.False(expectation.IsHit(invocation.Object));
    }
}

