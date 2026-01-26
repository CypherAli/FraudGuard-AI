# Quick Start Script - FraudGuard AI API Gateway
# Starts the API server with proper configuration

Write-Host " Starting FraudGuard AI API Gateway..." -ForegroundColor Cyan

# Set working directory
Push-Location $PSScriptRoot

# Note: SQLite disabled (requires CGO/GCC). Only PostgreSQL blacklist is active.
$env:CGO_ENABLED = "0"

# Start the server
Write-Host " Location: $(Get-Location)" -ForegroundColor Gray
Write-Host " Executing: go run cmd\api\main.go" -ForegroundColor Gray
Write-Host ""

go run cmd\api\main.go

Pop-Location
