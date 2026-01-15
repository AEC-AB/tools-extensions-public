param(
  [string]$ProjectsJson
)

$ErrorActionPreference = "Stop"

if (-not $ProjectsJson) {
  throw "ProjectsJson is required."
}

$projects = $ProjectsJson | ConvertFrom-Json
$projects = @($projects)

if (-not $projects -or $projects.Count -eq 0) {
  Write-Host "No projects to build."
  return
}

function Get-ProjectConfigurations {
  param(
    [string]$ProjectPath
  )

  $projectFullPath = Resolve-Path $ProjectPath
  $projectDir = Split-Path $projectFullPath -Parent

  $hasProjectOverride = $false
  [xml]$projectXml = Get-Content $projectFullPath
  $overrideNodes = $projectXml.SelectNodes("//*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='Configurations']")
  if ($overrideNodes -and $overrideNodes.Count -gt 0) {
    $hasProjectOverride = $true
  }

  $directoryPropsPath = $null
  if (-not $hasProjectOverride) {
    $dir = $projectDir
    while ($dir) {
      $candidate = Join-Path $dir "Directory.Build.props"
      if (Test-Path $candidate) {
        $directoryPropsPath = $candidate
        break
      }

      $parent = Split-Path $dir -Parent
      if ($parent -eq $dir) {
        break
      }

      $dir = $parent
    }
  }

  $targetsPath = Join-Path $PSScriptRoot "GetConfigurations.targets"
  $msbuildOutput = dotnet msbuild $projectFullPath /nologo /v:q /t:GetConfigurations /p:CustomAfterMicrosoftCommonTargets="$targetsPath"
  $configValue = $null
  foreach ($line in @($msbuildOutput)) {
    if ($line -match '^\s*CONFIGURATIONS=(.+)$') {
      $configValue = $Matches[1].Trim()
      break
    }
  }

  if (-not $configValue) {
    $configValue = "Debug;Release"
  }

  $source = if ($hasProjectOverride) {
    $projectFullPath
  } elseif ($directoryPropsPath) {
    $directoryPropsPath
  } else {
    "MSBuild defaults"
  }

  Write-Host "Configurations for $ProjectPath (from $source): $configValue"

  $configValue -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

foreach ($project in $projects) {
  $configurations = Get-ProjectConfigurations -ProjectPath $project
  $releaseConfigurations = $configurations | Where-Object { $_ -like "Release*" }

  if (-not $releaseConfigurations -or $releaseConfigurations.Count -eq 0) {
    Write-Warning "No Release configurations found for $project. Skipping."
    continue
  }

  foreach ($configuration in $releaseConfigurations) {
    Write-Host "Building $project ($configuration)"
    dotnet build $project -c "$configuration"
  }
}
