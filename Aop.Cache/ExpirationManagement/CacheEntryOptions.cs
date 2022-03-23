using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement;

public class CacheEntryOptions
{
    //
    // Summary:
    //     Gets or sets an absolute expiration date for the cache entry.
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    //
    // Summary:
    //     Gets or sets an absolute expiration time, relative to now.
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    //
    // Summary:
    //     Gets or sets how long a cache entry can be inactive (e.g. not accessed) before
    //     it will be removed. This will not extend the entry lifetime beyond the absolute
    //     expiration (if set).
    public TimeSpan? SlidingExpiration { get; set; }

    public static explicit operator MemoryCacheEntryOptions(CacheEntryOptions cacheEntryOptions)
    {
        return
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = cacheEntryOptions.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = cacheEntryOptions.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = cacheEntryOptions.SlidingExpiration
            };
    }

    public static explicit operator DistributedCacheEntryOptions(CacheEntryOptions cacheEntryOptions)
    {
        return
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = cacheEntryOptions.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = cacheEntryOptions.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = cacheEntryOptions.SlidingExpiration
            };
    }
}

