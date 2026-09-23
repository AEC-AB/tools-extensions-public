[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Za-z][0-9A-Za-z.+-]*$')]
    [string]$PackageVersion,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{7,64}$')]
    [string]$SourceSha,

    [string]$RepositoryRoot = (Get-Location).Path,

    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'

$packageId = 'CW.Assistant.Extensions.Docs'
$payloadPrefix = 'contentFiles/any/any/Resources/ExtensionDocs/'
$requiredPaths = @(
    'AGENT.md'
    'QUICK_START.md'
    'PLATFORM_GUIDES/ASSISTANT.md'
    'PLATFORM_GUIDES/AUTOCAD.md'
    'PLATFORM_GUIDES/NAVISWORKS.md'
    'PLATFORM_GUIDES/REVIT.md'
    'PLATFORM_GUIDES/REVIT_APP_EXTENSION.md'
    'PLATFORM_GUIDES/TEKLA.md'
    'PLATFORM_GUIDES/TEKLA_APP_EXTENSION.md'
)

$resolvedRepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$docsRoot = Join-Path $resolvedRepositoryRoot 'docs'
$destinationRoot = Join-Path $docsRoot 'dotnet'
$landingPagePath = Join-Path $docsRoot 'README.md'
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "extension-docs-$([Guid]::NewGuid().ToString('N'))"
$stagedRoot = Join-Path $temporaryRoot 'staged'
$downloadedPackage = Join-Path $temporaryRoot "$packageId.$PackageVersion.nupkg"

function Get-RelativePayloadPath([string]$EntryName) {
    $normalizedName = $EntryName.Replace('\', '/')
    if (-not $normalizedName.StartsWith($payloadPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    $relativePath = $normalizedName.Substring($payloadPrefix.Length)
    if ([string]::IsNullOrWhiteSpace($relativePath) -or $relativePath.EndsWith('/')) {
        return $null
    }

    $segments = $relativePath.Split('/')
    if ($segments | Where-Object { $_ -in @('', '.', '..') -or $_.Contains(':') }) {
        throw "Package entry '$EntryName' has an unsafe path."
    }

    return $relativePath
}

try {
    [void][IO.Directory]::CreateDirectory($stagedRoot)

    if ([string]::IsNullOrWhiteSpace($PackagePath)) {
        $normalizedVersion = $PackageVersion.ToLowerInvariant()
        $normalizedId = $packageId.ToLowerInvariant()
        $packageUrl = "https://api.nuget.org/v3-flatcontainer/$normalizedId/$normalizedVersion/$normalizedId.$normalizedVersion.nupkg"

        $downloaded = $false
        for ($attempt = 1; $attempt -le 12; $attempt++) {
            try {
                Invoke-WebRequest -Uri $packageUrl -OutFile $downloadedPackage -UseBasicParsing
                $downloaded = $true
                break
            }
            catch {
                if ($attempt -eq 12) {
                    throw
                }

                Write-Warning "Package download attempt $attempt failed. Retrying in 10 seconds."
                Start-Sleep -Seconds 10
            }
        }

        if (-not $downloaded) {
            throw "Failed to download $packageId $PackageVersion."
        }

        $resolvedPackagePath = $downloadedPackage
    }
    else {
        $resolvedPackagePath = [IO.Path]::GetFullPath($PackagePath)
    }

    if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf)) {
        throw "Package '$resolvedPackagePath' does not exist."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
    try {
        foreach ($entry in $archive.Entries) {
            $relativePath = Get-RelativePayloadPath $entry.FullName
            if ($null -eq $relativePath) {
                continue
            }

            $targetPath = Join-Path $stagedRoot ($relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
            $resolvedTargetPath = [IO.Path]::GetFullPath($targetPath)
            $stagedPrefix = $stagedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
            if (-not $resolvedTargetPath.StartsWith($stagedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Package entry '$($entry.FullName)' escapes the staging directory."
            }

            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedTargetPath))
            $entryStream = $entry.Open()
            $targetStream = $null
            try {
                $targetStream = [IO.File]::Open($resolvedTargetPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
                $entryStream.CopyTo($targetStream)
            }
            finally {
                if ($null -ne $targetStream) { $targetStream.Dispose() }
                $entryStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }

    foreach ($requiredPath in $requiredPaths) {
        $candidate = Join-Path $stagedRoot ($requiredPath.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "The package is missing required documentation '$requiredPath'."
        }
    }

    & (Join-Path $PSScriptRoot 'Test-MarkdownLinks.ps1') -RootPath $stagedRoot

    $privateReferences = Get-ChildItem -LiteralPath $stagedRoot -Filter '*.md' -File -Recurse |
        Select-String -Pattern @(
            'dev\.azure\.com/cowi-tools'
            'github\.com/AEC-AB/tools(?:[/?#]|$)'
            '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
            'github_pat_[A-Za-z0-9_]{20,}'
            'gh[pousr]_[A-Za-z0-9]{20,}'
            '(?:AccountKey|SharedAccessSignature|sig)=[A-Za-z0-9%+/=_-]{16,}'
        ) -CaseSensitive:$false
    if ($privateReferences) {
        $locations = $privateReferences | ForEach-Object { "$($_.Path):$($_.LineNumber)" }
        throw "The documentation contains private repository references: $($locations -join ', ')."
    }

    if (-not (Test-Path -LiteralPath $landingPagePath -PathType Leaf)) {
        throw "The public landing page '$landingPagePath' does not exist."
    }

    $landingPage = Get-Content -Raw -LiteralPath $landingPagePath
    $startMarker = '<!-- extension-docs-sync:start -->'
    $endMarker = '<!-- extension-docs-sync:end -->'
    if ([Regex]::Matches($landingPage, [Regex]::Escape($startMarker)).Count -ne 1 -or
        [Regex]::Matches($landingPage, [Regex]::Escape($endMarker)).Count -ne 1) {
        throw 'The public landing page must contain exactly one synchronization marker pair.'
    }
    $markerPattern = [Regex]::Escape($startMarker) + '.*?' + [Regex]::Escape($endMarker)
    $provenance = @"
$startMarker
Published from [$packageId $PackageVersion](https://www.nuget.org/packages/$packageId/$PackageVersion). Source commit: ``$SourceSha``.

## Start here

- [Quick start](./dotnet/QUICK_START.md)
- [Args developer guide](./dotnet/ARGS_DEVELOPER_GUIDE.md)
- [Assistant MCP guide](./dotnet/ASSISTANT_MCP.md)
- [Cookbook](./dotnet/COOKBOOK.md)
- [Reference](./dotnet/REFERENCE.md)
- [Platform guides](./dotnet/PLATFORM_GUIDES/)
$endMarker
"@
    $updatedLandingPage = [Regex]::Replace($landingPage, $markerPattern, $provenance, [Text.RegularExpressions.RegexOptions]::Singleline)
    if (Test-Path -LiteralPath $destinationRoot) {
        Remove-Item -LiteralPath $destinationRoot -Recurse -Force
    }
    [void][IO.Directory]::CreateDirectory($docsRoot)
    Move-Item -LiteralPath $stagedRoot -Destination $destinationRoot
    [IO.File]::WriteAllText($landingPagePath, $updatedLandingPage, [Text.UTF8Encoding]::new($false))

    Write-Host "Synchronized $packageId $PackageVersion into '$destinationRoot'."
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
