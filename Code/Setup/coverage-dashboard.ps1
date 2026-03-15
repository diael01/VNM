#Requires -Version 5.1
param(
    [switch] $SkipBackend,
    [switch] $SkipUi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$coverageRoot = Join-Path $repoRoot 'TestCoverage'
$legacyCoverageRoot = Join-Path $repoRoot 'coverage'
$backendResultsRoot = Join-Path $coverageRoot 'backend\results'
$backendReportRoot = Join-Path $coverageRoot 'backend\report'
$uiRoot = Join-Path $repoRoot 'ReactUI'

function Ensure-DotnetTool {
    param(
        [string] $PackageId,
        [string] $CommandName,
        [string] $Version = ''
    )

    Push-Location $repoRoot
    try {
        if (-not (Test-Path (Join-Path $repoRoot '.config\dotnet-tools.json'))) {
            dotnet new tool-manifest | Out-Null
        }

        $installArgs = @('tool', 'install', $PackageId)
        if (-not [string]::IsNullOrWhiteSpace($Version)) {
            $installArgs += @('--version', $Version)
        }

        dotnet @installArgs 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $updateArgs = @('tool', 'update', $PackageId)
            if (-not [string]::IsNullOrWhiteSpace($Version)) {
                $updateArgs += @('--version', $Version)
            }

            dotnet @updateArgs | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to install/update dotnet tool '$PackageId'."
            }
        }

        dotnet tool restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw 'dotnet tool restore failed.'
        }

        return $CommandName
    }
    finally {
        Pop-Location
    }
}

Write-Host 'Preparing coverage output folder...'
if (Test-Path $coverageRoot) {
    Remove-Item $coverageRoot -Recurse -Force
}

if ((Test-Path $legacyCoverageRoot) -and ($legacyCoverageRoot -ne $coverageRoot)) {
  Remove-Item $legacyCoverageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $coverageRoot | Out-Null

if (-not $SkipBackend) {
    Write-Host 'Running backend tests with coverage...'

    $backendProjects = @(
        (Join-Path $repoRoot 'BackEnd\Tests\Unit\Repositories\RepositoriesUnit.csproj'),
        (Join-Path $repoRoot 'BackEnd\Tests\Unit\InverterPolling\InverterPollingUnit.csproj'),
        (Join-Path $repoRoot 'BackEnd\Tests\Unit\EventBusHarness\EventBusHarnessUnit.csproj')
    )

    $backendFailures = @()

    foreach ($project in $backendProjects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $projectResults = Join-Path $backendResultsRoot $projectName
        New-Item -ItemType Directory -Path $projectResults -Force | Out-Null

        Write-Host "  -> $projectName"
        dotnet test $project --collect:'XPlat Code Coverage' --results-directory $projectResults --logger "trx;LogFileName=$projectName.trx"
        if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet test failed for $projectName. Coverage (if produced) will still be included."
        $backendFailures += $projectName
        }
    }

    $coverageReports = Get-ChildItem -Path $backendResultsRoot -Recurse -Filter 'coverage.cobertura.xml' |
        ForEach-Object { $_.FullName }

    if ($coverageReports -and $coverageReports.Count -gt 0) {
      Write-Host 'Generating backend HTML coverage report...'
      $reportGeneratorCommand = Ensure-DotnetTool -PackageId 'dotnet-reportgenerator-globaltool' -CommandName 'reportgenerator' -Version '5.*'

      $reportArgs = @(
        'tool', 'run', $reportGeneratorCommand,
        "-reports:$($coverageReports -join ';')",
        "-targetdir:$backendReportRoot",
        '-reporttypes:Html;TextSummary'
      )

      Push-Location $repoRoot
      try {
        dotnet @reportArgs
        if ($LASTEXITCODE -ne 0) {
          throw 'ReportGenerator failed for backend coverage.'
        }
      }
      finally {
        Pop-Location
        }
    }

    if ($backendFailures.Count -gt 0) {
      Write-Warning "Backend test failures: $($backendFailures -join ', ')"
    }

    if (-not $coverageReports -or $coverageReports.Count -eq 0) {
      Write-Warning 'No backend cobertura coverage files were produced.'
    }
}

if (-not $SkipUi) {
    Write-Host 'Running UI tests with coverage...'
    Push-Location $uiRoot
    try {
        npm run test -- --run --coverage
        if ($LASTEXITCODE -ne 0) {
            throw 'UI tests with coverage failed.'
        }
    }
    finally {
        Pop-Location
    }
}

$backendIndex = Join-Path $backendReportRoot 'index.html'
$uiIndex = Join-Path $coverageRoot 'ui\index.html'
$summaryIndex = Join-Path $coverageRoot 'index.html'

$backendStatus = if (Test-Path $backendIndex) { 'Ready' } else { 'Not generated' }
$uiStatus = if (Test-Path $uiIndex) { 'Ready' } else { 'Not generated' }

$summaryHtml = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>VNM TestCoverage Dashboard</title>
  <style>
    :root {
      --bg: #f4f7fb;
      --card: #ffffff;
      --ink: #13213a;
      --muted: #4f5d75;
      --accent: #0a7f8c;
      --accent-2: #ef6f6c;
      --ok: #2a9d8f;
      --bad: #9aa8bd;
    }
    body {
      margin: 0;
      font-family: "Segoe UI", "Trebuchet MS", sans-serif;
      color: var(--ink);
      background: radial-gradient(circle at top right, #d9eef2 0%, var(--bg) 45%), var(--bg);
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 24px;
    }
    .wrap {
      width: min(900px, 100%);
      background: var(--card);
      border-radius: 16px;
      padding: 28px;
      box-shadow: 0 15px 50px rgba(10, 38, 59, 0.14);
    }
    h1 {
      margin-top: 0;
      margin-bottom: 8px;
      letter-spacing: 0.2px;
    }
    p {
      margin-top: 0;
      color: var(--muted);
    }
    .grid {
      display: grid;
      gap: 14px;
      margin-top: 20px;
    }
    .card {
      border: 1px solid #dbe4f0;
      border-radius: 12px;
      padding: 16px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
    }
    .title {
      font-weight: 600;
      font-size: 18px;
    }
    .status {
      font-size: 13px;
      padding: 4px 10px;
      border-radius: 999px;
      background: #e9f6f4;
      color: var(--ok);
      white-space: nowrap;
    }
    .status.off {
      background: #eef2f7;
      color: var(--bad);
    }
    a.button {
      text-decoration: none;
      background: linear-gradient(120deg, var(--accent), #0e8ea0);
      color: white;
      padding: 10px 14px;
      border-radius: 10px;
      font-weight: 600;
      display: inline-block;
    }
    .footer {
      margin-top: 22px;
      font-size: 13px;
      color: var(--muted);
    }
  </style>
</head>
<body>
  <main class="wrap">
    <h1>VNM TestCoverage Dashboard</h1>
    <p>Generated by <strong>Setup/coverage-dashboard.ps1</strong>.</p>

    <section class="grid">
      <article class="card">
        <div>
          <div class="title">Backend Coverage</div>
          <div>ReportGenerator HTML output from .NET test projects.</div>
        </div>
        <div>
          <div class="status $(if ($backendStatus -eq 'Ready') { '' } else { 'off' })">$backendStatus</div>
          $(if (Test-Path $backendIndex) { '<div style="margin-top:8px;"><a class="button" href="backend/report/index.html">Open</a></div>' } else { '' })
        </div>
      </article>

      <article class="card">
        <div>
          <div class="title">UI Coverage</div>
          <div>Vitest coverage report for the UI workspace.</div>
        </div>
        <div>
          <div class="status $(if ($uiStatus -eq 'Ready') { '' } else { 'off' })">$uiStatus</div>
          $(if (Test-Path $uiIndex) { '<div style="margin-top:8px;"><a class="button" href="ui/index.html">Open</a></div>' } else { '' })
        </div>
      </article>
    </section>

    <div class="footer">Location: $summaryIndex</div>
  </main>
</body>
</html>
"@

Set-Content -Path $summaryIndex -Value $summaryHtml -Encoding UTF8

$summaryUri = [System.Uri]::new($summaryIndex).AbsoluteUri
Write-Host "TestCoverage dashboard generated: $summaryIndex"
Write-Host "Open dashboard: $summaryUri"

Start-Process -FilePath $summaryIndex | Out-Null
