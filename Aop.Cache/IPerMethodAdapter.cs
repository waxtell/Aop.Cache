using System;
using System.Linq.Expressions;
using Aop.Cache.ExpirationManagement;

namespace Aop.Cache
{
    public interface IPerMethodAdapter<T> where T : class
    {
        T Object { get; }
        IPerMethodAdapter<T> Cache<TReturn>(Expression<Func<T, TReturn>> target, IExpirationDelegate expirationDelegate);
    }
}