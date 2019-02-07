using Aop.Cache.ExpirationManagement;
using Xunit;

namespace Aop.Cache.Unit.Tests
{
    public class PerMethodAdapterTests
    {
        [Fact]
        public void MultipleNonCachedInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MultipleCachedInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.MethodCall(0,"zero"), While.Result.True<string>((r,dt) => true));

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void ExpiredInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.MethodCall(0, "zero"), While.Result.True<string>((r, dt) => false));

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(1, "one");
            proxy.MethodCall(2, "two");

            Assert.Equal<uint>(4, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MixedInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.MethodCall(0, "zero"), While.Result.True<string>((r, dt) => true));

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(1, "one");
            proxy.MethodCall(2, "two");

            Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MixedFuzzyInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), While.Result.True<string>((r, dt) => true));

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(1, "zero");
            proxy.MethodCall(1, "zero");
            proxy.MethodCall(2, "zero");
            proxy.MethodCall(2, "zero");

            Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MultipleCacheExpectationsYieldExpectedResult()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), While.Result.True<string>((r, dt) => true));

            proxy.MethodCall(0, "zero");
            var result0 = proxy.MethodCall(0, "zero");

            proxy.MethodCall(1, "zero");
            var result1 = proxy.MethodCall(1, "zero");

            Assert.Equal<uint>(2, instance.MethodCallInvocationCount);
            Assert.Equal("0zero", result0);
            Assert.Equal("1zero", result1);
        }

        [Fact]
        public void MultipleMemberInvocationsYieldsSingleInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.Member, While.Result.True<string>((r, dt) => true));

            proxy.Member = "test";
            // ReSharper disable once RedundantAssignment
            var result = proxy.Member;

            instance.Member = "not equal to test";

            result = proxy.Member;

            Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
            Assert.Equal("test", result);
        }
    }
}
