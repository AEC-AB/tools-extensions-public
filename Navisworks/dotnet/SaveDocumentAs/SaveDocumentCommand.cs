using System.IO;

namespace SaveDocumentAs;
public class SaveDocumentAsCommand : INavisworksExtension<SaveDocumentAsArgs>
{
    public IExtensionResult Run(INavisworksExtensionContext context, SaveDocumentAsArgs args, CancellationToken cancellationToken)
    {
        var saveFolder = Path.GetDirectoryName(args.Path);
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
        var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
        doc.SaveFile(args.Path);
        return Result.Text.Succeeded($"Saved: {args.Path}");
    }
}
