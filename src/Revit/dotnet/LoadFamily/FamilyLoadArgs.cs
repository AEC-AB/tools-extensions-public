
namespace LoadFamily;

public class FamilyLoadArgs
{
    public FamilyLoadArgs(bool overriteIfFound, bool overriteIfInUse, bool overwriteParameterValues)
    {
        OverriteIfFound = overriteIfFound;
        OverriteIfInUse = overriteIfInUse;
        OverwriteParameterValues = overwriteParameterValues;
    }

    public bool OverriteIfFound { get; }
    public bool OverriteIfInUse { get; }
    public bool OverwriteParameterValues { get; }

    public bool FamilyWasFound { get; set; }

    /// <summary>
    /// True when <see cref="FamilyLoadOptions.OnFamilyFound"/> returned true,
    /// meaning Revit actually performed the overwrite inside the transaction.
    /// </summary>
    public bool OverwriteWasApplied { get; set; }
}