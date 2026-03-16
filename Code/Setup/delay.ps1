param([int]$Seconds)
if (-not $Seconds) {
    Write-Host "Usage: delay.ps1 <seconds>"
    exit 1
}
Start-Sleep -Seconds $Seconds
Write-Host "Delay finished."
