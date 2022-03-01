namespace Aop.Cache;

public interface IPerInstanceAdapter<T> : ICacheAdapter<T> where T : class
{
}