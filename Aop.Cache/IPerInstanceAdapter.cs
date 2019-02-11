namespace Aop.Cache
{
    public interface IPerInstanceAdapter<out T> where T : class
    {
        T Object { get; }
    }
}