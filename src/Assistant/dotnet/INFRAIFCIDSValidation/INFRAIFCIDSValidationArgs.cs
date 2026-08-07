namespace INFRAIFCIDSValidation;

public class INFRAIFCIDSValidationArgs
{
    [OptionsField(Label = "Project name", CollectorType = typeof(AvailableProjectsCollector), CollectorSortOrder = SortOrder.SortByAscending)]
    public string AutoProjectName { get; set; } = string.Empty;

    [OptionsField(Label = "IDS files", CollectorType = typeof(AvailableIdsFilesCollector), CollectorSortOrder = SortOrder.SortByAscending)]
    public List<string> AutoSelectedIdsFiles { get; set; } = [];

    [FilePickerField(Label = "IFC files", FileExtensions = ["ifc"], ToolTip = "Select IFC files, or edit raw data to use wildcard paths, Assistant variables, embedded variable placeholders, or regex entries.")]
    public List<string> IfcFiles { get; set; } = [];

    [FolderPickerField(Label = "Output folder")]
    [Required(ErrorMessage = "Output folder is required.")]
    public string? OutputFolder { get; set; }

    [OptionsField(Label = "Validation commands", CollectorType = typeof(AvailableValidationCommandsCollector), CollectorSortOrder = SortOrder.None)]
    public List<string> Commands { get; set; } =
    [
        nameof(InfraCommand.IFC_CHECK),
    ];

    [BooleanField(Label = "Close INFRA on completion")]
    public bool CloseOnCompletion { get; set; }

    [BooleanField(Label = "Enable diagnostics")]
    public bool EnableDiagnostics { get; set; }
}
