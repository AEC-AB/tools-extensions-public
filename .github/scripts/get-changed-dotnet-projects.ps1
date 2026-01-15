param(
  [string]$EventName = $env:GITHUB_EVENT_NAME,
  [string]$PullRequestBaseSha,
  [string]$PushBeforeSha,
  [string]$HeadSha = $env:GITHUB_SHA,
  [switch]$RebuildAll,
  [string]$OutputPath = $env:GITHUB_OUTPUT
)

$allProjects = git ls-files "**/*.csproj" | Sort-Object -Unique
$rebuildAll = [bool]$RebuildAll
$changedFiles = @()

if (-not $HeadSha) {
  $HeadSha = "HEAD"
}

if (-not $rebuildAll) {
  if ($EventName -eq "workflow_dispatch") {
    $rebuildAll = $true
  } elseif ($EventName -eq "pull_request") {
    $baseSha = $PullRequestBaseSha
    $headSha = $HeadSha
  } else {
    $baseSha = $PushBeforeSha
    $headSha = $HeadSha
  }

  if (-not $baseSha -or $baseSha -eq "0000000000000000000000000000000000000000") {
    $rebuildAll = $true
  } else {
    $changedFiles = git diff --name-only $baseSha $headSha
  }
}

if (-not $rebuildAll) {
  $normalizedFiles = $changedFiles | ForEach-Object { $_.Replace('\', '/') }
  if ($normalizedFiles | Where-Object { $_ -match '(^|/)Directory.Build.props$' }) {
    $rebuildAll = $true
  }
}

if ($rebuildAll) {
  $projectsToBuild = $allProjects
} else {
  $projectsToBuild = @()
  $normalizedFiles = $changedFiles | ForEach-Object { $_.Replace('\', '/') }
  foreach ($proj in $allProjects) {
    $projDir = (Split-Path $proj -Parent).Replace('\', '/')
    if ($normalizedFiles | Where-Object { $_.StartsWith("$projDir/") }) {
      $projectsToBuild += $proj
    }
  }
}

$projectsToBuild = @($projectsToBuild | Sort-Object -Unique)
if (-not $projectsToBuild) {
  $projectsToBuild = @()
}

$projectsJson = $projectsToBuild | ConvertTo-Json -Compress
$needsNet10 = [bool]($projectsToBuild | Where-Object { $_ -like "Assistant/*" })

if ($OutputPath) {
  "projects=$projectsJson" >> $OutputPath
  "needs_net10=$($needsNet10.ToString().ToLowerInvariant())" >> $OutputPath
}

Write-Host "Projects to build:"
if ($projectsToBuild.Count -eq 0) {
  Write-Host " - none"
} else {
  $projectsToBuild | ForEach-Object { Write-Host " - $_" }
}
