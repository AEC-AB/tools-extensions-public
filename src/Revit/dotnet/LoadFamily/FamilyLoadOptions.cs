namespace LoadFamily;

public class FamilyLoadOptions : IFamilyLoadOptions
{
    private FamilyLoadArgs _args;

    public FamilyLoadOptions(FamilyLoadArgs args)
    {
        _args = args;
    }

    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        _args.FamilyWasFound = true;

        overwriteParameterValues = false;

        if (!_args.OverriteIfFound)
            return false;


        if (familyInUse && !_args.OverriteIfInUse)
            return false;

        overwriteParameterValues = _args.OverwriteParameterValues;
        _args.OverwriteWasApplied = true;
        return true;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
        throw new System.NotImplementedException("Shared families is not supported");
    }
}