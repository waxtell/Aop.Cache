using System;
using System.Threading.Tasks;
using Aop.Cache.ExpirationManagement;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aop.Cache.Unit.Tests;

public class PerMethodAdapterTests
{
    [Fact]
    public void ThrownExceptionsAreCachedTest()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory(), options => options.CacheExceptions = true)
            .Cache(x => x.ThrowsException(It.IsAny<int>()), For.Ever())
            .Adapt(instance);

        Assert.Throws<Exception>(() => proxy.ThrowsException(0));
        Assert.Throws<Exception>(() => proxy.ThrowsException(0));
        Assert.Equal<uint>(1, instance.ThrowExceptionInvocationCount);
    }

    [Fact]
    public async Task AsynchronousThrownExceptionsAreCachedTest()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory(), options => options.CacheExceptions = true)
            .Cache(x => x.ThrowsExceptionAsync(It.IsAny<int>()), For.Ever())
            .Adapt(instance);

        await Assert.ThrowsAsync<Exception>(async () => await proxy.ThrowsExceptionAsync(0));
        await Assert.ThrowsAsync<Exception>(async () => await proxy.ThrowsExceptionAsync(0));
        Assert.Equal<uint>(1, instance.ThrowExceptionAsyncInvocationCount);
    }

    [Fact]
    public async Task AsynchronousThrownExceptionsAreNotCachedTest()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory(), options => options.CacheExceptions = false)
            .Cache(x => x.ThrowsExceptionAsync(It.IsAny<int>()), For.Ever())
            .Adapt(instance);

        await Assert.ThrowsAsync<Exception>(async () => await proxy.ThrowsExceptionAsync(0));
        await Assert.ThrowsAsync<Exception>(async () => await proxy.ThrowsExceptionAsync(0));
        Assert.Equal<uint>(2, instance.ThrowExceptionAsyncInvocationCount);
    }

    [Fact]
    public void MultipleNonCachedInvocationsYieldsMultipleInvocations()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
            .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");

        Assert.Equal<uint>(3, instance.MethodCallInvocationCount);
    }

    [Fact]
    public void MultipleCachedInvocationsWithIgnoredParameterYieldsSingleActualInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
            .Cache(x => x.MethodCall(0, It.IsIgnored<string>()), For.Ever())
            .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "one");
        proxy.MethodCall(0, "two");

        Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
    }

    [Fact]
    public void MultipleCachedInvocationsYieldsSingleActualInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
            .Cache(x => x.MethodCall(0, "zero"), For.Ever())
            .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");

        Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
    }

    [Fact]
    public void MixedInvocationsYieldsMultipleInvocations()
    {
        var instance = new ForTestingPurposes();

        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
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

        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
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
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
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
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                        .Cache(x => x.AsyncMethodCall(It.IsAny<int>(), "zero"), For.Ever())
                        .Adapt(instance);

        await proxy.AsyncMethodCall(0, "zero");

        await Task.Delay(2000);

        var result = await proxy.AsyncMethodCall(0, "zero");

        Assert.Equal<uint>(1, instance.AsyncMethodCallInvocationCount);
        Assert.Equal("0zero", result);
    }

    [Fact]
    public void ClassProxyTargetOnlyVirtualMethodsAreCached()
    {
        var instance = new ForTestingPurposes();

        var proxy = new PerMethodAdapter<ForTestingPurposes>(CacheFactory())
                        .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Ever())
                        .Cache(x => x.VirtualMethodCall(It.IsAny<int>(), "zero"), For.Ever())
                        .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");
        proxy.VirtualMethodCall(0, "zero");
        proxy.VirtualMethodCall(0, "zero");

        Assert.Equal<uint>(2, proxy.MethodCallInvocationCount);
        Assert.Equal<uint>(1, instance.VirtualMethodCallInvocationCount);
        Assert.Equal<uint>(0, instance.MethodCallInvocationCount);
    }

    [Fact]
    public void MultipleMemberInvocationsYieldsSingleInvocation()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                        .Cache(x => x.Member, For.Ever())
                        .Adapt(instance);

        proxy.Member = "test";

        _ = proxy.Member;

        instance.Member = "not equal to test";

        var result = proxy.Member;

        Assert.Equal<uint>(1, instance.MemberGetInvocationCount);
        Assert.Equal("test", result);
    }

    [Fact]
    public async Task ExpiredResultYieldsMultipleActualInvocations()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
            .Cache(x => x.AsyncMethodCall(It.IsAny<int>(), "zero"), For.Milliseconds(1))
            .Adapt(instance);

        await proxy.AsyncMethodCall(0, "zero");

        await Task.Delay(2000);

        await proxy.AsyncMethodCall(0, "zero");

        Assert.Equal<uint>(2, instance.AsyncMethodCallInvocationCount);
    }

    [Fact]
    public async Task ExcludedValueIsNotCachedAsync() 
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                        .Cache
                        (
                            x => x.AsyncMethodCall(It.IsAny<int>(), It.IsAny<string>()), 
                            For.Ever(), 
                            s => s.Result == "0zero"
                        )
                        .Adapt(instance);

        await proxy.AsyncMethodCall(0, "zero");
        await proxy.AsyncMethodCall(0, "zero");

        Assert.Equal<uint>(2, instance.AsyncMethodCallInvocationCount);
    }

    [Fact]
    public void ExcludedValueIsNotCached() 
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                        .Cache
                        (
                            x => x.MethodCall(It.IsAny<int>(), It.IsAny<string>()),
                            For.Ever(),
                            s => s == "0zero"
                        )
                        .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(0, "zero");

        Assert.Equal<uint>(2, instance.MethodCallInvocationCount);
    }

    [Fact]
    public void ExcludedNullIsNotCached()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
            .Cache
            (
                x => x.ReturnsNullForOddNumbers(It.IsAny<int>()),
                For.Ever(),
                s => s == null
            )
            .Adapt(instance);

        proxy.ReturnsNullForOddNumbers(3);
        proxy.ReturnsNullForOddNumbers(3);
        proxy.ReturnsNullForOddNumbers(4);

        Assert.Equal<uint>(3, instance.ReturnsNullForOddNumbersInvocationCount);
    }


    [Fact]
    public void MultipleExcludedValuesAreNotCached() 
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                        .Cache
                        (
                            x => x.MethodCall(It.IsAny<int>(), It.IsAny<string>()),
                            For.Ever(),
                            x => x == "0zero", 
                            y => y == null
                        )
                        .Adapt(instance);

        proxy.MethodCall(0, "zero");
        proxy.MethodCall(1, "one");

        Assert.Equal<uint>(2, instance.MethodCallInvocationCount);
    }

    public static ICacheImplementation CacheFactory()
    {
        return
            CacheImplementationFactory
                .FromMemoryCache
                (
                    new MemoryCache
                    (
                        Options.Create(new MemoryCacheOptions())
                    )
                );
    }
}