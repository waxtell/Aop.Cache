using System;

namespace Aop.Cache;

public interface ICacheImplementation<in TEntryOptions>
{
    void Set(string cacheKey, object result, TEntryOptions options);
    bool TryGetValue(string cacheKey, Type valueType, out object value);
    void Remove(string cacheKey);
}