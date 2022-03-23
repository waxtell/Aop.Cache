namespace Aop.Cache;

public interface ICacheAdapter<T> where T : class
{
    T Adapt(T instance);
}