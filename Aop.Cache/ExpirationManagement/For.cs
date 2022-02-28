using Microsoft.Extensions.Caching.Memory;
using System;

namespace Aop.Cache.ExpirationManagement
{
    public static class For
    {
        public static MemoryCacheEntryOptions Milliseconds(int numMilliseconds)
        {
            return 
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(numMilliseconds)
                };
        }

        public static MemoryCacheEntryOptions Seconds(int numSeconds)
        {
            return
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(numSeconds)
                };
        }

        public static MemoryCacheEntryOptions Minutes(int numMinutes)
        {
            return
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(numMinutes)
                };
        }

        public static MemoryCacheEntryOptions Hours(int numHours)
        {
            return
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(numHours)
                };
        }

        public static MemoryCacheEntryOptions Ever()
        {
            return
                new MemoryCacheEntryOptions();

        }
    }
}