param(
    [Parameter(Mandatory = $false)]
    [string]$SourceRoot,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
}

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $scriptDirectory "..\docs\dotnet"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $scriptDirectory "..\artifacts\extension-docs.bundle.json"
}

function Get-Integration([string]$filePath, [string]$relativePath) {
    if ($relativePath -like "PLATFORM_GUIDES/*") {
        $platform = [System.IO.Path]::GetFileNameWithoutExtension($filePath)
        return $platform
    }

    return "Assistant"
}

function Get-DocId([string]$relativePath) {
    $id = $relativePath.Replace('\\', '/').ToLowerInvariant()
    $id = $id -replace "\.md$", ""
    $id = $id -replace "[^a-z0-9/]+", "-"
    $id = $id -replace "/", "-"
    $id = $id.Trim('-')

    return $id
}

$sourceRootFullPath = [System.IO.Path]::GetFullPath($SourceRoot)
if (-not (Test-Path -Path $sourceRootFullPath -PathType Container)) {
    throw "Source root not found: $sourceRootFullPath"
}

$targetFiles = @(
    "QUICK_START.md",
    "ARGS_DEVELOPER_GUIDE.md",
    "COOKBOOK.md",
    "REFERENCE.md",
    "PLATFORM_GUIDES/ASSISTANT.md",
    "PLATFORM_GUIDES/REVIT.md",
    "PLATFORM_GUIDES/AUTOCAD.md",
    "PLATFORM_GUIDES/TEKLA.md",
    "PLATFORM_GUIDES/NAVISWORKS.md"
)

$documents = @()
foreach ($targetFile in $targetFiles) {
    $absolutePath = Join-Path $sourceRootFullPath $targetFile
    if (-not (Test-Path -Path $absolutePath -PathType Leaf)) {
        Write-Warning "Skipping missing doc: $targetFile"
        continue
    }

    $content = Get-Content -Path $absolutePath -Raw
    $title = [System.IO.Path]::GetFileNameWithoutExtension($absolutePath).Replace('_', ' ')
    $relativePath = $targetFile.Replace('\\', '/')

    $documents += [ordered]@{
        id = Get-DocId -relativePath $relativePath
        title = $title
        integration = Get-Integration -filePath $absolutePath -relativePath $relativePath
        sourcePath = "docs/dotnet/$relativePath"
        content = $content
    }
}

$bundle = [ordered]@{
    version = "1.0.0"
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    documents = $documents
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
if ([string]::IsNullOrWhiteSpace($outputDirectory) -eq $false) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$bundle | ConvertTo-Json -Depth 10 | Set-Content -Path $outputFullPath -Encoding UTF8

Write-Host "Generated extension docs bundle: $outputFullPath"
Write-Host "Documents: $($documents.Count)"
