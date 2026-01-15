using CW.Assistant.Extensions.Assistant.Collectors;
using CW.Assistant.Extensions.Contracts.Enums;
using CW.Assistant.Extensions.Contracts.Fields;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace AssistantDemoExtension;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class AssistantDemoExtensionArgs
{
    [UrlField(
        Label = "Demo Extension GitHub URL",
        ToolTip = "Link to this demo extension")]
    public string ExtensionGitUrl { get; } = "https://github.com/AEC-AB/tools-extensions-public/blob/main/Assistant/dotnet/AssistantDemoExtension/AssistantDemoExtensionArgs.cs";

    [TextField(
       Label = "Text input",
       Hint = "Enter some text",
       ToolTip = """
        Its possible to add a tooltip to provide additional information about the input.
        You can use triple quotes to create multi-line tooltips.
        Create informative and user-friendly tooltips to enhance the user experience.
        """,
       HelperText = "The text must be clear and describe something")]
    [Required(ErrorMessage = "This field is required.")]
    public string Input { get; set; } = "Default input";

    [TextField(
        Label = "Multiline Text input",
        ToolTip = "Multiline text input lets you enter several lines of text.",
        IsMultiline = true,
        MinLines = 3,
        MaxLines = 6)]
    public string TextInputMultiline { get; set; } = """
        This is a sample multiline text input.
        You can enter multiple lines of text here.
        """;

    [TextField(
        Label = "Read-Only Text input",
        ToolTip = "Read-Only text input displays information that cannot be modified by the user."
    )]
    public string ReadOnlyTextInput { get; } = "This is a read-only text input.";

    [TextField(
        Label = "Text input with AutoFill",
        ToolTip = "Text input with AutoFill provides suggestions as you type.",
        CollectorType = typeof(CustomAutoFillCollector))]
    public string? AutoFillTextInput { get; set; }

    [OptionsField(
        Label = "Options field",
        ToolTip = "Options field provides a dropdown list of options populated by a custom collector.",
        CollectorType = typeof(CustomAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending
    )]
    public string? OptionsField { get; set; }

    [FilePickerField(
        Label = "Browse for File input",
        ToolTip = "Open file dialog control",
        Hint = "Select a JSON file",
        FileExtensions = ["json", "*"])]
    public string? BrowseForFile { get; set; }

    [FilePickerField(
        Label = "Browse for Multiple Files input",
        ToolTip = "Open file dialog control to select multiple files",
        FileExtensions = ["json", "*"])]
    public List<string> BrowseForMultipleFiles { get; set; } = [];

    [FolderPickerField(
        Label = "Browse for Directory input",
        ToolTip = "Open file dialog control to select a directory")]
    public string? BrowseForDirectory { get; set; }

    [FolderPickerField(
        Label = "Browse for Multiple Directories input",
        ToolTip = "Open file dialog control to select multiple directories")]
    public List<string> BrowseForMultipleDirectories { get; set; } = [];

    [SaveFileField(
        Label = "Save File input",
        Hint = "Save as JSON file",
        ToolTip = "Save file dialog control",
        FileExtensions = ["json", "*"])]
    public string? SaveFile { get; set; }

    [UrlField(
        Label = "URL input",
        Hint = "https://www.example.com",
        ToolTip = "URL input allows you to enter web addresses.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? UrlInput { get; set; }

    [IntegerField(
        Label = "Integer input",
        ToolTip = "Integer input allows you to enter whole numbers only.")]
    public int IntegerInput { get; set; } = 5;

    [IntegerField(
        Label = "Integer Slider input",
        ToolTip = "Integer Slider input allows you to select a value within a specified range using a slider control.",
        MinimumValue = 0,
        MaximumValue = 30,
        StepValue = 5)]
    public int IntegerSliderInput { get; set; } = 15;

    [DoubleField(
        Label = "Number input",
        ToolTip = "Number input allows you to enter numeric values only.")]
    public double NumberInput { get; set; } = 10.5;

    [BooleanField(
        Label = "Boolean input",
        ToolTip = "Boolean input represents a true/false value.")]
    public bool BooleanInput { get; set; }

    [DateTimeField(
        Label = "Date and Time Picker",
        ToolTip = "Date and time picker control for selecting both date and time values.",
        ShowTime = true)]
    public DateTime DateAndTime { get; set; }

    [DateTimeField(
        Label = "Date Only Picker",
        ToolTip = "Date only picker control for selecting date values without time.",
        ShowTime = false)]
    public DateTime DateOnly { get; set; }

    [OptionsField(
        Label = "ComboBox with custom enums",
        ToolTip = "ComboBox control with custom enums",
        CollectorSortOrder = SortOrder.None)]
    public CustomEnum CustomEnumControl { get; set; } = CustomEnum.Option1;

    [OptionsField(
        Label = "ListBox with custom enums",
        ToolTip = "ListBox control with custom enums",
        CollectorSortOrder = SortOrder.SortByDescending,
        MaxHeight = 200)]
    public List<CustomEnum> ListBoxWithEnum { get; set; } = [];

    [OptionsField(
        Label = "Compact ListBox with custom enums",
        ToolTip = "Compact ListBox control with custom enums",
        CompactMode = true,
        CollectorSortOrder = SortOrder.SortByAscending)]
    public List<CustomEnum> ListBoxWithEnumCompact { get; set; } = [];

    [ChoiceField(
        Label = "RadioButton with custom enums",
        ToolTip = "RadioButton control with custom enums")]
    [AllowedValues(nameof(CustomEnum.Option2), nameof(CustomEnum.Option3),
        ErrorMessage = "Please select either Option2 or Option3.")]
    public CustomEnum RadioButtonWithEnum { get; set; } = CustomEnum.Option2;

    [ChoiceField(
        Label = "Vertical RadioButton with custom enums",
        ToolTip = "Vertical RadioButton control with custom enums",
        Orientation = ChoiceOrientation.Vertical)]
    public CustomEnum RadioButtonVerticalWithEnum { get; set; } = CustomEnum.Option3;

    [ListField(
        Label = "String List input",
        ToolTip = "String List input allows you to enter multiple string values.")]
    public List<string> StringListInput { get; set; } = ["Item1", "Item2", "Item3"];

    [OptionsField(
        Label = "String List input with options",
        ToolTip = "String List input with predefined options for values.",
        CollectorType = typeof(CustomAutoFillCollector))]
    public List<string> StringListOptionsInput { get; set; } = [];

    [DictionaryField(
        Label = "Dictionary input",
        ToolTip = "Dictionary input allows you to enter key-value pairs.")]
    public Dictionary<string, string> DictionaryInput { get; set; } = [];

    [DictionaryField(
        Label = "Dictionary input with options",
        ToolTip = "Dictionary input with predefined options for values.",
        CollectorType = typeof(CustomAutoFillCollector))]
    public Dictionary<string, string> DictionaryWithOptionsInput { get; set; } = [];

    [PasswordField(
        Label = "Credentials for Application Id",
        ToolTip = "Select credentials stored in the Credential Manager for the specified Application Id.")]
    public string CredentialsForApplicationId { get; } = "TestApplication";

    [PasswordField(
        Label = "Credentials for Editable Application Id",
        ToolTip = "Select credentials stored in the Credential Manager for the specified Application Id.")]
    public string CredentialsForEditableApplicationId { get; set; } = "EditableApplication";

    [ColorField(
        Label = "Some color",
        ToolTip = "Color picker control to select a color.")]
    public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;

    // Conditional visibility example fields
    [TextField(Label = "")]
    public string ConditionalVisibilityExamples { get; } = "*** This section demonstrates conditional visibility of fields based on user input. ***";

    [BooleanField(Label = "Show the text field by clicking this")]
    public bool ShowTextField { get; set; }

    [TextField(HelperText = "Write 'Apple' to show more options.",
        Visibility = nameof(ShowTextField),
        ToolTip = "This field is shown or hidden based on the 'Show Text Field' checkbox.")]
    [RegularExpression("Apple", ErrorMessage = "Please enter 'Apple' to proceed.")]
    public string? TextInput { get; set; }

    [OptionsField(HelperText = "Select Beta to get more options",
        Visibility = $"{nameof(ShowTextField)} && {nameof(TextInput)} == 'Apple'")]
    [RegularExpression("Beta", ErrorMessage = "Please select 'Beta' to proceed.")]
    public SampleEnum OptionsInput { get; set; }

    [IntegerField(HelperText = "Are you over 18?",
        Visibility = $"{nameof(OptionsInput)} == 'Beta'")]
    [Range(0, 120, ErrorMessage = "Please enter a valid age between 0 and 120.")]
    public int NumericInput { get; set; }

    [TextField(
        Visibility = $"{nameof(NumericInput)} >= 18 && {nameof(OptionsInput)} == 'Beta'")]
    public string Notification { get; } = "You are old enough!";

    [ListField(
        Label = "Add items to the list",
        HelperText = "Add at least 3 items to see a notification",
        Visibility = $"{nameof(TextInput)} == 'Apple'")]
    [MinLength(3, ErrorMessage = "Please add at least 3 items to the list.")]
    public List<string>? Items { get; set; }

    [TextField(
        Label = "List Count Notification",
        Visibility = $"{nameof(Items)}.Count > 2")]
    public string? MoreThanTwoItemsNotification { get; } = "You have added more than two items!";
}


/// <summary>
/// This class implements a custom AutoFill collector for providing dynamic options.
/// It generates a dictionary of key-value pairs to be used as options in the UI.
/// Use values from the args class to customize the options if needed.
/// </summary>
internal class CustomAutoFillCollector : IAsyncAutoFillCollector<AssistantDemoExtensionArgs>
{
    public Task<Dictionary<string, string>> Get(AssistantDemoExtensionArgs args, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>();
        for (int i = 1; i <= 5; i++)
        {
            result.Add($"Key{i}", $"Display value {i}");
        }
        return Task.FromResult(result);
    }
}

/// <summary>
/// This enum represents sample options for demonstration purposes.
/// </summary>
public enum SampleEnum
{
    Alpha,
    Beta,
    Gamma
}

/// <summary>
/// This enum represents custom options for demonstration purposes.
/// Descriptions are shown as user-friendly names in the UI.
/// </summary>
public enum CustomEnum
{
    [Description("Option 1")]
    Option1,

    [Description("Option 2")]
    Option2,

    [Description("Option 3")]
    Option3,

    [Description("Option 4")]
    Option4,

    [Description("Option 5")]
    Option5
}