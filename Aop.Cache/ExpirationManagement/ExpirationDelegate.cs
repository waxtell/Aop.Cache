using System;

namespace Aop.Cache.ExpirationManagement
{
    public class ExpirationDelegate : IExpirationDelegate
    {
        private readonly Func<DateTime,bool> _delegate;

        internal ExpirationDelegate(Func<DateTime, bool> expirationDelegate)
        {
            _delegate = expirationDelegate;
        }

        public bool HasExpired(object instance, DateTime executionDateTime)
        {
            return _delegate.Invoke(executionDateTime);
        }
    }

    public class ExpirationDelegate<TReturn> : IExpirationDelegate
    {
        private readonly Func<TReturn, DateTime, bool> _delegate;

        internal ExpirationDelegate(Func<TReturn, DateTime, bool> expirationDelegate)
        {
            _delegate = expirationDelegate;
        }

        public bool HasExpired(object instance, DateTime executionDateTime)
        {
            return _delegate.Invoke((TReturn) instance, executionDateTime);
        }
    }
}
