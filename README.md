# Aop.Cache
Simple,AOP cache adapter.


[![Build](https://github.com/waxtell/Aop.Cache/actions/workflows/build.yml/badge.svg)](https://github.com/waxtell/Aop.Cache/actions/workflows/build.yml)

**Explicit Parameter Matching**

```csharp
[Fact]
public void MixedInvocationsYieldsMultipleActualInvocations()
{
    var instance = new ForTestingPurposes();

    var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                    .Cache(x => x.MethodCall(0, "zero"), For.Seconds(30))
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
public void MixedFuzzyInvocationsYieldsMultipleActualInvocations()
{
    var instance = new ForTestingPurposes();

    var proxy = new PerMethodAdapter<IForTestingPurposes>(CacheFactory())
                    .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Minutes(5))
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
**Result exclusion**
```csharp
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
```
**Per Instance (All methods cached)**
```csharp
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
```
