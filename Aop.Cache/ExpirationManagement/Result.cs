using System;
using System.Linq.Expressions;

namespace Aop.Cache.ExpirationManagement
{
    public class Result
    {
        public IExpirationDelegate True<T>(Func<T, DateTime, bool> delegateExpression)
        {
            Expression<Func<T, DateTime, bool>> expr = (i,d) => !delegateExpression(i,d);
            return new ExpirationDelegate<T>(expr.Compile());
        }
    }
}