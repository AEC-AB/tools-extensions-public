using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StreamBim;

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

        return name.EndsWith("-revs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "_backup", StringComparison.OrdinalIgnoreCase);
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
        var normalizedRemotePath = remotePath.Replace('\\', '/');

        var relativeRemotePath = projectPrefix.Length == 0 || projectPrefix == "/"
            ? normalizedRemotePath.TrimStart('/')
            : normalizedRemotePath.StartsWith(projectPrefix + "/", StringComparison.OrdinalIgnoreCase)
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

    internal static string CreateRemotePath(string projectPath, string? targetFolder, string uploadFolder, string localFilePath)
    {
        var relativePath = Path.GetRelativePath(uploadFolder, localFilePath).Replace('\\', '/');

        if (relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Local file path is outside the upload folder.");
        }

        var remoteBase = CombineFtpPath(projectPath, targetFolder);
        return CombineFtpPath(remoteBase, relativePath);
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
        var regex = "^" + Regex.Escape(fileNameWithWildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string NormalizeConfiguredFile(string projectPath, string? configuredFile)
    {
        if (string.IsNullOrWhiteSpace(configuredFile))
        {
            return string.Empty;
        }

        var normalized = NormalizeRelativePath(configuredFile.Trim().TrimStart('/').TrimEnd('/'));
        var trimmedProjectPath = projectPath.Trim('/');
        if (!string.IsNullOrEmpty(trimmedProjectPath) &&
            normalized.StartsWith(trimmedProjectPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[(trimmedProjectPath.Length + 1)..];
        }
        else if (string.Equals(normalized, trimmedProjectPath, StringComparison.OrdinalIgnoreCase))
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

        var normalized = project.Trim();
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

                return segment;
            });

        return string.Join("/", segments);
    }
}
