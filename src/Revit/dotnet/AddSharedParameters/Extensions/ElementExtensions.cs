namespace AddSharedParameters.Extensions;

public static class ElementExtensions
{
    public static long GetIdIntegerValue(this Element element)
    {
#if R2024_OR_GREATER
        return element.Id.Value;
#else
        return element.Id.IntegerValue;
#endif
    }
}