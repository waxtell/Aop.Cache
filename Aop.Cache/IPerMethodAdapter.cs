﻿using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Cache
{
    public interface IPerMethodAdapter<T> : ICacheAdapter<T> where T : class
    {
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<IMemoryCache, string, MemoryCacheEntryOptions> optionsFactory);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<IMemoryCache, string, MemoryCacheEntryOptions> optionsFactory);
    }
}