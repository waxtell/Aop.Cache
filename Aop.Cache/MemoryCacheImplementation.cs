using System;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache;

internal sealed class MemoryCacheImplementation : ICacheImplementation<MemoryCacheEntryOptions>
{
    private readonly IMemoryCache _cache;

    public MemoryCacheImplementation(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Set(string cacheKey, object result, MemoryCacheEntryOptions options)
    {
        try
        {
            _cache
                .Set(cacheKey, result, options);
        }
        catch
        {
            // ignore
        }
    }

    public bool TryGetValue(string cacheKey, Type _, out object value)
    {
        return
            _cache
                .TryGetValue(cacheKey, out value);
    }

    public void Remove(string cacheKey)
    {
        _cache
            .Remove(cacheKey);
    }
}