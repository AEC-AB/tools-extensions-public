namespace PrintPDF.Enums;

public enum OrderingTechnique
{
    /// <summary>
    /// Preserve the order as supplied by the caller (no reordering).
    /// </summary>
    PreserveSourceOrder = 0,

    /// <summary>
    /// Apply deterministic alphanumeric ordering.
    /// </summary>
    Alphanumeric = 1
}
