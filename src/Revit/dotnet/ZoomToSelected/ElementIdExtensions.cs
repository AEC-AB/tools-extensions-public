
namespace ZoomToSelected;


using System.Globalization;



public static class ElementIdExtensions
{
    public static long GetValue(this ElementId elementId)
    {
#if R2024_OR_GREATER
        return elementId.Value;
#else
        return elementId.IntegerValue;
#endif
    }

}