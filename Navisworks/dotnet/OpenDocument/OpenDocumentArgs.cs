using CW.Assistant.Extensions.Contracts.Fields;


namespace OpenDocument
{
    public class OpenDocumentArgs
    {
        [FilePickerField(Label = "Path", ToolTip = "File Path(ex: C:\\MyFile.ifc)", Hint = "Select a file")]
        public string Path { get; set; } = string.Empty;
    }
}