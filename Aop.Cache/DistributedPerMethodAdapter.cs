using Microsoft.Extensions.Caching.Distributed;

namespace Aop.Cache;

public class DistributedPerMethodAdapter<T> : PerMethodBaseAdapter<T, DistributedCacheEntryOptions> where T : class
{
    public DistributedPerMethodAdapter(IDistributedCache distributedCache) 
        : base(new DistributedMemoryCacheImplementation(distributedCache))
    {
    }
}