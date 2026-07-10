namespace AddSharedParameters.Providers;

public class ValueBackupProvider
{
    public bool HasValues => Items.Any();
    public List<ParameterValues> Items { get; set; } = [];
    public string DocumentName { get; set; }
}