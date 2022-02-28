using System;
using System.Linq.Expressions;
using Aop.Cache.ExpirationManagement;
using Xunit;

namespace Aop.Cache.Unit.Tests
{
    public partial class ExpectationTests
    {
        [Fact]
        public void ExpiredExpectationYieldsIsExpiredTrue()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");
            var expiration = While.Result.True<object>(_ => false);

            var expectation = Expectation
                                .FromMethodCallExpression
                                (
                                    expression.Body as MethodCallExpression,
                                    expiration
                                );

            Assert.True(expectation.IsExpired(null));
        }

        [Fact]
        public void NotExpiredExpectationYieldsIsExpiredTrue()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");
            var expiration = While.Result.True<object>(_ => true);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            Assert.False(expectation.IsExpired(null));
        }
    }
}
