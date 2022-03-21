using Aop.Cache.ExpirationManagement;
using Aop.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using Aop.Cache.TestConsole;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var serviceCollection = new ServiceCollection();

serviceCollection
    .AddStackExchangeRedisCache
    (
        options =>
        {
            options.Configuration = "localhost:6000";
        }
    );

serviceCollection
    .AddSingleton<IMemoryCache>
    (
        _ => new MemoryCache(Options.Create(new MemoryCacheOptions()))
    );

serviceCollection.AddSingleton<ILengthyOperation, LengthyOperation>();
serviceCollection.AddSingleton
(
    provider =>
    {
        return
            (PerMethodAdapter<ILengthyOperation>)
                new PerMethodAdapter<ILengthyOperation>
                (
                    CacheImplementationFactory
                        .FromDistributedCache(provider.GetService<IDistributedCache>())
                )
                .Cache
                (
                    x => x.Fibonacci(It.IsAny<int>()),
                    Expires.WhenInactiveFor(TimeSpan.FromMinutes(5))
                );
    }
);

serviceCollection.AddSingleton
(
    provider =>
    {
        return
            new PerMethodAdapter<ILengthyOperation>
                (
                    CacheImplementationFactory
                        .FromMemoryCache(provider.GetService<IMemoryCache>())
                )
                .Cache
                (
                    x => x.Fibonacci(It.IsAny<int>()),
                    For.Seconds(5)
                );
    }
);

serviceCollection.Decorate<ILengthyOperation>
(
    (instance, provider) => provider
                                .GetRequiredService<PerMethodAdapter<ILengthyOperation>>()!
                                .Adapt(instance)
);

serviceCollection.Decorate<ILengthyOperation>
(
    (inner, provider) => provider
                            .GetRequiredService<IPerMethodAdapter<ILengthyOperation>>()!
                            .Adapt(inner)
);

var stuff = serviceCollection
                .BuildServiceProvider()
                .GetService<ILengthyOperation>()!;

for (var i = 0; i < 10; i++)
{
    var stopWatch = new Stopwatch();
    stopWatch.Start();

    Console.Write(stuff.Fibonacci(40));

    stopWatch.Stop();
    Console.Write("     ");
    Console.WriteLine(stopWatch.Elapsed);
}