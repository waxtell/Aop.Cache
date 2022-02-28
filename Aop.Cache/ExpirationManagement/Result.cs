﻿using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Aop.Cache.ExpirationManagement
{
    public class Result
    {
        public Func<IMemoryCache, string, MemoryCacheEntryOptions> NotChanged(IChangeToken changeToken)
        {
            return 
                (cache, key) =>
                {
                    var options = new MemoryCacheEntryOptions();
                    changeToken.RegisterChangeCallback(_ => cache.Remove(key), null);
                    options.ExpirationTokens.Add(changeToken);

                    return options;
                };
        }
    }
}