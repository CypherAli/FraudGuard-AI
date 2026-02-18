# FraudGuard AI - Import Seed Data Script
Write-Host "Import Seed Data" -ForegroundColor Cyan

$PSQL = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$PG_HOST = "localhost"
$PG_PORT = "5433"
$PG_USER = "fraudguard"
$PG_DB = "fraudguard_db"

if (-not (Test-Path $PSQL)) {
    Write-Host "PostgreSQL not found at: $PSQL" -ForegroundColor Red
    exit 1
}

$seedScript = Join-Path $PSScriptRoot "seed_data.sql"
if (-not (Test-Path $seedScript)) {
    Write-Host "Seed data file not found" -ForegroundColor Red
    exit 1
}

Write-Host "Importing seed data..." -ForegroundColor Cyan
$env:PGPASSWORD = "fraudguard_secure_2024"
$env:PGCLIENTENCODING = "UTF8"
& $PSQL -U $PG_USER -h $PG_HOST -p $PG_PORT -d $PG_DB -f $seedScript

if ($LASTEXITCODE -eq 0) {
    Write-Host "Seed data imported successfully!" -ForegroundColor Green
}
else {
    Write-Host "Seed data import failed!" -ForegroundColor Red
}

$env:PGPASSWORD = ""
