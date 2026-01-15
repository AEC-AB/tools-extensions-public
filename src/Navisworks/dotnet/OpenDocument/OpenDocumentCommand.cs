
using System.Threading;
using Autodesk.Navisworks.Api;
using CW.Assistant.Extensions.Contracts;
using CW.Assistant.Extensions.Navisworks;

namespace OpenDocument
{
    public class OpenDocumentCommand : INavisworksExtension<OpenDocumentArgs>
    {
        public IExtensionResult Run(INavisworksExtensionContext context, OpenDocumentArgs args, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(args.Path))
            {
                return Result.Text.Failed("File path cannot be empty");
            }

            var doc = Application.ActiveDocument;
            doc.OpenFile(args.Path);

            return Result.Text.Succeeded($"Opened file at path: {args.Path}");
        }
    }
}