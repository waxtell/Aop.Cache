using System;
using System.Linq.Expressions;

namespace Aop.Cache.ExpirationManagement
{
    public class Result
    {
        public Func<T, DateTime, bool> True<T>(Func<T, DateTime, bool> delegateExpression)
        {
            Expression<Func<T, DateTime, bool>> expr = (i,d) => !delegateExpression(i,d);
            return expr.Compile();
        }
    }
}