using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Cache
{
    public interface IPerMethodAdapter<T> : ICacheAdapter<T> where T : class
    {
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target,Func<TReturn, DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<TReturn, DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<DateTime, bool> expirationDelegate);
    }
}