$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot '..\Sync-ExtensionDocs.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "extension-docs-sync-$([Guid]::NewGuid().ToString('N'))"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function New-TestPackage([string]$Path, [switch]$MissingRequiredPage, [switch]$BrokenLink) {
    $packageRoot = Join-Path $testRoot ([IO.Path]::GetRandomFileName())
    $payloadRoot = Join-Path $packageRoot 'contentFiles\any\any\Resources\ExtensionDocs'
    $platformRoot = Join-Path $payloadRoot 'PLATFORM_GUIDES'
    [void][IO.Directory]::CreateDirectory($platformRoot)

    if (-not $MissingRequiredPage) {
        $quickStart = if ($BrokenLink) { '# Quick start' + [Environment]::NewLine + '[Missing](./NOT_THERE.md)' } else { '# Quick start' }
        Set-Content -LiteralPath (Join-Path $payloadRoot 'QUICK_START.md') -Value $quickStart
    }

    Set-Content -LiteralPath (Join-Path $payloadRoot 'AGENT.md') -Value '# Agent'
    foreach ($name in @('ASSISTANT.md', 'AUTOCAD.md', 'NAVISWORKS.md', 'REVIT.md', 'REVIT_APP_EXTENSION.md', 'TEKLA.md', 'TEKLA_APP_EXTENSION.md')) {
        Set-Content -LiteralPath (Join-Path $platformRoot $name) -Value "# $name"
    }

    $zipPath = [IO.Path]::ChangeExtension($Path, '.zip')
    Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath
    Move-Item -LiteralPath $zipPath -Destination $Path
}

try {
    $repositoryRoot = Join-Path $testRoot 'repository'
    $docsRoot = Join-Path $repositoryRoot 'docs'
    $dotnetRoot = Join-Path $docsRoot 'dotnet'
    [void][IO.Directory]::CreateDirectory($dotnetRoot)
    Set-Content -LiteralPath (Join-Path $dotnetRoot 'STALE.md') -Value 'old'
    Set-Content -LiteralPath (Join-Path $docsRoot 'README.md') -Value @'
# Extension development documentation

<!-- extension-docs-sync:start -->
Pending initial publication.
<!-- extension-docs-sync:end -->

Start with the [quick start](./dotnet/QUICK_START.md).
'@

    $validPackage = Join-Path $testRoot 'valid.nupkg'
    New-TestPackage -Path $validPackage

    & $scriptPath `
        -PackageVersion '26.7.0' `
        -SourceSha '0123456789abcdef' `
        -RepositoryRoot $repositoryRoot `
        -PackagePath $validPackage

    Assert-True (Test-Path -LiteralPath (Join-Path $dotnetRoot 'QUICK_START.md')) 'The package payload was not synchronized.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot 'STALE.md'))) 'A stale documentation file was not removed.'

    $landingPage = Get-Content -Raw -LiteralPath (Join-Path $docsRoot 'README.md')
    Assert-True ($landingPage.Contains('26.7.0')) 'The landing page does not contain the package version.'
    Assert-True ($landingPage.Contains('0123456789abcdef')) 'The landing page does not contain the source commit.'

    $beforeSecondRun = Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $docsRoot 'README.md'), (Join-Path $dotnetRoot 'QUICK_START.md')
    & $scriptPath `
        -PackageVersion '26.7.0' `
        -SourceSha '0123456789abcdef' `
        -RepositoryRoot $repositoryRoot `
        -PackagePath $validPackage
    $afterSecondRun = Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $docsRoot 'README.md'), (Join-Path $dotnetRoot 'QUICK_START.md')
    Assert-True (($beforeSecondRun.Hash -join ',') -eq ($afterSecondRun.Hash -join ',')) 'A second synchronization changed identical content.'

    $invalidPackage = Join-Path $testRoot 'invalid.nupkg'
    New-TestPackage -Path $invalidPackage -MissingRequiredPage
    $failed = $false
    try {
        & $scriptPath `
            -PackageVersion '26.7.1' `
            -SourceSha 'fedcba9876543210' `
            -RepositoryRoot $repositoryRoot `
            -PackagePath $invalidPackage
    }
    catch {
        $failed = $true
    }

    Assert-True $failed 'A package without QUICK_START.md should fail validation.'
    Assert-True (Test-Path -LiteralPath (Join-Path $dotnetRoot 'QUICK_START.md')) 'A failed synchronization replaced the current documentation.'

    $brokenLinkPackage = Join-Path $testRoot 'broken-link.nupkg'
    New-TestPackage -Path $brokenLinkPackage -BrokenLink
    $failed = $false
    try {
        & $scriptPath `
            -PackageVersion '26.7.2' `
            -SourceSha 'abcdef0123456789' `
            -RepositoryRoot $repositoryRoot `
            -PackagePath $brokenLinkPackage
    }
    catch {
        $failed = $true
    }

    Assert-True $failed 'A package with a broken relative link should fail validation.'
    Assert-True (Test-Path -LiteralPath (Join-Path $dotnetRoot 'QUICK_START.md')) 'Broken-link validation replaced the current documentation.'

    Write-Host 'Extension docs synchronization tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
