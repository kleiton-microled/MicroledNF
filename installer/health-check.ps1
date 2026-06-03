# Post-install smoke test for Local Agent
param(
    [int] $Port = 5278,
    [int] $MaxAttempts = 15,
    [int] $DelaySeconds = 2
)

$ErrorActionPreference = "Continue"
$uri = "http://127.0.0.1:$Port/api/local/health"

for ($i = 1; $i -le $MaxAttempts; $i++) {
    try {
        $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "Health check OK: $uri"
            Write-Host $response.Content
            exit 0
        }
    }
    catch {
        Write-Host "Attempt $i/$MaxAttempts - waiting for Local Agent..."
        Start-Sleep -Seconds $DelaySeconds
    }
}

Write-Warning "Health check failed after $MaxAttempts attempts: $uri"
exit 1
