using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(For.Ever())
                            .Adapt(instance);

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
        }

        [Fact]
        public async Task MultipleCachedAsyncInvocationsYieldsSingleInstanceInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(For.Ever())
                            .Adapt(instance);

            // ReSharper disable once NotAccessedVariable
            var result = await proxy.AsyncMethodCall(0, "zero");

            Thread.Sleep(2000);

            // ReSharper disable once RedundantAssignment
            result = await proxy.AsyncMethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.AsyncMethodCallInvocationCount);
        }

        [Fact]
        public void MultipleDistinctCachedInvocationsYieldsSingleActualInvocationPerDistinctInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(For.Ever()).Adapt(instance);

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
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(For.Ever()).Adapt(instance);

            _ = proxy.Member;
            _ = proxy.Member;
            _ = proxy.Member;

            Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
        }

        [Fact]
        public void VoidReturnTypeInvocationsAreNotCached()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(For.Ever()).Adapt(instance);

            proxy.Member = "Test";
            proxy.Member = "Test";
            proxy.Member = "Test";

            Assert.Equal<uint>(3, instance.MemberSetInvocationCount);
        }
    }
}
