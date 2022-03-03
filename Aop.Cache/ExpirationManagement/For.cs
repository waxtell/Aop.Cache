using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement;

public static class For
{
    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Milliseconds(int numMilliseconds)
    {
        return
            (_, _) => new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(numMilliseconds)
            };
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Seconds(int numSeconds)
    {
        return
            (_, _) => new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(numSeconds)
            };
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Minutes(int numMinutes)
    {
        return
            (_, _) => new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(numMinutes)
            };
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Hours(int numHours)
    {
        return
            (_, _) => new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(numHours)
            };
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Ever()
    {
        return
            (_, _) => new MemoryCacheEntryOptions();
    }
}