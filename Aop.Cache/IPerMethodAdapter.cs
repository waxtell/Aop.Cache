using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Cache
{
    public interface IPerMethodAdapter<T> where T : class
    {
        T Object { get; }

        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target,Func<TReturn, DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, Task<TReturn>>> target, Func<DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<TReturn, DateTime, bool> expirationDelegate);
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, Func<DateTime, bool> expirationDelegate);
    }
}