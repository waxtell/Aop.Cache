using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement;

public static class For
{
    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Ever()
    {
        return
            (_, _) => new MemoryCacheEntryOptions();
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Milliseconds(int numMilliseconds)
    {
        return
            FromTimeSpan(TimeSpan.FromMilliseconds(numMilliseconds));
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Seconds(int numSeconds)
    {
        return
            FromTimeSpan(TimeSpan.FromSeconds(numSeconds));
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Minutes(int numMinutes)
    {
        return
            FromTimeSpan(TimeSpan.FromMinutes(numMinutes));
    }

    public static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> Hours(int numHours)
    {
        return
            FromTimeSpan(TimeSpan.FromHours(numHours));
    }

    private static Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> FromTimeSpan(TimeSpan timeSpan)
    {
        return
            (_, _) => new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeSpan
            };
    }
}