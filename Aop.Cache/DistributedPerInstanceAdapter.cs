using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Aop.Cache;

public class DistributedPerInstanceAdapter<T> : PerInstanceBaseAdapter<T, DistributedCacheEntryOptions> where T : class
{
    public DistributedPerInstanceAdapter(IDistributedCache distributedCache, Func<ICacheImplementation<DistributedCacheEntryOptions>, string, DistributedCacheEntryOptions> optionsFactory)
        : base(new DistributedMemoryCacheImplementation(distributedCache), optionsFactory)
    {
    }
}