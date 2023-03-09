using Aop.Cache.ExpirationManagement;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Cache;

public interface IPerMethodAdapter<T> : ICacheAdapter<T> where T : class
{
    IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<CacheEntryOptions> optionsFactory, params Func<Task<TReturn>, bool>[] exclusions);
    IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<CacheEntryOptions> optionsFactory, params Func<TReturn, bool>[] exclusions);
}