using System.Diagnostics.CodeAnalysis;
using Aop.Cache.ExpirationManagement;
using Xunit;

namespace Aop.Cache.Unit.Tests
{
    public class PerInstanceAdapterTests
    {
        [Fact]
        public void MultipleCachedInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever())
                            .Object;

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MultipleDistinctCachedInvocationsYieldsSingleActualInvocationPerDistinctInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever());
            var proxy = adapter.Object;

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(1, "zero");
            proxy.MethodCall(1, "zero");
            proxy.MethodCall(2, "zero");
            proxy.MethodCall(2, "zero");

            Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
        }

        [Fact]
        [SuppressMessage("ReSharper", "AssignmentIsFullyDiscarded")]
        public void MultipleDistinctCachedMemberInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever());
            var proxy = adapter.Object;

            _ = proxy.Member;
            _ = proxy.Member;
            _ = proxy.Member;

            Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
        }

        [Fact]
        public void VoidReturnTypeInvocationsAreNotCached()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever());
            var proxy = adapter.Object;

            proxy.Member = "Test";
            proxy.Member = "Test";
            proxy.Member = "Test";

            Assert.Equal<uint>(3, instance.MemberSetInvocationCount);
        }
    }
}