#Requires -Version 5.1
param(
    [switch] $SkipStart
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PlainTextFromSecureString {
    param([Security.SecureString] $Secure)

    if (-not $Secure) {
        return ''
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Get-ExistingSecretValue {
    param([string] $SecretKey)

    $output = & dotnet user-secrets list --project "Aspire/AppHost/AppHost.csproj" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read AppHost user-secrets. Ensure .NET SDK is installed."
    }

    foreach ($line in $output) {
        if ($line -match "^$([Regex]::Escape($SecretKey))\s*=\s*(.*)$") {
            return $Matches[1]
        }
    }

    return $null
}

function Set-SecretValue {
    param(
        [string] $Key,
        [string] $Value
    )

    & dotnet user-secrets set $Key $Value --project "Aspire/AppHost/AppHost.csproj" 1>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set secret '$Key'."
    }
}

function Remove-SecretValue {
    param([string] $Key)

    & dotnet user-secrets remove $Key --project "Aspire/AppHost/AppHost.csproj" 1>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to remove secret '$Key'."
    }
}

function Migrate-LegacyRabbitMqSecret {
    $canonicalKey = "Parameters:res08-rabbitmq-password"
    $legacyKey = "Parameters:rabbitmq-password"

    $canonicalValue = Get-ExistingSecretValue -SecretKey $canonicalKey
    if (-not [string]::IsNullOrWhiteSpace($canonicalValue)) {
        return
    }

    $legacyValue = Get-ExistingSecretValue -SecretKey $legacyKey
    if ([string]::IsNullOrWhiteSpace($legacyValue)) {
        return
    }

    Write-Host "Migrating legacy RabbitMQ secret '$legacyKey' -> '$canonicalKey'..."
    Set-SecretValue -Key $canonicalKey -Value $legacyValue
    Remove-SecretValue -Key $legacyKey
    Write-Host "Legacy RabbitMQ secret migrated."
}

function Test-SqlPasswordValid {
    param([string]$Password)
    if ($Password.Length -lt 8) { return $false }
    if ($Password -notmatch '[A-Z]') { return $false }
    if ($Password -notmatch '[a-z]') { return $false }
    if ($Password -notmatch '[0-9]') { return $false }
    if ($Password -notmatch '[^A-Za-z0-9]') { return $false }
    return $true
}

function Ensure-Secret {
    param(
        [string] $Key,
        [string] $Prompt,
        [switch] $MaskInput,
        [switch] $ValidateSqlPassword
    )

    $existing = Get-ExistingSecretValue -SecretKey $Key
    if (-not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Host "Secret '$Key' is already configured."
        return
    }

    Write-Host "Secret '$Key' is missing."

    while ($true) {
        if ($MaskInput) {
            $secureValue = Read-Host -Prompt $Prompt -AsSecureString
            $value = Get-PlainTextFromSecureString -Secure $secureValue
        } else {
            $value = Read-Host -Prompt $Prompt
        }

        if ([string]::IsNullOrWhiteSpace($value)) {
            Write-Host "Secret '$Key' cannot be empty."
            continue
        }

        if ($ValidateSqlPassword) {
            if (-not (Test-SqlPasswordValid $value)) {
                Write-Host ""
                Write-Host "ERROR: Password does not meet SQL Server requirements."
                Write-Host "Rules:"
                Write-Host "  - At least 8 characters"
                Write-Host "  - At least one uppercase letter (A-Z)"
                Write-Host "  - At least one lowercase letter (a-z)"
                Write-Host "  - At least one number (0-9)"
                Write-Host "  - At least one special character (!@# etc)"
                Write-Host ""
                continue
            }
        }
        break
    }

    Set-SecretValue -Key $Key -Value $value
    Write-Host "Secret '$Key' saved."
}

Write-Host "== VNM EasyRun =="
Write-Host "Checking required local secrets..."

Migrate-LegacyRabbitMqSecret

Ensure-Secret -Key "Parameters:sql-password" -Prompt "Enter SQL password" -MaskInput -ValidateSqlPassword
Ensure-Secret -Key "Parameters:res08-rabbitmq-password" -Prompt "Enter RabbitMQ password" -MaskInput

Write-Host "Running prerequisite checks..."
& powershell -NoProfile -NonInteractive -File ".\Setup\prereq-check.ps1" -RequireDocker -EnsureContainer "vnm-sqlserver,vnm-rabbitmq"
if ($LASTEXITCODE -ne 0) {
    throw "Prerequisite check failed."
}

if ($SkipStart) {
    Write-Host "EasyRun completed. AppHost start skipped by -SkipStart."
    exit 0
}

Write-Host "Starting AppHost..."
& dotnet run --project "Aspire/AppHost/AppHost.csproj"
