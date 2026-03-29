#Requires -Version 5.1
<#
.SYNOPSIS
    VNM environment setup and database initialization.

.DESCRIPTION
    Environment-aware setup script that can run from Aspire (local) or directly
    from CI/CD for non-Aspire environments.

    Modes:
      - local (default): uses Docker SQL container access via docker exec.
      - stg/prod: uses host SQL endpoint via local sqlcmd.

    Steps:
      1. Wait for SQL Server to accept connections.
      2. Drop (if exists) and recreate VNM from Database/VNM.sql.
      3. Drop (if exists) and recreate VNM_TEST from Database/VNM_TEST.sql.
            4. Seed both VNM and VNM_TEST via Database/Seed.sql.

.PARAMETER Mode
    Setup mode: local, stg, or prod. Default: local.

.PARAMETER ContainerName
    SQL Server Docker container name for local mode. Default: vnm-sqlserver.

.PARAMETER SaPassword
    SQL login password. Reads SA_PASSWORD env var first, then ConnectionStrings__VnmDb if available.

.PARAMETER SqlHost
    SQL host for stg/prod mode. Default: localhost.

.PARAMETER SqlPort
    SQL port for stg/prod mode. Default: 1433.

.PARAMETER SqlUser
    SQL login for stg/prod mode. Default: sa.

.PARAMETER TimeoutSecs
    Seconds to wait for SQL readiness. Default: 120.

.PARAMETER DatabaseDir
    Directory containing VNM.sql, VNM_TEST.sql and Seed.sql.

.PARAMETER SkipIfInitialized
    If set, setup exits successfully when VNM and VNM_TEST already exist with required core tables.
    Defaults to enabled when not explicitly set.

.PARAMETER ForceRecreate
    If true, bypasses SkipIfInitialized and forces drop/recreate + seed.
#>
param(
    [ValidateSet('local', 'stg', 'prod')]
    [string] $Mode = $(if ($env:SETUP_MODE) { $env:SETUP_MODE } else { 'local' }),

    [string] $ContainerName = $(if ($env:CONTAINER_NAME) { $env:CONTAINER_NAME } else { 'vnm-sqlserver' }),
    [string] $SaPassword = $(if ($env:SA_PASSWORD) { $env:SA_PASSWORD } else { '' }),

    [string] $SqlHost = $(if ($env:SQL_HOST) { $env:SQL_HOST } else { 'localhost' }),
    [int] $SqlPort = 1433,
    [string] $SqlUser = 'sa',

    [switch] $SkipIfInitialized,
    [switch] $ForceRecreate,

    [int] $TimeoutSecs = 120,
    [string] $DatabaseDir = (Join-Path $PSScriptRoot '..\Database')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$skipIfInitializedEffective = $true
if ($PSBoundParameters.ContainsKey('SkipIfInitialized')) {
    $skipIfInitializedEffective = $SkipIfInitialized.IsPresent
}
elseif (-not [string]::IsNullOrWhiteSpace($env:SKIP_IF_INITIALIZED)) {
    $parsedSkip = $false
    if ([bool]::TryParse($env:SKIP_IF_INITIALIZED, [ref]$parsedSkip)) {
        $skipIfInitializedEffective = $parsedSkip
    }
}

$script:SqlHostExplicit = $PSBoundParameters.ContainsKey('SqlHost')
$script:SqlPortExplicit = $PSBoundParameters.ContainsKey('SqlPort')
$script:SqlUserExplicit = $PSBoundParameters.ContainsKey('SqlUser')

function Apply-ConnectionStringDefaults {
    param([string] $ConnectionString)

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        return
    }

    $connectionBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString

    if ([string]::IsNullOrWhiteSpace($script:SaPassword)) {
        $script:SaPassword = $connectionBuilder.Password
    }

    if (-not $script:SqlUserExplicit -and -not [string]::IsNullOrWhiteSpace($connectionBuilder.UserID)) {
        $script:SqlUser = $connectionBuilder.UserID
    }

    $dataSource = $connectionBuilder.DataSource
    if ($dataSource.StartsWith('tcp:', [System.StringComparison]::OrdinalIgnoreCase)) {
        $dataSource = $dataSource.Substring(4)
    }

    if (-not [string]::IsNullOrWhiteSpace($dataSource)) {
        $hostPart = $dataSource
        $portPart = $null

        if ($dataSource.Contains(',')) {
            $parts = $dataSource.Split(',', 2)
            $hostPart = $parts[0]
            $portPart = $parts[1]
        }

        if (-not $script:SqlHostExplicit -and -not [string]::IsNullOrWhiteSpace($hostPart)) {
            $script:SqlHost = $hostPart
        }

        if (-not $script:SqlPortExplicit -and $portPart -match '^\d+$') {
            $script:SqlPort = [int] $portPart
        }
    }
}

Apply-ConnectionStringDefaults -ConnectionString $env:ConnectionStrings__VnmDb

if ([string]::IsNullOrWhiteSpace($SaPassword)) {
    throw 'SA password is missing. Set SA_PASSWORD, provide ConnectionStrings__VnmDb, or pass -SaPassword.'
}

$databaseRoot = (Resolve-Path $DatabaseDir).Path
$vnmScript = Join-Path $databaseRoot 'VNM.sql'
$vnmTestScript = Join-Path $databaseRoot 'VNM_TEST.sql'
$seedScript = Join-Path $databaseRoot 'Seed.sql'

foreach ($scriptPath in @($vnmScript, $vnmTestScript, $seedScript)) {
    if (-not (Test-Path $scriptPath)) {
        throw "Required SQL script not found: $scriptPath"
    }
}

# -- Helpers -------------------------------------------------------------------

$script:ResolvedDockerSqlCmd = $null
$script:ResolvedHostSqlCmd = $null

function Resolve-DockerSqlCmdPath {
    if ($script:ResolvedDockerSqlCmd) { return $script:ResolvedDockerSqlCmd }
    foreach ($path in '/opt/mssql-tools18/bin/sqlcmd', '/opt/mssql-tools/bin/sqlcmd') {
        docker exec $ContainerName test -f $path 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $script:ResolvedDockerSqlCmd = $path
            return $path
        }
    }
    throw "sqlcmd not found inside container '$ContainerName'."
}

function Resolve-HostSqlCmdPath {
    if ($script:ResolvedHostSqlCmd) { return $script:ResolvedHostSqlCmd }

    $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($null -eq $cmd) {
        throw 'sqlcmd not found on host PATH. Install SqlPackage/sqlcmd or run in local Docker mode.'
    }

    $script:ResolvedHostSqlCmd = $cmd.Source
    return $script:ResolvedHostSqlCmd
}

function Invoke-Sql {
    param(
        [string] $Query,
        [string] $Db = 'master'
    )

    if ($Mode -eq 'local') {
        $dockerSqlCmd = Resolve-DockerSqlCmdPath
        docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -d $Db -C -Q $Query
        if ($LASTEXITCODE -ne 0) { throw "Inline SQL command failed (exit $LASTEXITCODE)." }
        return
    }

    $hostSqlCmd = Resolve-HostSqlCmdPath
    & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -d $Db -C -Q $Query
    if ($LASTEXITCODE -ne 0) { throw "Inline SQL command failed (exit $LASTEXITCODE)." }
}

function Invoke-SqlFile {
    param(
        [string] $LocalPath,
        [string] $Db = 'master'
    )

    if ($Mode -eq 'local') {
        $dockerSqlCmd = Resolve-DockerSqlCmdPath
        $tmpPath = "/tmp/$(Split-Path $LocalPath -Leaf)"
        docker cp $LocalPath "${ContainerName}:${tmpPath}" | Out-Null
        docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -d $Db -C -i $tmpPath
        $exitCode = $LASTEXITCODE
        try {
            docker exec $ContainerName rm -f $tmpPath 2>$null | Out-Null
        } catch { }
        if ($exitCode -ne 0) { throw "SQL file failed (exit $exitCode): $LocalPath" }
        return
    }

    $hostSqlCmd = Resolve-HostSqlCmdPath
    & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -d $Db -C -i $LocalPath
    if ($LASTEXITCODE -ne 0) { throw "SQL file failed (exit $LASTEXITCODE): $LocalPath" }
}

function Drop-IfExists {
    param([string]$DbName)

    Invoke-Sql @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$DbName')
BEGIN
    ALTER DATABASE [$DbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DbName];
END
"@
}

function Test-DbAndTableExists {
    param(
        [string] $DbName,
        [string] $TableName
    )

    $dbExistsQuery = "IF DB_ID(N'$DbName') IS NULL SELECT 0 ELSE SELECT 1;"
    $tableExistsQuery = "IF OBJECT_ID(N'dbo.$TableName', N'U') IS NULL SELECT 0 ELSE SELECT 1;"

    function Get-BitResult {
        param([object] $SqlOutput)

        $candidate = $SqlOutput |
            ForEach-Object { $_.ToString().Trim() } |
            Where-Object { $_ -match '^[01]$' } |
            Select-Object -Last 1

        if ($null -eq $candidate) { return $null }
        return $candidate
    }

    if ($Mode -eq 'local') {
        $dockerSqlCmd = Resolve-DockerSqlCmdPath
        $dbResult = docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -d master -h -1 -W -C -Q $dbExistsQuery 2>$null
        if ($LASTEXITCODE -ne 0) { return $false }
        if ((Get-BitResult -SqlOutput $dbResult) -ne '1') { return $false }

        $tableResult = docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -d $DbName -h -1 -W -C -Q $tableExistsQuery 2>$null
        if ($LASTEXITCODE -ne 0) { return $false }
        return ((Get-BitResult -SqlOutput $tableResult) -eq '1')
    }

    $hostSqlCmd = Resolve-HostSqlCmdPath
    $dbResult = & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -d master -h -1 -W -C -Q $dbExistsQuery 2>$null
    if ($LASTEXITCODE -ne 0) { return $false }
    if ((Get-BitResult -SqlOutput $dbResult) -ne '1') { return $false }

    $tableResult = & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -d $DbName -h -1 -W -C -Q $tableExistsQuery 2>$null
    if ($LASTEXITCODE -ne 0) { return $false }
    return ((Get-BitResult -SqlOutput $tableResult) -eq '1')
}

function Test-AlreadyInitialized {
    $vnmReady = Test-DbAndTableExists -DbName 'VNM' -TableName 'InverterReadings'
    $vnmTestReady = Test-DbAndTableExists -DbName 'VNM_TEST' -TableName 'InverterReadings'
    return ($vnmReady -and $vnmTestReady)
}

function Wait-ForSql {
    Write-Host "`n[1/4] Waiting for SQL Server (mode: $Mode)..."

    $deadline = (Get-Date).AddSeconds($TimeoutSecs)
    $ready = $false

    while (-not $ready) {
        try {
            if ($Mode -eq 'local') {
                $dockerSqlCmd = Resolve-DockerSqlCmdPath
                docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -C -Q 'SELECT 1' 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) { $ready = $true }
            } else {
                $hostSqlCmd = Resolve-HostSqlCmdPath
                & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -C -Q 'SELECT 1' 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) { $ready = $true }
            }
        } catch { }

        if (-not $ready) {
            if ((Get-Date) -gt $deadline) {
                throw "SQL Server was not ready after ${TimeoutSecs} seconds."
            }
            Write-Host '      not ready yet, retrying in 3 s...'
            Start-Sleep 3
        }
    }

    Write-Host '      ready.'
}

# -- Pre-flight ----------------------------------------------------------------

if ($Mode -eq 'local' -and -not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI not found in PATH. Install Docker Desktop and try again.'
}

# -- Setup ---------------------------------------------------------------------

Wait-ForSql

if (-not $ForceRecreate -and $skipIfInitializedEffective -and (Test-AlreadyInitialized)) {
    Write-Host "`nSetup skipped. VNM and VNM_TEST are already initialized."
    Write-Host "Use -ForceRecreate to run full drop/recreate + seed."
    exit 0
}



# [2/4] Drop and recreate VNM using VNM.sql, then mark migration as applied
Write-Host "`n[2/4] Checking if VNM database exists..."

# Improved DB existence check: robustly parse output for '1' or '0' only
$vnmExists = $false
$dbExistsQuery = "IF DB_ID(N'VNM') IS NULL SELECT 0 ELSE SELECT 1;"
function Parse-DbExistsResult {
    param([string[]]$lines)
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '1') { return $true }
        if ($trimmed -eq '0') { return $false }
    }
    return $false
}
try {
    if ($Mode -eq 'local') {
        $dockerSqlCmd = Resolve-DockerSqlCmdPath
        $result = docker exec $ContainerName $dockerSqlCmd -S localhost -U sa -P $SaPassword -d master -h -1 -W -C -Q $dbExistsQuery 2>$null
        $vnmExists = Parse-DbExistsResult $result
    } else {
        $hostSqlCmd = Resolve-HostSqlCmdPath
        $result = & $hostSqlCmd -S "$SqlHost,$SqlPort" -U $SqlUser -P $SaPassword -d master -h -1 -W -C -Q $dbExistsQuery 2>$null
        $vnmExists = Parse-DbExistsResult $result
    }
} catch { $vnmExists = $false }

if (-not $vnmExists) {
    Write-Host "VNM database does not exist. Creating using VNM.sql..."
    Invoke-SqlFile -LocalPath $vnmScript
    Write-Host '      done.'

    # Scaffold models and context from the new DB
    Push-Location "$PSScriptRoot/../BackEnd/Libs/Repositories"
    $connStr = "Server=$SqlHost,$SqlPort;Database=VNM;User Id=$SqlUser;Password=$SaPassword;TrustServerCertificate=True;"
    Write-Host "Scaffolding models and context from VNM database..."
    dotnet ef dbcontext scaffold "$connStr" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context VnmDbContext --force --no-onconfiguring
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core scaffold failed. See output above."
    }

    # Remove existing migrations if any
    if (Test-Path ./Migrations) {
        Remove-Item ./Migrations/* -Force -Recurse
    }

    # Create initial migration
    $migrationName = "InitialCreate"
    Write-Host "Creating InitialCreate migration from scaffolded code..."
    dotnet ef migrations add $migrationName --project Repositories.csproj --startup-project Repositories.csproj
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core migration creation failed. See output above."
    }


    $migrationFile = Get-ChildItem ./Migrations/*.cs | Select-Object -First 1
    $migrationId = if ($migrationFile) { [System.IO.Path]::GetFileNameWithoutExtension($migrationFile.Name) } else { "" }
    if ($migrationId) {
        Write-Host "Ensuring __EFMigrationsHistory table exists..."
        $createHistoryTable = @"
IF OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
"@
        Invoke-Sql -Query $createHistoryTable -Db 'VNM'

        Write-Host "Marking migration '$migrationId' as applied in __EFMigrationsHistory..."
        $insertHistory = "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('$migrationId', '$(dotnet --version)');"
        Invoke-Sql -Query $insertHistory -Db 'VNM'
        Write-Host '      done.'
    } else {
        Write-Host "Could not determine migration ID to mark as applied."
    }
    Pop-Location
} else {
    Write-Host "VNM database already exists. Skipping creation and migration marking."
}

Write-Host "`n[3/4] Dropping and recreating VNM_TEST database..."
Drop-IfExists 'VNM_TEST'
Invoke-SqlFile -LocalPath $vnmTestScript
Write-Host '      done.'

Write-Host "`n[4/4] Seeding reference data into VNM and VNM_TEST (Seed.sql is idempotent)..."
Invoke-SqlFile -LocalPath $seedScript -Db 'VNM'
Invoke-SqlFile -LocalPath $seedScript -Db 'VNM_TEST'
Write-Host '      done.'

Write-Host "`nSetup complete. VNM and VNM_TEST databases are ready."
