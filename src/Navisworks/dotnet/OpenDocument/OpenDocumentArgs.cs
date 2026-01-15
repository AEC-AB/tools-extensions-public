using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace OpenDocument
{
    public class OpenDocumentArgs
    {
        [FilePickerField(Label = "Path", ToolTip = "File Path(ex: C:\\MyFile.ifc)", Hint = "Select a file")]
        [Required(ErrorMessage = "Value can not be empty")]
        public string Path { get; set; } = string.Empty;
    }
}