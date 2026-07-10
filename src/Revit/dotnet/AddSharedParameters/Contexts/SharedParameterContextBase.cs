using AddSharedParameters.Helpers;
using System.Text;

namespace AddSharedParameters.Contexts;

public abstract class SharedParameterContextBase : IDisposable
{
    protected readonly List<string> _messages = [];
    private bool _disposed;

    protected SharedParameterContextBase(ExternalDefinition externalDefinition, CategoriesToBindResult categories)
    {
        ExternalDefinition = externalDefinition;
        CategoriesToBind = categories;
    }

    public ExternalDefinition ExternalDefinition { get; }

    protected InternalDefinition? _internalDefinition;

    public InternalDefinition InternalDefinition
    {
        get => _internalDefinition ??
            throw new NullReferenceException("InternalDefinition was newer set");
    }

    public bool MergedParameters { get; set; }
    public CategoriesToBindResult CategoriesToBind { get; }

    private List<ParameterElement>? _duplicatedParameters;
    private List<ParameterRef> _duplicatedParameterRefs = [];

    public void AddDuplicatedParameter(ParameterElement parameter)
    {
        _duplicatedParameters ??= [];
        _duplicatedParameters.Add(parameter);
        var parameterRef = new ParameterRef(parameter);
        _duplicatedParameterRefs.Add(parameterRef);
    }

    public void AddDuplicatedParameterRange(List<ParameterElement> parameters)
    {
        foreach (var parameter in parameters)
            AddDuplicatedParameter(parameter);
    }

    internal List<ParameterElement> GetDuplicatedParameters()
    {
        if (_duplicatedParameters is null)
            throw new InvalidOperationException("DuplicatedParameters was not set on context");

        return _duplicatedParameters;
    }


    public bool VariesAcrossGroupsUpdated { get; set; }
    public bool DeletedDuplicatedParameters { get; set; }
    public RestoreValuesResult? MergeParameterResult { get; set; }
    

    internal void AddMessage(string message)
    {
        _messages.Add(message);
    }

    protected string GetDetails()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"** Parameter: {ExternalDefinition.Name} **");

        if (_messages.Any())
            sb.AppendLine(" - Has warnings, see warnings above");

        AddSpesificDetails(sb);

        if (DeletedDuplicatedParameters)
        {
            sb.AppendLine();
            sb.AppendLine($" - Following parameters was merged and deleted");
            foreach (var duplicatedParamRef in _duplicatedParameterRefs)
            {
                if (duplicatedParamRef.IsShared())
                    sb.AppendLine($"   * {duplicatedParamRef.Name} (shared) Guid: {duplicatedParamRef.GuidValue}");
                else
                    sb.AppendLine($"   * {duplicatedParamRef.Name} (project)");
            }

            if (MergeParameterResult is not null)
            {
                sb.Append(" - Merge result: ");
                MergeParameterResult.AppendDetails(sb);
            }
        }

        if (VariesAcrossGroupsUpdated)
            sb.AppendLine($" - Varies across groups was updated");

        return sb.ToString();
    }

    public string? GetWarning()
    {
        return _messages.Any() ? string.Join(Environment.NewLine, _messages) : null;
    }

    public abstract void AddSpesificDetails(StringBuilder sb);

    public void Dispose()
    {
        if (_disposed)
            return;

        CategoriesToBind?.Dispose();

        // Dispose duplicated parameters if they haven't been deleted
        if (_duplicatedParameters is not null && !DeletedDuplicatedParameters)
        {
            foreach (var param in _duplicatedParameters)
            {
                param?.Dispose();
            }
        }

        _disposed = true;
    }
}
