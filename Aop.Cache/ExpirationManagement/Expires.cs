using System;

namespace Aop.Cache.ExpirationManagement;

public static class Expires
{
    public static Func<CacheEntryOptions> WhenInactiveFor(TimeSpan slidingExpiration)
    {
        return
            () => new CacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };
    }

    public static Func<CacheEntryOptions> At(DateTimeOffset at)
    {
        return
            () => new CacheEntryOptions
            {
                AbsoluteExpiration = at
            };
    }
}