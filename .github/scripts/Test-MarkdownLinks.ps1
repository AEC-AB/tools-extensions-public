[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = [IO.Path]::GetFullPath($RootPath)
$rootPrefix = $resolvedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$brokenLinks = [Collections.Generic.List[string]]::new()

function Test-LinkTarget([IO.FileInfo]$MarkdownFile, [string]$RawTarget) {
    $target = $RawTarget.Trim().Trim('<', '>')
    if ($target -match '^[A-Za-z][A-Za-z0-9+.-]*:' -or $target.StartsWith('#') -or $target.StartsWith('//')) {
        return
    }

    $pathWithoutFragment = ($target -split '[?#]', 2)[0]
    if ([string]::IsNullOrWhiteSpace($pathWithoutFragment)) {
        return
    }

    $decodedPath = [Uri]::UnescapeDataString($pathWithoutFragment).Replace('/', [IO.Path]::DirectorySeparatorChar)
    $linkPath = if ($decodedPath.StartsWith([IO.Path]::DirectorySeparatorChar)) {
        Join-Path $resolvedRoot $decodedPath.TrimStart([IO.Path]::DirectorySeparatorChar)
    }
    else {
        Join-Path $MarkdownFile.DirectoryName $decodedPath
    }
    $resolvedLinkPath = [IO.Path]::GetFullPath($linkPath)
    $relativeMarkdownPath = $MarkdownFile.FullName.Substring($resolvedRoot.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    if (-not $resolvedLinkPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $resolvedLinkPath)) {
        $brokenLinks.Add("$relativeMarkdownPath -> $target")
    }
}

foreach ($markdownFile in Get-ChildItem -LiteralPath $resolvedRoot -Filter '*.md' -File -Recurse) {
    $content = Get-Content -Raw -LiteralPath $markdownFile.FullName
    $targets = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

    foreach ($match in [Regex]::Matches($content, '\[[^\]]+\]\((?<target><[^>]+>|[^)\s]+)(?:\s+["''][^)]*["''])?\)')) {
        [void]$targets.Add($match.Groups['target'].Value)
    }
    foreach ($match in [Regex]::Matches($content, '(?m)^\s*\[[^\]]+\]:\s*(?<target><[^>]+>|\S+)')) {
        [void]$targets.Add($match.Groups['target'].Value)
    }
    foreach ($match in [Regex]::Matches($content, '(?i)(?:href|src)\s*=\s*["''](?<target>[^"'']+)["'']')) {
        [void]$targets.Add($match.Groups['target'].Value)
    }

    foreach ($target in $targets) {
        Test-LinkTarget $markdownFile $target
    }
}

if ($brokenLinks.Count -gt 0) {
    throw "The documentation contains broken relative links: $($brokenLinks -join ', ')."
}

Write-Host "Validated Markdown links under '$resolvedRoot'."
