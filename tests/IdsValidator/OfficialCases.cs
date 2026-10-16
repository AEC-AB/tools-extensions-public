using System.IO.Compression;
using System.Security.Cryptography;

internal static class OfficialCases
{
    private const string Commit = "870f9c4e6e8f414e737b4d84ca1aa9b46fc6c8f3";
    private const string Sha256 = "6035F80D1C40104AC7AEC8896954D40BFDA0A2F22A75CBF12A95283E7C2AB8B8";

    public static async Task EnsureAsync(string directory)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        Directory.CreateDirectory(directory);
        var archive = Path.GetFullPath("IDS.zip", Path.GetDirectoryName(directory) ?? directory);
        if (!File.Exists(archive))
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            await using var source = await client.GetStreamAsync("https://codeload.github.com/buildingSMART/IDS/zip/" + Commit);
            await using var target = File.Create(archive);
            await source.CopyToAsync(target);
        }
        await using (var stream = File.OpenRead(archive))
            if (Convert.ToHexString(await SHA256.HashDataAsync(stream)) != Sha256)
                throw new InvalidDataException("Official IDS corpus archive checksum mismatch");
        using var zip = ZipFile.OpenRead(archive);
        const string marker = "/Documentation/ImplementersDocumentation/TestCases/";
        foreach (var entry in zip.Entries)
        {
            var start = entry.FullName.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0 || !(entry.FullName.EndsWith(".ids") || entry.FullName.EndsWith(".ifc"))) continue;
            var target = ExtractionPath(directory, entry.FullName[(start + marker.Length)..]);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
        }
    }

    internal static string ExtractionPath(string directory, string relativePath)
    {
        directory = Path.GetFullPath(directory);
        var prefix = Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;
        if (Path.IsPathRooted(relativePath)) throw new InvalidDataException("Rooted corpus archive entry");
        var target = Path.GetFullPath(relativePath, directory);
        if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsafe corpus archive entry");
        return target;
    }
}
