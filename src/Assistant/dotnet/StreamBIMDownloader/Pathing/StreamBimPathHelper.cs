using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamBIMDownloader;

internal static class StreamBimPathHelper
{
    internal static bool ContainsIgnoredFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(IsIgnoredDirectoryName);
    }

    internal static bool IsIgnoredDirectoryName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalizedName = NormalizeForComparison(name);
        return normalizedName.EndsWith("-revs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedName, "_backup", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool AreEquivalentTimestamps(DateTime remoteTimestamp, DateTime localTimestamp)
    {
        if (remoteTimestamp == default || localTimestamp == default)
        {
            return false;
        }

        return (remoteTimestamp - localTimestamp).Duration() <= TimeSpan.FromSeconds(2);
    }

    internal static string CombineFtpPath(string basePath, string? relativePath)
    {
        var normalizedBasePath = string.IsNullOrWhiteSpace(basePath)
            ? "/"
            : "/" + basePath.Trim('/');

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return normalizedBasePath;
        }

        return normalizedBasePath == "/"
            ? "/" + relativePath.Trim('/')
            : normalizedBasePath + "/" + relativePath.Trim('/');
    }

    internal static string CombineRelativePath(string relativeFolderPath, string name)
    {
        if (string.IsNullOrWhiteSpace(relativeFolderPath))
        {
            return name;
        }

        return relativeFolderPath.Trim('/') + "/" + name;
    }

    internal static bool ContainsWildcard(string? fileName)
    {
        return !string.IsNullOrEmpty(fileName) && (fileName.Contains('*') || fileName.Contains('?'));
    }

    internal static string CreateDisplayPath(string projectPath, string configuredFile)
    {
        var safeProjectPath = NormalizeProjectPath(projectPath);
        var normalizedConfiguredFile = configuredFile.Trim().Replace('\\', '/').TrimStart('/');
        return CombineFtpPath(safeProjectPath, normalizedConfiguredFile).TrimStart('/');
    }

    internal static string CreateLocalPath(string downloadFolder, string projectPath, string remotePath)
    {
        var projectPrefix = NormalizeProjectPath(projectPath).TrimEnd('/');
        var normalizedRemotePath = NormalizeForComparison(remotePath.Replace('\\', '/'));

        var relativeRemotePath = projectPrefix.Length == 0 || projectPrefix == "/"
            ? normalizedRemotePath.TrimStart('/')
            : StartsWithNormalized(normalizedRemotePath, projectPrefix + "/")
                ? normalizedRemotePath[(projectPrefix.Length + 1)..]
                : normalizedRemotePath.TrimStart('/');

        var relativePath = NormalizeRelativePath(relativeRemotePath);
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var localPath = downloadFolder;
        foreach (var segment in segments)
        {
            if (Path.IsPathRooted(segment) || segment.Contains(Path.VolumeSeparatorChar))
            {
                throw new InvalidOperationException("Remote path contains an invalid rooted path segment.");
            }

            localPath = Path.Combine(localPath, segment);
        }

        var downloadRoot = Path.GetFullPath(downloadFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullLocalPath = Path.GetFullPath(localPath);
        if (!fullLocalPath.StartsWith(downloadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullLocalPath, downloadRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved download path escapes the selected download folder.");
        }

        return fullLocalPath;
    }

    internal static string GetLeafName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.TrimEnd('/');
        var lastSeparatorIndex = normalized.LastIndexOf('/');
        return lastSeparatorIndex < 0
            ? normalized
            : normalized[(lastSeparatorIndex + 1)..];
    }

    internal static bool MatchesWildcard(string fileName, string fileNameWithWildcard)
    {
        var regex = "^" + Regex.Escape(NormalizeForComparison(fileNameWithWildcard)).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        return Regex.IsMatch(NormalizeForComparison(fileName), regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string NormalizeConfiguredFile(string projectPath, string? configuredFile)
    {
        if (string.IsNullOrWhiteSpace(configuredFile))
        {
            return string.Empty;
        }

        var normalized = NormalizeRelativePath(configuredFile.Trim().TrimStart('/').TrimEnd('/'));
        var trimmedProjectPath = NormalizeForComparison(projectPath.Trim('/'));
        if (!string.IsNullOrEmpty(trimmedProjectPath) &&
            StartsWithNormalized(normalized, trimmedProjectPath + "/"))
        {
            normalized = normalized[(trimmedProjectPath.Length + 1)..];
        }
        else if (EqualsNormalized(normalized, trimmedProjectPath))
        {
            normalized = string.Empty;
        }

        return normalized;
    }

    internal static string NormalizeProjectPath(string? project)
    {
        if (string.IsNullOrWhiteSpace(project) || project == "/")
        {
            return "/";
        }

        var normalized = NormalizeForComparison(project.Trim());
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    internal static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var segments = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment =>
            {
                if (segment == "." || segment == "..")
                {
                    throw new ArgumentException("Path segments cannot contain '.' or '..'.");
                }

                return NormalizeForComparison(segment);
            });

        return string.Join("/", segments);
    }

    internal static bool ContainsNormalized(string value, string expected)
    {
        return NormalizeForComparison(value).Contains(NormalizeForComparison(expected), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool EqualsNormalized(string? left, string? right)
    {
        return string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeForComparison(string? value)
    {
        return (value ?? string.Empty).Normalize(NormalizationForm.FormC);
    }

    internal static bool StartsWithNormalized(string value, string expected)
    {
        return NormalizeForComparison(value).StartsWith(NormalizeForComparison(expected), StringComparison.OrdinalIgnoreCase);
    }
}
