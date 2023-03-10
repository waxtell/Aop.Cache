using Aop.Cache.ExpirationManagement;
using Aop.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using Aop.Cache.TestConsole;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

Console.WriteLine("Two stage caching:");

var worker = BindingsForTwoStageCache()
                .BuildServiceProvider()
                .GetService<ILengthyOperation>()!;

DoWork(worker);

Console.WriteLine("Memory Cache:");

worker = BindingsForMemoryCache()
            .BuildServiceProvider()
            .GetService<ILengthyOperation>()!;

DoWork(worker);

Console.WriteLine("Distributed Cache:");

worker = BindingsForDistributedCache()
            .BuildServiceProvider()
            .GetService<ILengthyOperation>()!;

DoWork(worker);

void DoWork(ILengthyOperation lengthyOperation)
{
    for (var i = 0; i < 10; i++)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        Console.Write(lengthyOperation.Fibonacci(40));

        stopWatch.Stop();
        Console.Write("     ");
        Console.WriteLine(stopWatch.Elapsed);
    }
}

ServiceCollection BindingsForMemoryCache()
{
    var serviceCollection = new ServiceCollection();

    serviceCollection
        .AddSingleton<IMemoryCache>
        (
            _ => new MemoryCache(Options.Create(new MemoryCacheOptions()))
        );

    serviceCollection.AddTransient<ILengthyOperation, LengthyOperation>();

    serviceCollection
        .AddSingleton
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

    serviceCollection
        .Decorate<ILengthyOperation>
        (
            (inner, provider) => provider
                .GetRequiredService<IPerMethodAdapter<ILengthyOperation>>()
                .Adapt(inner)
        );

    return
        serviceCollection;
}

ServiceCollection BindingsForDistributedCache()
{
    var serviceCollection = new ServiceCollection();

    serviceCollection
        .AddSingleton<IDistributedCache>
        (
            _ => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))
        );

    serviceCollection.AddTransient<ILengthyOperation, LengthyOperation>();

    serviceCollection
        .AddSingleton
        (
            provider =>
            {
                return
                    new PerMethodAdapter<ILengthyOperation>
                        (
                            CacheImplementationFactory
                                .FromDistributedCache(provider.GetService<IDistributedCache>())
                        )
                        .Cache
                        (
                            x => x.Fibonacci(It.IsAny<int>()),
                            For.Seconds(5)
                        );
            }
        );

    serviceCollection
        .Decorate<ILengthyOperation>
        (
            (inner, provider) => provider
                .GetRequiredService<IPerMethodAdapter<ILengthyOperation>>()
                .Adapt(inner)
        );

    return 
        serviceCollection;
}

ServiceCollection BindingsForTwoStageCache()
{
    var serviceCollection = new ServiceCollection();

    serviceCollection
        .AddStackExchangeRedisCache
        (
            options => { options.Configuration = "localhost:6000"; }
        );

    serviceCollection
        .AddSingleton<IMemoryCache>
        (
            _ => new MemoryCache(Options.Create(new MemoryCacheOptions()))
        );

    serviceCollection.AddTransient<ILengthyOperation, LengthyOperation>();
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
            .GetRequiredService<PerMethodAdapter<ILengthyOperation>>()
            .Adapt(instance)
    );

    serviceCollection.Decorate<ILengthyOperation>
    (
        (inner, provider) => provider
            .GetRequiredService<IPerMethodAdapter<ILengthyOperation>>()
            .Adapt(inner)
    );
    return serviceCollection;
}