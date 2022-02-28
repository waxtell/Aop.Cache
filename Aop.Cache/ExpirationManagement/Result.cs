using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;

namespace Aop.Cache.ExpirationManagement
{
    public class Result
    {
        public MemoryCacheEntryOptions True<T>(Func<T, bool> delegateExpression)
        {
            Expression<Func<T, bool>> expr = i => !delegateExpression(i);

            var mceo = new MemoryCacheEntryOptions();
            mceo.ExpirationTokens.Add(new CacheStuff<T>(expr.Compile()));

            return mceo;
        }
    }
}