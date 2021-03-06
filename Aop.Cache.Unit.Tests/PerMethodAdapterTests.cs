using System.Threading;
using System.Threading.Tasks;
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
            var proxy = new PerMethodAdapter<IForTestingPurposes>().Adapt(instance);

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MultipleCachedInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.MethodCall(0, "zero"), For.Ever())
                            .Adapt(instance);

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void ExpiredInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.MethodCall(0, "zero"), While.Result.True<string>((result, dt) => false))
                            .Adapt(instance);

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

            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.MethodCall(0, "zero"), For.Ever())
                            .Adapt(instance);

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

            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Adapt(instance);

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
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Adapt(instance);

            proxy.MethodCall(0, "zero");
            var result0 = proxy.MethodCall(0, "zero");

            proxy.MethodCall(1, "zero");
            var result1 = proxy.MethodCall(1, "zero");

            Assert.Equal<uint>(2, instance.MethodCallInvocationCount);
            Assert.Equal("0zero", result0);
            Assert.Equal("1zero", result1);
        }

        [Fact]
        public async Task MultipleCachedAsyncInvocationsYieldsSingleInstanceInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.AsyncMethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Adapt(instance);

            // ReSharper disable once NotAccessedVariable
            // ReSharper disable once RedundantAssignment
            var result = await proxy.AsyncMethodCall(0, "zero");

            // I hate to have to do this, but otherwise the second
            // invocation may complete before the first invocation
            // is added to cache.
            Thread.Sleep(2000);

            result = await proxy.AsyncMethodCall(0, "zero");

            Assert.Equal<uint>(1,instance.AsyncMethodCallInvocationCount);
            Assert.Equal("0zero", result);
        }

        [Fact]
        public void ClassProxyTargetOnlyVirtualMethodsAreCached()
        {
            var instance = new ForTestingPurposes();

            var proxy = new PerMethodAdapter<ForTestingPurposes>()
                            .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Cache(x => x.VirtualMethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Adapt(instance);

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.VirtualMethodCall(0,"zero");
            proxy.VirtualMethodCall(0, "zero");

            Assert.Equal<uint>(2, proxy.MethodCallInvocationCount);
            Assert.Equal<uint>(1, instance.VirtualMethodCallInvocationCount);
            Assert.Equal<uint>(0, instance.MethodCallInvocationCount);
        }

        [Fact]
        public void MultipleMemberInvocationsYieldsSingleInvocation()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.Member, For.Ever())
                            .Adapt(instance);

            proxy.Member = "test";
            // ReSharper disable once RedundantAssignment
            var result = proxy.Member;

            instance.Member = "not equal to test";

            result = proxy.Member;

            Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
            Assert.Equal("test", result);
        }

        [Fact]
        public async Task ExpiredResultYieldsMultipleActualInvocations()
        {
            var instance = new ForTestingPurposes();
            var proxy = new PerMethodAdapter<IForTestingPurposes>()
                            .Cache(x => x.AsyncMethodCall(It.IsAny<int>(), "zero"), While.Result.True<string>((s, dt) => false))
                            .Adapt(instance);

            // ReSharper disable once NotAccessedVariable
            // ReSharper disable once RedundantAssignment
            await proxy.AsyncMethodCall(0, "zero");

            // I hate to have to do this, but otherwise the second
            // invocation may complete before the first invocation
            // is added to cache.
            Thread.Sleep(2000);

            await proxy.AsyncMethodCall(0, "zero");

            Assert.Equal<uint>(2, instance.AsyncMethodCallInvocationCount);
        }
    }
}
