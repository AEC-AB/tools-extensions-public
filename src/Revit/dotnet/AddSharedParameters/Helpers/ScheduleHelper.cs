using System.Diagnostics.CodeAnalysis;

namespace AddSharedParameters.Helpers;

public class ScheduleHelper
{
    internal void CreateSchedule(string scheduleName, IEnumerable<InternalDefinition> internalDefinitions, Document document, List<string> categories)
    {
        var schedule = GetSchedule(document, scheduleName) ?? CreateSchedule(document, scheduleName, categories);
        var definition = schedule.Definition;

        var availableFields = definition.GetSchedulableFields();
        var fields = GetFields(definition.GetFieldOrder(), definition);

        foreach (var internalDefinition in internalDefinitions)
        {
            var parameterId = internalDefinition.Id;

            if (ScheduleContainsParameter(fields, parameterId))
                continue;

            if (availableFields.FirstOrDefault(x => x.ParameterId.Equals(parameterId)) is not { } field)
                continue;

            definition.AddField(field);
        }
    }

    private ViewSchedule CreateSchedule(Document document, string scheduleName, IReadOnlyCollection<string> categories)
    {
        var schedule = ViewSchedule.CreateSchedule(document, GetScheduleType(categories));

        schedule.Name = scheduleName;
        return schedule;
    }

    private ElementId GetScheduleType(IReadOnlyCollection<string> categories)
    {
        var builtInCategories = categories
            .Select(TryParseBuiltInCategory)
            .Where(category => category.HasValue)
            .Select(category => category!.Value)
            .Distinct()
            .ToList();

        if (builtInCategories.Count.Equals(1))
            return new ElementId(builtInCategories.First());

        return ElementId.InvalidElementId;
    }

    private BuiltInCategory? TryParseBuiltInCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        if (Enum.TryParse<BuiltInCategory>(categoryName, out var builtInCategory))
            return builtInCategory;

        return null;
    }

    private bool ScheduleContainsParameter(IEnumerable<SchedulableField> fields, ElementId parameterId)
    {
        return fields.Any(field => field.ParameterId.Equals(parameterId));
    }

    private List<SchedulableField> GetFields(IEnumerable<ScheduleFieldId> scheduleFieldIds, ScheduleDefinition definition)
    {
        var result = new List<SchedulableField>();

        foreach (var scheduleFieldId in scheduleFieldIds)
        {
            result.Add(definition.GetField(scheduleFieldId).GetSchedulableField());
        }

        return result;
    }

    private ViewSchedule GetSchedule(Document document, string scheduleName)
    {
        return (ViewSchedule)new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)).WherePasses(ScheduleNameFilter(scheduleName)).FirstElement();
    }

    private ElementFilter ScheduleNameFilter(string scheduleName)
    {
        var parameterId = new ElementId(BuiltInParameter.VIEW_NAME);
#if R2023_OR_GREATER
        var rule = ParameterFilterRuleFactory.CreateEqualsRule(parameterId, scheduleName);
#else
        var rule = ParameterFilterRuleFactory.CreateEqualsRule(parameterId, scheduleName, false);
#endif

        return new ElementParameterFilter(rule);
    }

    public ScheduleBackups? ScheduleBackup(Document document, InternalDefinition internalDefinition)
    {
        var backups = new ScheduleBackups();

        using var collector = new FilteredElementCollector(document).OfClass(typeof(ViewSchedule));
        using var iterator = collector.GetElementIterator();

        while (iterator.MoveNext())
        {
            if (iterator.Current is not ViewSchedule schedule)
                continue;

            using var definition = schedule.Definition;

            if (!definition.TryGetField(internalDefinition.Id, out var fieldId))
                continue;

            var index = definition.GetFieldOrder().IndexOf(fieldId);

            var backup = new ScheduleBackup(schedule.Id, index);
            backups.Add(backup);
        }

        if (!backups.Any())
            return null;

        return backups;
    }

    public void RestoreSchedule(Document document, UpdateSharedParameterContext context)
    {
        if (context.SheduleBackup is null)
            return;

        foreach (var backup in context.SheduleBackup.Get())
        {
            if (document.GetElement(backup.Id) is not ViewSchedule schedule)
                throw new InvalidOperationException($"Schedule with id {backup.Id} was not found");

            using var definition = schedule.Definition;
            var scheduleFieldIds = definition.GetSchedulableFields();

            var field = scheduleFieldIds.FirstOrDefault(x => x.ParameterId.Equals(context.InternalDefinition.Id));

            if (field is null)
            { 
                context.AddMessage($"Field {context.InternalDefinition.Name} was not applicable to schedule {schedule.Name}");
                continue;
            }

            definition.InsertField(field, backup.Index);
        }

        context.SheduleBackup = null;
        context.RestoredSchedules = true;
    }
}

public static class ScheduleExtensions
{
    public static bool TryGetField(this ScheduleDefinition definition, ElementId parameterId, [NotNullWhen(true)] out ScheduleFieldId? foundField)
    {
        var fields = definition.GetFieldOrder();

        var scheduleFields = fields.Select(x => definition.GetField(x));

        var field = scheduleFields.FirstOrDefault(x => x.ParameterId.Equals(parameterId));

        if (field is not null)
        {
            foundField = field.FieldId;
            return true;
        }

        foundField = default;
        return false;
    }
}
