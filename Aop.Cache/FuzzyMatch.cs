namespace Aop.Cache;

public static class It
{
    /// <summary>
    /// Matches any value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T IsAny<T>()
    {
        return default;
    }

    /// <summary>
    /// Matches any non-null value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T IsNotNull<T>()
    {
        return default;
    }

    /// <summary>
    /// Excludes the parameter from match evaluation AND key generation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T IsIgnored<T>()
    {
        return default;
    }
}