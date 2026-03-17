#Requires -Version 5.1
param(
    [switch] $RequireDocker,
    [string[]] $EnsureContainer,
    [string] $SqlContainerImage = 'mcr.microsoft.com/mssql/server:2022-latest',
    [string] $RabbitMqContainerImage = 'rabbitmq:3-management',
    [string] $SqlSaPassword,
    [string] $RabbitMqPassword
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-CommandAvailable {
    param([string] $Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Test-DockerDaemonReachable {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & docker info 1>$null 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

function Get-DockerDesktopExecutablePath {
    $candidatePaths = @()

    if ($env:ProgramFiles) {
        $candidatePaths += (Join-Path $env:ProgramFiles 'Docker\\Docker\\Docker Desktop.exe')
    }

    if (${env:ProgramFiles(x86)}) {
        $candidatePaths += (Join-Path ${env:ProgramFiles(x86)} 'Docker\\Docker\\Docker Desktop.exe')
    }

    if ($env:LocalAppData) {
        $candidatePaths += (Join-Path $env:LocalAppData 'Programs\\Docker\\Docker\\Docker Desktop.exe')
    }

    foreach ($path in $candidatePaths) {
        if ($path -and (Test-Path -Path $path)) {
            return $path
        }
    }

    return $null
}

function Start-DockerDesktop {
    $dockerDesktopPath = Get-DockerDesktopExecutablePath
    if (-not $dockerDesktopPath) {
        throw "Docker CLI is available but Docker Desktop executable could not be found. Start Docker manually and retry."
    }

    Write-Host "Starting Docker Desktop..."
    Start-Process -FilePath $dockerDesktopPath | Out-Null
}

function Wait-ForDockerDaemon {
    param(
        [int] $TimeoutSeconds = 180,
        [int] $PollIntervalSeconds = 4
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (Test-DockerDaemonReachable) {
            return $true
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    }
    while ((Get-Date) -lt $deadline)

    return $false
}

function Invoke-DockerWithRetry {
    param(
        [string[]] $Arguments,
        [string] $Operation,
        [int] $MaxAttempts = 3,
        [int] $DelaySeconds = 3
    )

    if (-not $Arguments -or $Arguments.Count -eq 0) {
        throw "Docker operation '$Operation' was called without arguments."
    }

    $previousNativeErrorPreference = $null
    $hasNativePreference = $false
    if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
        $hasNativePreference = $true
        $previousNativeErrorPreference = $global:PSNativeCommandUseErrorActionPreference
        $global:PSNativeCommandUseErrorActionPreference = $false
    }

    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try {
        $lastOutput = $null
        for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
            $result = & docker @Arguments 2>&1
            if ($LASTEXITCODE -eq 0) {
                return $result
            }

            $lastOutput = ($result | Out-String).Trim()
            if ($attempt -lt $MaxAttempts) {
                Start-Sleep -Seconds $DelaySeconds
            }
        }

        if ([string]::IsNullOrWhiteSpace($lastOutput)) {
            throw "Docker operation '$Operation' failed after $MaxAttempts attempts."
        }

        throw "Docker operation '$Operation' failed after $MaxAttempts attempts. Last output: $lastOutput"
    }
    finally {
        $ErrorActionPreference = $previousErrorAction

        if ($hasNativePreference) {
            $global:PSNativeCommandUseErrorActionPreference = $previousNativeErrorPreference
        }
    }
}

function Get-ContainerStatus {
    param([string] $Name)

    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $status = & docker container inspect --format "{{.State.Status}}" $Name 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        return ($status | Select-Object -First 1)
    }
    catch {
        return $null
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

function Get-ContainerHostPort {
    param(
        [string] $Name,
        [string] $ContainerPort
    )

    if ([string]::IsNullOrWhiteSpace($Name) -or [string]::IsNullOrWhiteSpace($ContainerPort)) {
        return $null
    }

    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $mapping = & docker port $Name $ContainerPort 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $mapping) {
            return $null
        }

        $firstMapping = ($mapping | Select-Object -First 1).ToString().Trim()
        if ([string]::IsNullOrWhiteSpace($firstMapping)) {
            return $null
        }

        if ($firstMapping.Contains(':')) {
            return ($firstMapping.Split(':') | Select-Object -Last 1)
        }

        return $firstMapping
    }
    catch {
        return $null
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

function Write-ContainerAccessHint {
    param([string] $Name)

    switch ($Name) {
        'vnm-rabbitmq' {
            $mgmtHostPort = Get-ContainerHostPort -Name $Name -ContainerPort '15672/tcp'
            if ([string]::IsNullOrWhiteSpace($mgmtHostPort)) {
                Write-Host "RabbitMQ management UI port is not published for container '$Name'."
                return
            }

            Write-Host "RabbitMQ management UI: http://localhost:$mgmtHostPort"

            if ($mgmtHostPort -ne '15672') {
                Write-Host "Note: '$Name' is not using fixed host port 15672. To make the URL stable, recreate it with fixed port mapping (docker rm -f vnm-rabbitmq) and rerun prereq-check."
            }

            return
        }
        default {
            return
        }
    }
}

function Get-RabbitMqPassword {
    $rabbitPassword = $RabbitMqPassword
    if ([string]::IsNullOrWhiteSpace($rabbitPassword)) {
        $rabbitPassword = $env:APPHOST_RABBITMQ_PASSWORD
    }

    if ([string]::IsNullOrWhiteSpace($rabbitPassword)) {
        $rabbitPassword = $env:RABBITMQ_DEFAULT_PASS
    }

    if ([string]::IsNullOrWhiteSpace($rabbitPassword)) {
        Write-Host "[vnm-rabbitmq] RabbitMqPassword and env variables are empty. Attempting to read from .NET user-secrets..."
        $appHostPath = Join-Path -Path $PSScriptRoot -ChildPath '../Aspire/AppHost'
        if (Test-Path "$appHostPath/AppHost.csproj") {
            $secretsOutput = dotnet user-secrets list --project "$appHostPath/AppHost.csproj" 2>$null
            if ($secretsOutput) {
                $rabbitSecret = $secretsOutput | Select-String -Pattern 'Parameters:res08-rabbitmq-password' | ForEach-Object {
                    $line = $_.ToString()
                    # Do not log secret values
                    if ($line -match 'Parameters:res08-rabbitmq-password\s*=\s*(.+)') { Write-Host "[vnm-rabbitmq] Found Parameters:res08-rabbitmq-password secret."; $matches[1] }
                } | Select-Object -First 1
                if ($rabbitSecret) {
                    Write-Host "[vnm-rabbitmq] Using secret value for RabbitMQ password."
                    $rabbitPassword = $rabbitSecret
                    $env:APPHOST_RABBITMQ_PASSWORD = $rabbitPassword
                    $env:RABBITMQ_DEFAULT_PASS = $rabbitPassword
                } else {
                    Write-Host "[vnm-rabbitmq] No RabbitMQ password found in user-secrets."
                }
            } else {
                Write-Host "[vnm-rabbitmq] No output from user-secrets list."
            }
        } else {
            Write-Host "[vnm-rabbitmq] AppHost.csproj not found at $appHostPath."
        }
    }

    return $rabbitPassword
}

function Sync-RabbitMqGuestPassword {
    param([string] $ContainerName)

    $rabbitPassword = Get-RabbitMqPassword
    if ([string]::IsNullOrWhiteSpace($rabbitPassword)) {
        return
    }

    $changeArgs = @(
        'exec',
        $ContainerName,
        'rabbitmqctl',
        'change_password',
        'guest',
        $rabbitPassword
    )

    Invoke-DockerWithRetry -Arguments $changeArgs -Operation "synchronize RabbitMQ credentials for container '$ContainerName'" -MaxAttempts 15 -DelaySeconds 2 | Out-Null
    Write-Host "RabbitMQ credentials synchronized for container '$ContainerName'."
}

function New-ContainerIfMissing {
          
    param([string] $Name)

    switch ($Name) {
        'vnm-sqlserver' {
            # Always initialize sqlPort to default at the start
            $sqlPort = 14333
                Write-Host "[vnm-sqlserver] Checking for SQL password..."
                $sqlPassword = $SqlSaPassword
                if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
                    Write-Host "[vnm-sqlserver] SqlSaPassword parameter is empty. Checking APPHOST_SQL_PASSWORD env..."
                    $sqlPassword = $env:APPHOST_SQL_PASSWORD
                }

                if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
                    Write-Host "[vnm-sqlserver] APPHOST_SQL_PASSWORD env is empty. Checking SA_PASSWORD env..."
                    $sqlPassword = $env:SA_PASSWORD
                }

                if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
                    Write-Host "[vnm-sqlserver] SA_PASSWORD env is empty. Attempting to read from .NET user-secrets..."
                    $appHostPath = Join-Path -Path $PSScriptRoot -ChildPath '../Aspire/AppHost'
                    if (Test-Path "$appHostPath/AppHost.csproj") {
                        Write-Host "[vnm-sqlserver] AppHost.csproj found at $appHostPath. Running 'dotnet user-secrets list'..."
                        $secretsOutput = dotnet user-secrets list --project "$appHostPath/AppHost.csproj" 2>$null
                        if ($secretsOutput) {
                            Write-Host "[vnm-sqlserver] Checked user-secrets for SQL password."
                            $sqlSecret = $secretsOutput | Select-String -Pattern 'APPHOST_SQL_PASSWORD|SA_PASSWORD|Parameters:sql-password' | ForEach-Object {
                                $line = $_.ToString()
                                # Do not log secret values
                                if ($line -match 'APPHOST_SQL_PASSWORD\s*:\s*(.+)') { Write-Host "[vnm-sqlserver] Found APPHOST_SQL_PASSWORD secret."; $matches[1] }
                                elseif ($line -match 'SA_PASSWORD\s*:\s*(.+)') { Write-Host "[vnm-sqlserver] Found SA_PASSWORD secret."; $matches[1] }
                                elseif ($line -match 'Parameters:sql-password\s*=\s*(.+)') { Write-Host "[vnm-sqlserver] Found Parameters:sql-password secret."; $matches[1] }
                            } | Select-Object -First 1
                            if ($sqlSecret) {
                                Write-Host "[vnm-sqlserver] Using secret value for SQL password."
                                $sqlPassword = $sqlSecret
                                $env:APPHOST_SQL_PASSWORD = $sqlPassword
                                $env:SA_PASSWORD = $sqlPassword
                            } else {
                                Write-Host "[vnm-sqlserver] No SQL password found in user-secrets."
                            }
                        } else {
                            Write-Host "[vnm-sqlserver] No output from user-secrets list."
                        }
                    } else {
                        Write-Host "[vnm-sqlserver] AppHost.csproj not found at $appHostPath."
                    }
                    if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
                        Write-Host "[vnm-sqlserver] SQL password still empty after all checks. Throwing error."
                        throw "Container '$Name' is missing and cannot be created because no SQL SA password was provided. Pass -SqlSaPassword or set APPHOST_SQL_PASSWORD/SA_PASSWORD."
                    }
                }

            Write-Host "Creating SQL container '$Name' from image '$SqlContainerImage'..."
            # Always initialize sqlPort to default
            $sqlPort = 14333
            # Read fixed port from Aspire/AppHost/appsettings.json
            $appSettingsPath = Join-Path $PSScriptRoot '../Aspire/AppHost/appsettings.json'
            Write-Host "[vnm-sqlserver] Resolved appsettings.json path: $appSettingsPath"
            if (Test-Path $appSettingsPath) {
                try {
                    $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
                    if ($appSettings.SqlPort -and $appSettings.SqlPort -ne 0 -and $appSettings.SqlPort -ne "") {
                        $sqlPort = $appSettings.SqlPort
                        Write-Host "[vnm-sqlserver] Using SQL port from appsettings.json: $sqlPort"
                    } else {
                        Write-Host "[vnm-sqlserver] SqlPort not found or invalid in appsettings.json, using default: $sqlPort"
                    }
                } catch {
                    Write-Host "[vnm-sqlserver] Failed to read appsettings.json, using default port: $sqlPort"
                }
            } else {
                Write-Host "[vnm-sqlserver] appsettings.json not found, using default port: $sqlPort"
            }
            # Ensure SQL port is set and valid BEFORE constructing docker command
            if (-not $sqlPort -or $sqlPort -eq 0 -or $sqlPort -eq "") {
                Write-Host "[vnm-sqlserver] WARNING: SQL port is missing or invalid. Defaulting to 14333."
                $sqlPort = 14333
            }
            Write-Host "[vnm-sqlserver] Final SQL port value: $sqlPort"
            Write-Host "[vnm-sqlserver] Value of $sqlPort before port mapping: $sqlPort"
            $maskedPwd = '****'
            $portMapping = "${sqlPort}:1433"
            Write-Host "[vnm-sqlserver] Port mapping: $portMapping"
            $dockerCmd = "docker run -d --name $Name --restart unless-stopped -p $portMapping -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD=$maskedPwd $SqlContainerImage"
            Write-Host "[vnm-sqlserver] Docker command: $dockerCmd"
            $sqlRunArgs = @(
                'run',
                '-d',
                '--name', $Name,
                '--restart', 'unless-stopped',
                '-p', $portMapping,
                '-e', 'ACCEPT_EULA=Y',
                '-e', 'MSSQL_PID=Developer',
                '-e', "MSSQL_SA_PASSWORD=$sqlPassword",
                $SqlContainerImage
            )

            Invoke-DockerWithRetry -Arguments $sqlRunArgs -Operation "create SQL container '$Name'" -MaxAttempts 4 -DelaySeconds 4 | Out-Null

            return $true
        }
        'vnm-rabbitmq' {
            $rabbitPassword = Get-RabbitMqPassword
            if ([string]::IsNullOrWhiteSpace($rabbitPassword)) {
                throw "Container '$Name' is missing and cannot be created because no RabbitMQ password was provided. Pass -RabbitMqPassword or set APPHOST_RABBITMQ_PASSWORD/RABBITMQ_DEFAULT_PASS."
            }

            Write-Host "Creating RabbitMQ container '$Name' from image '$RabbitMqContainerImage'..."
            $rabbitRunArgs = @(
                'run',
                '-d',
                '--name', $Name,
                '--restart', 'unless-stopped',
                '-p', '5672:5672',
                '-p', '15672:15672',
                '-e', 'RABBITMQ_DEFAULT_USER=guest',
                '-e', "RABBITMQ_DEFAULT_PASS=$rabbitPassword",
                $RabbitMqContainerImage
            )

            Invoke-DockerWithRetry -Arguments $rabbitRunArgs -Operation "create RabbitMQ container '$Name'" -MaxAttempts 4 -DelaySeconds 4 | Out-Null

            Sync-RabbitMqGuestPassword -ContainerName $Name

            return $true
        }
        default {
            Write-Host "Container '$Name' does not exist and has no prereq auto-create rule."
            return $false
        }
    }
}

function Ensure-ContainerRunning {
    param([string] $Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return
    }

    $status = Get-ContainerStatus -Name $Name

    if (-not $status) {
        if (-not (New-ContainerIfMissing -Name $Name)) {
            return
        }

        Write-Host "Container '$Name' created successfully."
        return
    }

    switch ($status) {
        'running' {
            Write-Host "Container '$Name' is already running."
            return
        }
        'paused' {
            Write-Host "Unpausing container '$Name'..."
            Invoke-DockerWithRetry -Arguments @('unpause', $Name) -Operation "unpause container '$Name'" | Out-Null

            return
        }
        'restarting' {
            Write-Host "Container '$Name' is already restarting."
            return
        }
        default {
            Write-Host "Starting container '$Name'..."
            Invoke-DockerWithRetry -Arguments @('start', $Name) -Operation "start container '$Name'" | Out-Null

            return
        }
    }
}

if ($RequireDocker) {
    Write-Host "Checking Docker CLI..."
    Assert-CommandAvailable -Name 'docker'

    Write-Host "Checking Docker daemon..."
    if (-not (Test-DockerDaemonReachable)) {
        Write-Host "Docker daemon is not reachable. Attempting to start Docker Desktop..."
        Start-DockerDesktop

        if (-not (Wait-ForDockerDaemon)) {
            throw "Docker daemon is not reachable after auto-start attempt. Start Docker Desktop manually and retry."
        }
    }

    Write-Host "Docker prerequisite check passed."

    $containersToEnsure = $EnsureContainer |
        ForEach-Object { ($_ -split ',') } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique

    foreach ($containerName in $containersToEnsure) {
        Ensure-ContainerRunning -Name $containerName

        if ($containerName -eq 'vnm-rabbitmq') {
            Sync-RabbitMqGuestPassword -ContainerName $containerName
        }

        Write-ContainerAccessHint -Name $containerName
    }
}

Write-Host "Prerequisite check complete."
