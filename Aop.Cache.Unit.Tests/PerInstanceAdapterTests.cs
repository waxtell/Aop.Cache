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
            var adapter = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever());
            var proxy = adapter.Object;

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
        public void MultipleDistinctCachedMemberInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever());
            var proxy = adapter.Object;

            // ReSharper disable once NotAccessedVariable
            var memberValue = proxy.Member;
            memberValue = proxy.Member;
            memberValue = proxy.Member;

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
