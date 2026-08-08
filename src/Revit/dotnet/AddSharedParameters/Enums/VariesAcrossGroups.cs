namespace AddSharedParameters.Enums;

public enum VariesAcrossGroups
{
    [Description("Values are aligned per group type")]
    Aligned,

    [Description("Values can vary by group instance")]
    Vary
}