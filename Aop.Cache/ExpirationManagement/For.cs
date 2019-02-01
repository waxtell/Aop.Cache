using System;

namespace Aop.Cache.ExpirationManagement
{
    public static class For
    {
        public static IExpirationDelegate Milliseconds(int numMilliseconds)
        {
            return new ExpirationDelegate(x => DateTime.UtcNow > x.AddMilliseconds(numMilliseconds));
        }
        public static IExpirationDelegate Seconds(int numSeconds)
        {
            return new ExpirationDelegate(x => DateTime.UtcNow>x.AddSeconds(numSeconds));
        }
        public static IExpirationDelegate Minutes(int numMinutes)
        {
            return new ExpirationDelegate(x => DateTime.UtcNow > x.AddMinutes(numMinutes));
        }
        public static IExpirationDelegate Hours(int numHours)
        {
            return new ExpirationDelegate(x => DateTime.UtcNow > x.AddHours(numHours));
        }
    }
}