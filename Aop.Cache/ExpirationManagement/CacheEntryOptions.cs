using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement
{
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
            var result = new MemoryCacheEntryOptions();

            if (cacheEntryOptions.AbsoluteExpiration.HasValue)
            {
                result.AbsoluteExpiration = cacheEntryOptions.AbsoluteExpiration.Value;
            }

            if (cacheEntryOptions.AbsoluteExpirationRelativeToNow.HasValue)
            {
                result.AbsoluteExpirationRelativeToNow = cacheEntryOptions.AbsoluteExpirationRelativeToNow.Value;
            }

            if (cacheEntryOptions.SlidingExpiration.HasValue)
            {
                result.SlidingExpiration = cacheEntryOptions.SlidingExpiration.Value;
            }

            return result;
        }

        public static explicit operator DistributedCacheEntryOptions(CacheEntryOptions cacheEntryOptions)
        {
            var result = new DistributedCacheEntryOptions();

            if (cacheEntryOptions.AbsoluteExpiration.HasValue)
            {
                result.AbsoluteExpiration = cacheEntryOptions.AbsoluteExpiration.Value;
            }

            if (cacheEntryOptions.AbsoluteExpirationRelativeToNow.HasValue)
            {
                result.AbsoluteExpirationRelativeToNow = cacheEntryOptions.AbsoluteExpirationRelativeToNow.Value;
            }

            if (cacheEntryOptions.SlidingExpiration.HasValue)
            {
                result.SlidingExpiration = cacheEntryOptions.SlidingExpiration.Value;
            }

            return result;
        }
    }
}
