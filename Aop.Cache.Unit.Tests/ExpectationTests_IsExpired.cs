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
            Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0,"zero");
            var expiration = While.Result.True<string>((i, dt) => false);

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, expiration);

            Assert.True(expectation.IsExpired(null, DateTime.UtcNow));
        }

        [Fact]
        public void NotExpiredExpectationYieldsIsExpiredTrue()
        {
            Expression<Func<ForTestingPurposes, string>> expression = s => s.MethodCall(0, "zero");

            var expectation = Expectation.FromMethodCallExpression(expression.Body as MethodCallExpression, For.Ever());

            Assert.False(expectation.IsExpired(null, DateTime.UtcNow));
        }
    }
}
