using System;

namespace Aop.Cache.ExpirationManagement
{
    public static class For
    {
        public static Func<DateTime, bool> Milliseconds(int numMilliseconds)
        {
            return (dt) => DateTime.UtcNow > dt.AddMilliseconds(numMilliseconds);
        }
        public static Func<DateTime, bool> Seconds(int numSeconds)
        {
            return (dt) => DateTime.UtcNow > dt.AddSeconds(numSeconds);
        }

        public static Func<DateTime, bool> Minutes(int numMinutes)
        {
            return (dt) => DateTime.UtcNow > dt.AddMinutes(numMinutes);
        }
        public static Func<DateTime, bool> Hours(int numHours)
        {
            return (dt) => DateTime.UtcNow > dt.AddHours(numHours);
        }

        public static Func<DateTime, bool> Ever()
        {
            return (dt) => false;
        }
    }
}