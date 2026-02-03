# Import data to Render PostgreSQL
# Replace with your actual connection string from Render
$DATABASE_URL = "postgresql://fraudguard_user:xxx@dpg-xxx.oregon-postgres.render.com/fraudguard"

Write-Host "📊 Importing data to Render PostgreSQL..." -ForegroundColor Cyan

# Check if psql is installed
if (!(Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "❌ PostgreSQL client not found!" -ForegroundColor Red
    Write-Host "Installing via winget..." -ForegroundColor Yellow
    winget install PostgreSQL.PostgreSQL
}

# Import schema
Write-Host "`n🏗️ Creating schema..." -ForegroundColor Yellow
$env:PGPASSWORD = ($DATABASE_URL -split ':' -split '@')[2]
psql $DATABASE_URL -f "E:\FraudGuard-AI\services\api-gateway\setup_database.sql"

# Import data
Write-Host "`n📦 Importing 42 fraud numbers..." -ForegroundColor Yellow
psql $DATABASE_URL -f "E:\FraudGuard-AI\services\api-gateway\seed_data.sql"

# Verify
Write-Host "`n✔️ Verifying..." -ForegroundColor Yellow
psql $DATABASE_URL -c "SELECT COUNT(*) FROM blacklist;"

Write-Host "`n✅ Done!" -ForegroundColor Green
