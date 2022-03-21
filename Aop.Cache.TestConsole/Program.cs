Console.WriteLine("Hi");
//using Aop.Cache.ExpirationManagement;
//using Aop.Cache;
//using Microsoft.Extensions.DependencyInjection;
//using System.Diagnostics;
//using Aop.Cache.TestConsole;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Options;

//var serviceCollection = new ServiceCollection();

//serviceCollection
//    .AddStackExchangeRedisCache
//    (
//        options =>
//        {
//            options.Configuration = "localhost:6000";
//        }
//    );

//serviceCollection
//    .AddSingleton<IMemoryCache>
//    (
//        _ => new MemoryCache(Options.Create(new MemoryCacheOptions()))
//    );

//serviceCollection
//    .AddSingleton<ICacheImplementation>
//    (
//        provider =>
//        {
//            return 
//                CacheImplementationFactory
//                    .FromMemoryCache(provider.GetRequiredService<IMemoryCache>());
//        }
//    );

////serviceCollection
////    .AddSingleton<IDistributedCache>
////    (
////        _ => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))
////    );

//serviceCollection.AddSingleton<ILengthyOperation, LengthyOperation>();
//serviceCollection.AddSingleton
//(
//    provider =>
//    {
//        return
//            new PerMethodAdapter<ILengthyOperation>(provider.GetService<IDistributedCache>())
//                .Cache
//                (
//                    x => x.Fibonacci(It.IsAny<int>()),
//                    For.Hours(1)
//                );
//    }
//);
//serviceCollection.AddSingleton
//(
//    provider =>
//    {
//        return
//            (PerMethodAdapter<ILengthyOperation>)
//            new PerMethodAdapter<ILengthyOperation>(provider.GetService<IMemoryCache>())
//                .Cache
//                (
//                    x => x.Fibonacci(It.IsAny<int>()),
//                    For.Minutes(45)
//                );
//    }
//);

//serviceCollection.Decorate<ILengthyOperation>
//(
//    (instance, provider) => provider
//                                .GetRequiredService<DistributedPerMethodAdapter<ILengthyOperation>>()!
//                                .Adapt(instance)
//);

//serviceCollection.Decorate<ILengthyOperation>
//(
//    (inner, provider) => provider
//                            .GetRequiredService<PerMethodAdapter<ILengthyOperation>>()!
//                            .Adapt(inner)
//);

//var stuff = serviceCollection
//                .BuildServiceProvider()
//                .GetService<ILengthyOperation>()!;

//for (var i = 0; i < 10; i++)
//{
//    var stopWatch = new Stopwatch();
//    stopWatch.Start();

//    Console.Write(stuff.Fibonacci(40));

//    stopWatch.Stop();
//    Console.Write(@"     ");
//    Console.WriteLine(stopWatch.Elapsed);
//}