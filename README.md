# Aop.Cache
Simple,AOP cache adapter.


[![Build status](https://ci.appveyor.com/api/projects/status/hxyxeqsgos31dhh7?svg=true)](https://ci.appveyor.com/project/waxtell/aop-cache) [![NuGet Badge](https://buildstats.info/nuget/Aop.Cache)](https://www.nuget.org/packages/Aop.Cache/)

**Explicit Parameter Matching**

```csharp
        [Fact]
        public void MixedCachedInvocationsYieldsMultipleInvocations()
        {
            var instance = new ForTestingPurposes();

            var proxy = new PerMethodAdapter<IForTestingPurposes>(instance)
                            .Cache(x => x.MethodCall(0, "zero"), For.Ever())
                            .Object;

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

            var proxy = new PerMethodAdapter<IForTestingPurposes>(instance)
                            .Cache(x => x.MethodCall(It.IsAny<int>(), "zero"), For.Ever())
                            .Object;

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

            var proxy = new PerInstanceAdapter<IForTestingPurposes>(instance, For.Ever())
                            .Object;

            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");
            proxy.MethodCall(0, "zero");

            Assert.Equal<uint>(1, instance.MethodCallInvocationCount);
        }
```

[TODO]
1) Replace reflection with expressions in per instance async code
