namespace AddSharedParameters.Models;

public class ScheduleBackup
{
    public ScheduleBackup(ElementId id, int index)
    {
        Id = id;
        Index = index;
    }

    public ElementId Id { get; }
    public int Index { get; }
}