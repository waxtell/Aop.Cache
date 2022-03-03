using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;

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
        _cache
            .Set(cacheKey, result, options);
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

internal sealed class DistributedMemoryCacheImplementation : ICacheImplementation<DistributedCacheEntryOptions>
{
    private readonly IDistributedCache _cache;

    public DistributedMemoryCacheImplementation(IDistributedCache cache)
    {
        _cache = cache;
    }

    public void Set(string cacheKey, object result, DistributedCacheEntryOptions options)
    {
        var serializedValue = JsonConvert.SerializeObject(result);
        var encodedValue = Encoding.UTF8.GetBytes(serializedValue);

        _cache
            .Set(cacheKey, encodedValue, options);
    }

    public bool TryGetValue(string cacheKey, Type valueType, out object value)
    {
        value = null;

        var result = _cache.Get(cacheKey);

        if (result == null)
        {
            return false;
        }

        var decodedValue = Encoding.UTF8.GetString(result);
        value = JsonConvert.DeserializeObject(decodedValue, valueType);

        return true;
    }

    public void Remove(string cacheKey)
    {
        _cache
            .Remove(cacheKey);
    }
}