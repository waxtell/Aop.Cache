using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Cache;

public interface IPerMethodAdapter<T, in TEntryOptions> : ICacheAdapter<T> where T : class
{
    IPerMethodAdapter<T, TEntryOptions> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory);
    IPerMethodAdapter<T, TEntryOptions> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<ICacheImplementation<TEntryOptions>, string, TEntryOptions> optionsFactory);
}