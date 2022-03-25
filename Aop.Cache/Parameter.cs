using Newtonsoft.Json;

namespace Aop.Cache;

internal class Parameter
{
    private readonly MatchPrecision _precision;
    private readonly string _serializedValue;

    private enum MatchPrecision
    {
        Exact,
        Any,
        NotNull,
        Ignore
    }

    private Parameter(string serializedValue, MatchPrecision precision)
    {
        _serializedValue = serializedValue;
        _precision = precision;
    }

    public static Parameter MatchExact(object value)
    {
        return new Parameter(JsonConvert.SerializeObject(value), MatchPrecision.Exact);
    }

    public static Parameter MatchAny()
    {
        return new Parameter(null, MatchPrecision.Any);
    }

    public static Parameter MatchNotNull()
    {
        return new Parameter(null, MatchPrecision.NotNull);
    }

    public static Parameter Ignore()
    {
        return new Parameter(null, MatchPrecision.Ignore);
    }

    public bool IsEvaluated()
    {
        return _precision != MatchPrecision.Ignore;
    }

    public bool IsMatch(object value)
    {
        return _precision switch
        {
            MatchPrecision.Ignore => true,
            MatchPrecision.Any => true,
            MatchPrecision.NotNull => value != null,
            _ => JsonConvert.SerializeObject(value) == _serializedValue
        };
    }
}