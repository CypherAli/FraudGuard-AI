# FraudGuard AI - Database Setup Script
# This script sets up the PostgreSQL database for FraudGuard AI

Write-Host "🚀 FraudGuard AI - Database Setup" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# PostgreSQL paths
$PSQL = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$PG_HOST = "localhost"
$PG_PORT = "5433"
$PG_USER = "postgres"

# Check if psql exists
if (-not (Test-Path $PSQL)) {
    Write-Host "❌ PostgreSQL not found at: $PSQL" -ForegroundColor Red
    Write-Host "Please update the path in this script." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Found PostgreSQL at: $PSQL" -ForegroundColor Green
Write-Host ""

# Check if PostgreSQL service is running
$service = Get-Service -Name "postgresql-x64-17" -ErrorAction SilentlyContinue
if ($service -eq $null) {
    Write-Host "❌ PostgreSQL service not found!" -ForegroundColor Red
    exit 1
}

if ($service.Status -ne "Running") {
    Write-Host "⚠️  PostgreSQL service is not running. Starting..." -ForegroundColor Yellow
    Start-Service -Name "postgresql-x64-17"
    Start-Sleep -Seconds 3
    Write-Host "✅ PostgreSQL service started" -ForegroundColor Green
} else {
    Write-Host "✅ PostgreSQL service is running" -ForegroundColor Green
}
Write-Host ""

# Run the setup SQL script
Write-Host "📝 Running database setup script..." -ForegroundColor Cyan
Write-Host "You will be prompted for the PostgreSQL 'postgres' user password." -ForegroundColor Yellow
Write-Host ""

$setupScript = Join-Path $PSScriptRoot "setup_database.sql"

if (-not (Test-Path $setupScript)) {
    Write-Host "❌ Setup script not found: $setupScript" -ForegroundColor Red
    exit 1
}

# Execute the SQL script
& $PSQL -U $PG_USER -h $PG_HOST -p $PG_PORT -f $setupScript

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Database setup completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Verify the .env file has correct settings" -ForegroundColor White
    Write-Host "  2. Run: go run cmd/api/main.go" -ForegroundColor White
    Write-Host "  3. Test the API at: http://localhost:8080/health" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Database setup failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Yellow
    Write-Host ""
}
