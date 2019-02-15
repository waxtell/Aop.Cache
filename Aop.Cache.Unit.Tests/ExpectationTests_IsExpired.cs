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
            var expiration = While.Result.True<object>((i, dt) => false);

            var expectation = Expectation
                                .FromMethodCallExpression
                                (
                                    expression.Body as MethodCallExpression,
                                    expiration
                                );

            Assert.True(expectation.IsExpired(null, DateTime.UtcNow));
        }

        [Fact]
        public void NotExpiredExpectationYieldsIsExpiredTrue()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");
            var expiration = While.Result.True<object>((i, dt) => true);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            Assert.False(expectation.IsExpired(null, DateTime.UtcNow));
        }
    }
}
