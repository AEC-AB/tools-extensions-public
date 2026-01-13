namespace TeklaIFCExport;

public class TeklaIFCExportCommand : ITeklaExtension<TeklaIFCExportArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, TeklaIFCExportArgs args, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(args.ExportConfigFilePath))
            {
                return Result.Text.Failed("IFC export config path is not set.");
            }

            var bytes = File.ReadAllBytes(args.ExportConfigFilePath);

            MemoryStream memStream = new(bytes);
            XmlSerializer serializer = new(typeof(IFCExportConfig));
            IFCExportConfig exportConfig = (IFCExportConfig)serializer.Deserialize(memStream);

            if (!string.IsNullOrEmpty(args.FilePathOverride))
            {
                exportConfig.OutputFile = args.FilePathOverride!;
            }

            if (string.IsNullOrEmpty(exportConfig.OutputFile))
            {
                return Result.Text.Failed("Output file path is not set.");
            }

            exportConfig.OutputFile = Path.GetFullPath(exportConfig.OutputFile);
            if (!exportConfig.OutputFile.ToUpper().EndsWith(".IFC"))
            {
                exportConfig.OutputFile += ".ifc";
            }
            var directoryPath = Path.GetDirectoryName(exportConfig.OutputFile);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var logFilePath = Path.Combine(Path.GetDirectoryName(exportConfig.OutputFile), $"{Path.GetFileNameWithoutExtension(exportConfig.OutputFile)}.log");

            var componentInput = new ComponentInput();
            componentInput.AddOneInputPosition(new Point(0, 0, 0));
            var comp = new Tekla.Structures.Model.Component(componentInput)
            {
                Name = "ExportIFC",
                Number = BaseComponent.PLUGIN_OBJECT_NUMBER
            };

            var propInfos = exportConfig.GetType().GetProperties();
            if (args.BasePointName.ToUpper() == "Model Origin".ToUpper())
            {
                comp.SetAttribute("LocationBy", 0);
            }
            else
            {
                BasePoint basePointByName = ProjectInfo.GetBasePointByName(args.BasePointName);

                if (string.IsNullOrEmpty(basePointByName?.Name))
                {
                    return Result.Text.Failed($"Base point with name '{args.BasePointName}' not found.");
                }
                comp.SetAttribute("LocationBy", -1);
                var bpName = args.BasePointName;
                comp.SetAttribute("BasePointName", args.BasePointName);
            }
            foreach (var propInfo in propInfos)
            {
                var attribute = propInfo.GetAttribute<XmlElementAttribute>();
                if (attribute == null)
                {
                    continue;
                }
                var name = attribute.ElementName;
                var type = propInfo.PropertyType;
                var propValue = propInfo.GetValue(exportConfig);

                if (propValue != null)
                {
                    if (name == "LocationBy")
                    {
                        continue;
                    }
                    if (name == "BasePointName")
                    {
                        continue;
                    }
                    if (type == typeof(string))
                        comp.SetAttribute(name, propValue.ToString());
                    else if (type == typeof(int))
                        comp.SetAttribute(name, (int)propValue);
                }
            }
            comp.Insert();

            var logFileContentLines = File.ReadAllLines(logFilePath);
            var logFileContent = string.Join("\n", logFileContentLines);
            var numberOfExports = 0;
            foreach (var line in logFileContentLines)
            {
                if (line.Contains("Successful exports:"))
                {
                    var didParse = int.TryParse(line.Split(':').Last().Trim(), out numberOfExports);
                    if (!didParse)
                    {
                        continue;
                    }
                    if (numberOfExports > 0)
                    {
                        return Result.Text.Succeeded($"IFC Exported to {exportConfig.OutputFile} \n{logFileContent}");
                    }
                    break;
                }
            }
            if (numberOfExports == 0)
            {
                return Result.Text.Failed($"ERROR: {numberOfExports} WAS EXPORTED!\n\n{logFileContent}");
            }

            return Result.Text.PartiallySucceeded("Export completed with no results.");
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"An error occurred during IFC export: {ex.Message}");
        }
    }
}

public static class PropertyInfoExtensions
{
    public static TAttribute? GetAttribute<TAttribute>(this PropertyInfo pi) where TAttribute : class
    {
        TAttribute? attribute =
            (TAttribute?)pi.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault();
        return attribute;
    }
}