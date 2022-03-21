using System;
using Aop.Cache.ExpirationManagement;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache;

internal sealed class MemoryCacheImplementation : ICacheImplementation
{
    private readonly IMemoryCache _cache;

    public MemoryCacheImplementation(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Set(string cacheKey, object result, CacheEntryOptions options)
    {
        try
        {
            _cache
                .Set(cacheKey, result, (MemoryCacheEntryOptions) options);
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