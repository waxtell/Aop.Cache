using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache;

public class PerMethodAdapter<T> : PerMethodBaseAdapter<T, MemoryCacheEntryOptions> where T : class
{
    public PerMethodAdapter(IMemoryCache memoryCache) 
        : base(new MemoryCacheImplementation(memoryCache))
    {
    }
}