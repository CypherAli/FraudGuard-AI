#!/usr/bin/env pwsh
# setup-database.ps1 - Khởi tạo PostgreSQL database cho FraudGuard AI
#
# Chạy script này lần đầu để tạo user, database và schema.
# Yêu cầu: PostgreSQL 17 đang chạy trên máy.

Write-Host "`n=== FraudGuard AI - Database Setup ===" -ForegroundColor Cyan

$projectRoot = Split-Path $PSScriptRoot -Parent
$migrationsDir = Join-Path $projectRoot "services\api-gateway\migrations"

# Detect psql path
$psqlPaths = @(
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe"
)
$PSQL = $psqlPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $PSQL) {
    $psqlCmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($psqlCmd) { $PSQL = $psqlCmd.Source }
}

if (-not $PSQL) {
    Write-Host "ERROR: PostgreSQL (psql) not found!" -ForegroundColor Red
    Write-Host "Install from: https://www.postgresql.org/download/windows/" -ForegroundColor Yellow
    exit 1
}
Write-Host "  Using psql: $PSQL" -ForegroundColor Green

# Check PostgreSQL service
$pgService = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($pgService -and $pgService.Status -ne "Running") {
    Write-Host "  Starting PostgreSQL service..." -ForegroundColor Yellow
    Start-Service $pgService.Name
    Start-Sleep -Seconds 3
}

Write-Host "`n[1/3] Creating database and user..." -ForegroundColor Cyan
Write-Host "  (You will be prompted for the postgres superuser password)" -ForegroundColor Yellow
& $PSQL -U postgres -h localhost -p 5433 -f (Join-Path $migrationsDir "000_create_database.sql")
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Step 1 failed!" -ForegroundColor Red; exit 1 }

Write-Host "`n[2/3] Running schema migrations..." -ForegroundColor Cyan
& $PSQL -U fraudguard -h localhost -p 5433 -d fraudguard_db -f (Join-Path $migrationsDir "001_init.sql")
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Step 2 failed!" -ForegroundColor Red; exit 1 }

Write-Host "`n[3/3] Seeding fraud blacklist data..." -ForegroundColor Cyan
& $PSQL -U fraudguard -h localhost -p 5433 -d fraudguard_db -f (Join-Path $migrationsDir "003_seed_data.sql")
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Step 3 failed!" -ForegroundColor Red; exit 1 }

Write-Host "`n=== Database setup complete! ===" -ForegroundColor Green
Write-Host "  Next: Copy services/api-gateway/.env.example to .env and fill in values" -ForegroundColor Cyan
Write-Host "  Then: .\scripts\start-server.ps1" -ForegroundColor Cyan
