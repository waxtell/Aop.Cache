using System;

namespace Aop.Cache.ExpirationManagement;

public static class For
{
    public static Func<CacheEntryOptions> Ever()
    {
        return
            () => new CacheEntryOptions();
    }

    public static Func<CacheEntryOptions> Milliseconds(int numMilliseconds)
    {
        return
            FromTimeSpan(TimeSpan.FromMilliseconds(numMilliseconds));
    }

    public static Func<CacheEntryOptions> Seconds(int numSeconds)
    {
        return
            FromTimeSpan(TimeSpan.FromSeconds(numSeconds));
    }

    public static Func<CacheEntryOptions> Minutes(int numMinutes)
    {
        return
            FromTimeSpan(TimeSpan.FromMinutes(numMinutes));
    }

    public static Func<CacheEntryOptions> Hours(int numHours)
    {
        return
            FromTimeSpan(TimeSpan.FromHours(numHours));
    }

    private static Func<CacheEntryOptions> FromTimeSpan(TimeSpan timeSpan)
    {
        return
            () => new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeSpan
            };
    }
}