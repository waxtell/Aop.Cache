using System.Threading.Tasks;
using Aop.Cache.ExpirationManagement;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aop.Cache.Unit.Tests;

public class PerInstanceAdapterTests
{
    [Fact]
    public void MultipleCachedInvocationsYieldsSingleActualInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
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
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
            .Adapt(instance);

        _ = await proxy.AsyncMethodCall(0, "zero");

        await Task.Delay(2000);

        _ = await proxy.AsyncMethodCall(0, "zero");
            
        Assert.Equal<uint>(1, instance.AsyncMethodCallInvocationCount);
    }

    [Fact]
    public void MultipleDistinctCachedInvocationsYieldsSingleActualInvocationPerDistinctInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
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
    public void MultipleDistinctCachedMemberInvocationsYieldsSingleActualInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
            .Adapt(instance);

        _ = proxy.Member;
        _ = proxy.Member;
        _ = proxy.Member;

        Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
    }

    [Fact]
    public void VoidReturnTypeInvocationsAreNotCached()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
            .Adapt(instance);

        proxy.Member = "Test";
        proxy.Member = "Test";
        proxy.Member = "Test";

        Assert.Equal<uint>(3, instance.MemberSetInvocationCount);
    }

    [Fact]
    public async Task AsyncActionInvocationsAreNotCached()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever())
            .Adapt(instance);

        await proxy.AsyncAction(0, 1, "two");

        Assert.Equal<uint>(1, instance.AsyncActionCallInvocationCount);
    }

    [Fact]
    public async Task ExpiredResultYieldsMultipleActualInvocations()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Milliseconds(1))
            .Adapt(instance);

        _ = await proxy.AsyncMethodCall(0, "zero");

        await Task.Delay(2000);

        await proxy.AsyncMethodCall(0, "zero");

        Assert.Equal<uint>(2, instance.AsyncMethodCallInvocationCount);
    }

    [Fact]
    public void MultipleInstancesYieldsSingleActualInvocationPerDistinctInvocation()
    {
        var instance1 = new ForTestingPurposes();
        var instance2 = new ForTestingPurposes();

        var adapter = new PerInstanceAdapter<IForTestingPurposes>(CacheFactory(), For.Ever());
        var proxy1 = adapter.Adapt(instance1);
        var proxy2 = adapter.Adapt(instance2);

        proxy1.MethodCall(0, "zero");
        proxy1.MethodCall(0, "zero");
        proxy1.MethodCall(1, "zero");
        proxy1.MethodCall(1, "zero");
        proxy1.MethodCall(2, "zero");
        proxy1.MethodCall(2, "zero");

        proxy2.MethodCall(0, "zero");
        proxy2.MethodCall(0, "zero");
        proxy2.MethodCall(1, "zero");
        proxy2.MethodCall(1, "zero");
        proxy2.MethodCall(2, "zero");
        proxy2.MethodCall(2, "zero");

        Assert.Equal<uint>(3, instance1.MethodCallInvocationCount);
        Assert.Equal<uint>(0, instance2.MethodCallInvocationCount);
    }
    public static IMemoryCache CacheFactory()
    {
        return
            new MemoryCache(Options.Create(new MemoryCacheOptions()));
    }
}