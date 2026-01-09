using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrintPDF.Helpers;

public static class ExportFileHelpers
{
    public static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleaned = input.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(cleaned);
    }

    public static string CreateTempFileNameForSheet(ViewSheet sheet)
    {
        return $"Sheet-{Guid.NewGuid()}-{SanitizeFileName(sheet.Name)}.pdf";
    }

    public static string FindGeneratedPdfForSheet(string folder, string sheetName)
    {
        try
        {
            var files = new System.IO.DirectoryInfo(folder).GetFiles("*.pdf");
            // Prefer files containing the sheetName (sanitized) or recent ones.
            var sanitized = SanitizeFileName(sheetName);
            var match = files.OrderByDescending(f => f.LastWriteTimeUtc)
                             .FirstOrDefault(f => f.Name.IndexOf(sanitized, StringComparison.CurrentCultureIgnoreCase) >= 0
                                                   || f.Name.StartsWith("Sheet-", StringComparison.CurrentCultureIgnoreCase));
            return match?.FullName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string FindMostRecentPdf(string folder)
    {
        try
        {
            var dir = new System.IO.DirectoryInfo(folder ?? string.Empty);
            var recent = dir.GetFiles("*.pdf").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            return recent?.FullName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string FindGeneratedPdfForNameOrRecent(string folder, string name)
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var found = FindGeneratedPdfForSheet(folder, name);
                if (!string.IsNullOrEmpty(found) && System.IO.File.Exists(found))
                    return found;
            }
            var recent = FindMostRecentPdf(folder);
            return recent;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool MoveOrCopyFileSafely(string sourcePath, string destinationPath)
    {
        try
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                return false;

            if (!System.IO.File.Exists(sourcePath))
                return false;

            // Ensure destination directory exists
            var destDir = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
                System.IO.Directory.CreateDirectory(destDir);

            // If destination exists, delete it first
            if (System.IO.File.Exists(destinationPath))
                System.IO.File.Delete(destinationPath);

            try
            {
                System.IO.File.Move(sourcePath, destinationPath);
                return true;
            }
            catch
            {
                // Move failed, try copy-delete fallback
                try
                {
                    System.IO.File.Copy(sourcePath, destinationPath);
                    System.IO.File.Delete(sourcePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    public static string ReplaceRevitParameterVariables(string customFileName, ViewSheet sheet)
    {
        var parameters = sheet.Parameters;
        foreach (Parameter param in parameters)
        {
            var paramValue = param.AsValueString();
            if (string.IsNullOrEmpty(paramValue))
                continue;
            var paramName = param.Definition.Name;
            customFileName = customFileName.Replace("{" + paramName + "}", paramValue);
        }
        return customFileName;
    }

    public static bool ContainsInvalidFileNameChars(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return input.Any(c => invalid.Contains(c));
    }
}
