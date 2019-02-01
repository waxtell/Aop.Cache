using System;

namespace Aop.Cache.ExpirationManagement
{
    public interface IExpirationDelegate
    {
        bool HasExpired(object instance, DateTime executionDateTime);
    }
}