using System;
using System.Linq.Expressions;
using System.Reflection;
using Aop.Cache.ExpirationManagement;
using Moq;
using Xunit;

namespace Aop.Cache.Unit.Tests
{
    public partial class ExpectationTests
    {
        [Fact]
        public void SignatureMatchYieldsHit()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.DoStuff(0,"zero");
            var expiration = For.Milliseconds(0);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
            var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

            methodInfo.Setup(x => x.Name).Returns("DoStuff");
            methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

            invocation.Setup(x => x.Method).Returns(methodInfo.Object);
            invocation.Setup(x => x.Arguments).Returns(new object[] {0, "zero"});

            Assert.True(expectation.IsHit(invocation.Object));
        }

        [Fact]
        public void ReturnTypeMismatchYieldsNoHit()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.DoStuff(0, "zero");
            var expiration = For.Milliseconds(0);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
            var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

            methodInfo.Setup(x => x.Name).Returns("DoStuff");
            methodInfo.Setup(x => x.ReturnType).Returns(typeof(int));

            invocation.Setup(x => x.Method).Returns(methodInfo.Object);
            invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

            Assert.False(expectation.IsHit(invocation.Object));
        }

        [Fact]
        public void ParameterMismatchYieldsNoHit()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.DoStuff(0, "zero");
            var expiration = For.Milliseconds(0);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
            var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

            methodInfo.Setup(x => x.Name).Returns("DoStuff");
            methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

            invocation.Setup(x => x.Method).Returns(methodInfo.Object);
            invocation.Setup(x => x.Arguments).Returns(new object[] { 1, "zero" });

            Assert.False(expectation.IsHit(invocation.Object));
        }

        [Fact]
        public void MethodNameMismatchYieldsNoHit()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.DoStuff(0, "zero");
            var expiration = For.Milliseconds(0);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>(MockBehavior.Strict);
            var methodInfo = new Mock<MethodInfo>(MockBehavior.Strict);

            methodInfo.Setup(x => x.Name).Returns("DoOtherStuff");
            methodInfo.Setup(x => x.ReturnType).Returns(typeof(string));

            invocation.Setup(x => x.Method).Returns(methodInfo.Object);
            invocation.Setup(x => x.Arguments).Returns(new object[] { 0, "zero" });

            Assert.False(expectation.IsHit(invocation.Object));
        }
    }
}
