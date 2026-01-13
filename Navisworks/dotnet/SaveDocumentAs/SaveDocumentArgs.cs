using CW.Assistant.Extensions.Contracts.Fields;

namespace SaveDocumentAs
{
    public class SaveDocumentAsArgs
    {
        [SaveFileField(Label = "Path", Hint = "Save file path", ToolTip = "File Path(ex: C:\\MyFile.nwd)")]
        public string Path { get; set; } = string.Empty;
    }
}