using RevitExtensionDemo.ACC;

namespace RevitExtensionDemo;

public class RevitExtensionDemoCommand : IRevitExtension<RevitExtensionDemoArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, RevitExtensionDemoArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;

        var message = string.Empty;

        if (document is null)
        {
            return Result.Text.Failed("No active document found.");
        }

        if (args.AutodeskClient is null)
        {
            return Result.Text.Failed("No Autodesk Client configuration provided.");
        }

        if (args.ValueCopy is null)
        {
            return Result.Text.Failed("No ValueCopy configuration provided.");
        }

        var client = new AccClient(args.AutodeskClient);
        var hubs = client.GetHubs();
        message += $"Found {hubs.Data.Count} hubs.\n";
        var hub = hubs.Data.First(x => x.Attributes.Extension.Type == "hubs:autodesk.bim360:Account");
        message += $"Selected hub: {hub.Attributes.Name}\n";

        var projects = client.GetProjects(hub.Id);
        message += $"Found {projects.Data.Count} projects\n";
        var project = projects.Data[0];
        message += $"Selected project: {project.Attributes.Name}\n";

        var topFolders = client.GetTopFolders(hub.Id, project.Id);
        var folder = topFolders.Data[0];
        message += $"Selected folder: {folder.Attributes.DisplayName}\n";
        var folderContent = client.GetFolderContents(project.Id, folder.Id);
        message += $"Found {folderContent.Data.Count} items in folder.\n\n";
        foreach (var item in folderContent.Data)
        {
            message += $"{item.Type}: {item.Attributes.DisplayName}\n";
        }

        using var trans = new Transaction(document, "RevitExtensionDemo Command");
        trans.Start();

        //TODO: Write the macro here!

        //ValueCopy Demo Implementation

        var valueCopyHandler = context.GetHandler(args.ValueCopy);
        //var result = valueCopyHandler.Handle(sourceElement, targetElement);

        trans.Commit();

        // Return a result with the message
        return Result.Text.Succeeded(message);        
    }
}
