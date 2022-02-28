using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement
{
    public static class For
    {
        public static Func<IMemoryCache, string, MemoryCacheEntryOptions> Milliseconds(int numMilliseconds)
        {
            return
                (cache, key) => new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(numMilliseconds)
                };
        }

        public static Func<IMemoryCache, string, MemoryCacheEntryOptions> Seconds(int numSeconds)
        {
            return
                (cache, key) => new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(numSeconds)
                };
        }

        public static Func<IMemoryCache, string, MemoryCacheEntryOptions> Minutes(int numMinutes)
        {
            return
                (cache, key) => new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(numMinutes)
                };
        }

        public static Func<IMemoryCache, string, MemoryCacheEntryOptions> Hours(int numHours)
        {
            return
                (cache, key) => new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(numHours)
                };
        }

        public static Func<IMemoryCache, string, MemoryCacheEntryOptions> Ever()
        {
            return
                (cache, key) => new MemoryCacheEntryOptions();
        }
    }
}