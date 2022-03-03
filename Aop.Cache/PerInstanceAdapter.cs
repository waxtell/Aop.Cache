using System;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache;

public class PerInstanceAdapter<T> : PerInstanceBaseAdapter<T, MemoryCacheEntryOptions> where T : class
{
    public PerInstanceAdapter(IMemoryCache memoryCache, Func<ICacheImplementation<MemoryCacheEntryOptions>, string, MemoryCacheEntryOptions> optionsFactory) 
        : base(new MemoryCacheImplementation(memoryCache), optionsFactory)
    {
    }
}