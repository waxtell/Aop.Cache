using Aop.Cache.ExpirationManagement;
using System;

namespace Aop.Cache;

public interface ICacheImplementation
{
    void Set(string cacheKey, object result, CacheEntryOptions options);
    bool TryGetValue(string cacheKey, Type valueType, out object value);
    void Remove(string cacheKey);
}