using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Aop.Cache.ExpirationManagement;

public static class Expires
{
    public static Func<ICacheImplementation<DistributedCacheEntryOptions>, string, DistributedCacheEntryOptions> Never()
    {
        return
            (_, _) => new DistributedCacheEntryOptions();
    }

    public static Func<ICacheImplementation<DistributedCacheEntryOptions>, string, DistributedCacheEntryOptions> At(
        DateTimeOffset absoluteExpiration)
    {
        return
            (_, _) => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };
    }
    public static Func<ICacheImplementation<DistributedCacheEntryOptions>, string, DistributedCacheEntryOptions> After(
        TimeSpan absoluteExpiration)
    {
        return
            (_, _) => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration
            };
    }

    public static Func<ICacheImplementation<DistributedCacheEntryOptions>, string, DistributedCacheEntryOptions> AfterInactive(
        TimeSpan slidingExpiration)
    {
        return
            (_, _) => new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };
    }
}