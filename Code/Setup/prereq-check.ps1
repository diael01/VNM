#Requires -Version 5.1
param(
    [switch] $RequireDocker,
    [string[]] $EnsureContainer
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
        [int] $TimeoutSeconds = 90,
        [int] $PollIntervalSeconds = 3
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

function Ensure-ContainerRunning {
    param([string] $Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return
    }

    $status = Get-ContainerStatus -Name $Name

    if (-not $status) {
        Write-Host "Container '$Name' does not exist yet. It will be created by Aspire if needed."
        return
    }

    switch ($status) {
        'running' {
            Write-Host "Container '$Name' is already running."
            return
        }
        'paused' {
            Write-Host "Unpausing container '$Name'..."
            & docker unpause $Name 1>$null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to unpause container '$Name'."
            }

            return
        }
        'restarting' {
            Write-Host "Container '$Name' is already restarting."
            return
        }
        default {
            Write-Host "Starting container '$Name'..."
            & docker start $Name 1>$null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to start container '$Name'."
            }

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

    foreach ($containerName in ($EnsureContainer | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        Ensure-ContainerRunning -Name $containerName
    }
}

Write-Host "Prerequisite check complete."
