using System;

namespace Aop.Cache.ExpirationManagement;

public static class Expires
{
    public static Func<CacheEntryOptions> WhenInactiveFor(TimeSpan slidingExpiration, DateTimeOffset? absoluteExpiration = null)
    {
        return
            () => new CacheEntryOptions
            {
                SlidingExpiration = slidingExpiration,
                AbsoluteExpiration = absoluteExpiration
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