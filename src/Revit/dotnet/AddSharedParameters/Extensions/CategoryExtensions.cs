namespace AddSharedParameters.Extensions;

public static class CategoryExtensions
{
    public static bool IsSameAs(this Category category, Category other)
    {
#if R2024_OR_GREATER
        return category.Id.Value.Equals(other.Id.Value);
#else
        return category.Id.IntegerValue.Equals(other.Id.IntegerValue);
#endif
    }    
    
    public static bool IsSameAs(this Category category, BuiltInCategory other)
    {
#if R2024_OR_GREATER
         return category.Id.Value.Equals((long)other);
#else
         return category.Id.IntegerValue.Equals((int)other);
#endif
    }
}