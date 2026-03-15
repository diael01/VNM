#Requires -Version 5.1
param(
    [switch] $RequireDocker
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-CommandAvailable {
    param([string] $Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

if ($RequireDocker) {
    Write-Host "Checking Docker CLI..."
    Assert-CommandAvailable -Name 'docker'

    Write-Host "Checking Docker daemon..."
    docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker daemon is not reachable. Start Docker Desktop and retry."
    }

    Write-Host "Docker prerequisite check passed."
}

Write-Host "Prerequisite check complete."
