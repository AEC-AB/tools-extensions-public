
namespace AddSharedParameters.Models;

public class ScheduleBackups
{
    private readonly List<ScheduleBackup> _backups = [];

    public void Add(ScheduleBackup backup)
    {
        _backups.Add(backup);
    }

    public bool Any()
    {
        return _backups.Any();
    }

    public IList<ScheduleBackup> Get()
    {
        return _backups;
    }
}