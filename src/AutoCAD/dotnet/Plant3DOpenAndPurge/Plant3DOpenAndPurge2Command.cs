using System.Text;
using System.Xml;
using Autodesk.AutoCAD.Internal;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.ProjectManager;

namespace Plant3DOpenAndPurge2;

public class Plant3DOpenAndPurge2Command : IAutoCADExtension<Plant3DOpenAndPurge2Args>
{
       public IExtensionResult Run(IAutoCADExtensionContext context, Plant3DOpenAndPurge2Args args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(args.ProjectPath))
            return Result.Text.Failed("Project path is null or empty");

        var result = new StringBuilder();

        var project = PlantProject.LoadProject(args.ProjectPath, true, "", "");
        if (project is null)
            return Result.Text.Failed("Failed to load project from the specified path");
        result.AppendLine($"Project '{project.Name}' loaded successfully.");
       

        PlantApplication.SetCurrentProject(project);

        
        var numnerOfParts = project.ProjectParts.Count;
        result.AppendLine($"Number of project parts: {numnerOfParts}");

        foreach (var part in project.ProjectParts)
        {
            result.AppendLine("Project Name =  " + part.ProjectName);

            var projPartName = project.ProjectPartName(part);
            result.AppendLine("Project Part Name = " + projPartName);
            result.AppendLine("Project Directory = " + part.ProjectDirectory);

            var dwgList = part.GetPnPDrawingFiles();
            result.AppendLine("Number of PnP Drawings = " + dwgList.Count);

            List<string> allNotPurgeds = [];
            List<string> allPurgedFilesReApps = [];

            foreach (var dwg in dwgList)
            {
                result.AppendLine("Absolute File name = " + dwg.AbsoluteFileName);

                try
                {
                    PurgeRegApps(dwg.AbsoluteFileName, out string? notPurgeds, out string? purgedFilesReApps, result);

                    allNotPurgeds.Add(notPurgeds);
                    allPurgedFilesReApps.Add(purgedFilesReApps);
                   
                }
                catch (Exception ex)
                {
                    result.AppendLine($"\nRegApp purge failed: {dwg.AbsoluteFileName} -> {ex.Message}");
                }
            }
        }

        // Return a result with the message
        return Result.Text.Succeeded(result.ToString());
    }


    private static void PurgeRegApps(string filePath, out string notPurgedsFiles, out string purgedFilesReApps, StringBuilder result)
    {
        notPurgedsFiles = "";
        purgedFilesReApps = "";

        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
        {
            notPurgedsFiles = System.IO.Path.GetFileName(filePath);
            result.AppendLine($"File not found or invalid: {filePath}");
            return;
        }

        using Database db = new Database(false, true);
        {
            db.ReadDwgFile(filePath, System.IO.FileShare.ReadWrite, false, null);
            //db.CloseInput(true);
            db.CloseInput(false);

            bool purgedSomething = true;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                while (purgedSomething)
                {
                    purgedSomething = false;

                    RegAppTable regAppTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);

                    ObjectIdCollection ids = [.. regAppTable];

                    db.Purge(ids);

                    result.AppendLine($"Number of RegApps purged = {ids.Count}");

                    foreach (ObjectId id in ids)
                    {
                        try
                        {
                            DBObject obj = tr.GetObject(id, OpenMode.ForWrite, false);
                            if (obj != null && !obj.IsErased)
                            {
                                obj.Erase();
                                purgedSomething = true;
                                result.AppendLine("Object purged = " + obj.Handle.ToString());
                            }
                        }
                        catch
                        {
                            // skip non-erasable regapps (e.g. ACAD)
                        }
                    }
                }

                try
                {
                    db.CloseInput(true);
                        tr.Commit();
                        db.SaveAs(filePath, DwgVersion.Current);
                    purgedFilesReApps = System.IO.Path.GetFileName(filePath);
                    result.AppendLine("File Saved = " + filePath);
                }
                catch (Exception ex)
                {
                    result.AppendLine("File not saved = " + filePath);
                }
            }
        }
    }
}


