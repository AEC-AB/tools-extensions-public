namespace AddSharedParameters.Models;

public class ParameterValues
{
    public string ParameterUniqueId { get; set; }
    public List<ElementParameterValue> Items { get; set; }
    public string ParameterName { get; set; }
}