# Aop.Cache
Simple,AOP cache adapter.


[![Build status](https://ci.appveyor.com/api/projects/status/r9ax7l4277b6692y?svg=true)](https://ci.appveyor.com/project/waxtell/aop-cache-e6xqw) [![NuGet Badge](https://buildstats.info/nuget/Aop.Cache)](https://www.nuget.org/packages/Aop.Cache/)
[![Coverage Status](https://coveralls.io/repos/github/waxtell/Aop.Cache/badge.svg?branch=master)](https://coveralls.io/github/waxtell/Aop.Cache?branch=master)

**Explicit Parameter Matching**

```csharp
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
```
**Fuzzy Parameter Matching**
```csharp
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
```
**Per Instance (All methods cached)**
```csharp
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
```

[TODO]
1) Replace reflection with expressions in per instance async code
