# ============================================
# test-all-resources.ps1
# End-to-end test for Aspire services
# ============================================

# Service URLs (from ServiceDefaults)
$AspireServices = @{
    "eventbus-mock" = "https://localhost:7240"
    "meter-ingestion" = "https://localhost:7242"
    "multi-site-energy-platform" = "https://localhost:7244"
    "stub-services" = "https://localhost:7246"
}

# -----------------------------
# Function: Health check
# -----------------------------
function Test-Health($name, $url) {
    Write-Host "Checking health for $name..."
    try {
        $resp = Invoke-RestMethod -Uri "$url/health" -Method Get -SkipCertificateCheck
        Write-Host "$name is Healthy ✅"
        return $true
    } catch {
        Write-Host "$name health check failed ❌ $_"
        return $false
    }
}

# -----------------------------
# Function: Test endpoint
# -----------------------------
function Test-Endpoint($name, $url, $method, $endpoint, $body=$null) {
    Write-Host "Testing $name endpoint $endpoint..."
    try {
        if ($body) {
            $resp = Invoke-RestMethod -Uri "$url$endpoint" -Method $method -Body $body -SkipCertificateCheck -ContentType "application/json"
        } else {
            $resp = Invoke-RestMethod -Uri "$url$endpoint" -Method $method -SkipCertificateCheck
        }
        Write-Host "$name $endpoint response:"
        Write-Host $resp
    } catch {
        Write-Host "$name $endpoint failed ❌ $_"
    }
}

# -----------------------------
# 1️⃣ Health checks
# -----------------------------
$allHealthy = $true
foreach ($svc in $AspireServices.Keys) {
    $healthy = Test-Health $svc $AspireServices[$svc]
    if (-not $healthy) { $allHealthy = $false }
}

if (-not $allHealthy) {
    Write-Host "One or more services are unhealthy, stopping tests."
    exit
}

Write-Host "All services are healthy! Proceeding with endpoint tests..."

# -----------------------------
# 2️⃣ Test main endpoints
# -----------------------------

# Eventbus-mock POST
$event = @{ EventType="TestEvent"; Payload="Hello Aspire" } | ConvertTo-Json
Test-Endpoint "eventbus-mock" $AspireServices["eventbus-mock"] "POST" "/api/eventbus/publish" $event

# Meter-ingestion POST
$reading = @{ MeterId="123"; Value=42; Timestamp=(Get-Date) } | ConvertTo-Json
Test-Endpoint "meter-ingestion" $AspireServices["meter-ingestion"] "POST" "/api/meter/ingest" $reading

# Multi-site-energy-platform GET
Test-Endpoint "multi-site-energy-platform" $AspireServices["multi-site-energy-platform"] "GET" "/api/platform/status"

# Stub-services GET
Test-Endpoint "stub-services" $AspireServices["stub-services"] "GET" "/api/stub/status"

Write-Host "✅ End-to-end test complete!"
