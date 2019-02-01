using Aop.Cache.ExpirationManagement;
using System.Linq;
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

            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");

            Assert.Equal(3, instance.InvocationHistory.Count(x => x == "DoStuff"));
        }

        [Fact]
        public void MultipleCachedInvocationsYieldsSingleActualInvocation()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.DoStuff(0,"zero"), While.Result.True<string>((r,dt) => true));

            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");

            Assert.Equal(1, instance.InvocationHistory.Count(x => x=="DoStuff"));
        }

        [Fact]
        public void ExpiredInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.DoStuff(0, "zero"), While.Result.True<string>((r, dt) => false));

            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");
            proxy.DoStuff(1, "one");
            proxy.DoStuff(2, "two");

            Assert.Equal(4, instance.InvocationHistory.Count(x => x == "DoStuff"));
        }

        [Fact]
        public void MixedInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();
            var adapter = new PerMethodAdapter<IForTestingPurposes>(instance);
            var proxy = adapter.Object;

            adapter.Cache(x => x.DoStuff(0, "zero"), While.Result.True<string>((r, dt) => true));

            proxy.DoStuff(0, "zero");
            proxy.DoStuff(0, "zero");
            proxy.DoStuff(1, "one");
            proxy.DoStuff(2, "two");

            Assert.Equal(3, instance.InvocationHistory.Count(x => x == "DoStuff"));
        }
    }
}
