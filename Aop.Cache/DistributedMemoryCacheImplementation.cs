using System;
using System.Text;
using Aop.Cache.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Aop.Cache;

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
        value = JsonConvert.DeserializeObject(decodedValue, valueType.GetBaseType());

        return true;
    }

    public void Remove(string cacheKey)
    {
        _cache
            .Remove(cacheKey);
    }
}