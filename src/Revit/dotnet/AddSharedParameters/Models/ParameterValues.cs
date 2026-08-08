namespace AddSharedParameters.Models;

public class ParameterValues
{
    public required string ParameterUniqueId { get; set; }
    public required List<ElementParameterValue> Items { get; set; }
    public required string ParameterName { get; set; }
}