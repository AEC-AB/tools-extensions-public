namespace OpenWorksets;

/// <summary>
/// The shape of <see cref="OpenWorksetsArgs"/> before version 2. Kept so that
/// <see cref="OpenWorksetsArgsUpgradeV1ToV2"/> can migrate saved configurations.
/// </summary>
public class OpenWorksetsArgsV1
{
    public List<int>? WorksetIds { get; set; }

    public List<string>? RegexWorksets { get; set; }
}

/// <summary>
/// Migrates saved configurations from version 1 to version 2. Version 2 stores the
/// selected worksets as strings because <c>OptionsField</c> multi-select does not
/// support <c>List&lt;int&gt;</c>; the values are still workset ids, so no selection is lost.
/// </summary>
public class OpenWorksetsArgsUpgradeV1ToV2 : IArgsUpgrade<OpenWorksetsArgsV1, OpenWorksetsArgs>
{
    public OpenWorksetsArgs Upgrade(OpenWorksetsArgsV1 from) => new()
    {
        WorksetIds = from.WorksetIds?
            .Select(worksetId => worksetId.ToString(CultureInfo.InvariantCulture))
            .ToList() ?? [],
        RegexWorksets = from.RegexWorksets ?? [],
    };
}
