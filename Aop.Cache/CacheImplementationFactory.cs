using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache
{
    public static  class CacheImplementationFactory
    {
        public static ICacheImplementation FromMemoryCache(IMemoryCache cache)
        {
            return new MemoryCacheImplementation(cache);
}

        public static ICacheImplementation FromDistributedCache(IDistributedCache cache)
        {
            return new DistributedMemoryCacheImplementation(cache);
        }
    }
}
