# Import Blacklist Data to Railway PostgreSQL
Write-Host "📊 Importing fraud blacklist data to Railway PostgreSQL..." -ForegroundColor Cyan

# Get database credentials from Railway
Write-Host "`n🔑 Getting database credentials..." -ForegroundColor Yellow
$env:PGPASSWORD = railway variables get PGPASSWORD

$dbHost = railway variables get PGHOST
$dbPort = railway variables get PGPORT  
$dbName = railway variables get PGDATABASE
$dbUser = railway variables get PGUSER

Write-Host "Host: $dbHost" -ForegroundColor Gray
Write-Host "Database: $dbName" -ForegroundColor Gray

# Check if psql is available
if (!(Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "`n❌ PostgreSQL client (psql) not found!" -ForegroundColor Red
    Write-Host "`n📥 Installing PostgreSQL client..." -ForegroundColor Yellow
    
    # Try to install via winget (Windows 11)
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        winget install --id PostgreSQL.PostgreSQL -e --silent
    } else {
        Write-Host "Please install PostgreSQL manually:" -ForegroundColor Yellow
        Write-Host "https://www.postgresql.org/download/windows/" -ForegroundColor Cyan
        Start-Process "https://www.postgresql.org/download/windows/"
        exit 1
    }
}

# Step 1: Create schema
Write-Host "`n🏗️ Step 1: Creating database schema..." -ForegroundColor Yellow
railway run psql -f setup_database.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Schema created successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create schema!" -ForegroundColor Red
    exit 1
}

# Step 2: Import seed data
Write-Host "`n📦 Step 2: Importing 42 fraud phone numbers..." -ForegroundColor Yellow
railway run psql -f seed_data.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Data imported successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to import data!" -ForegroundColor Red
    exit 1
}

# Step 3: Verify
Write-Host "`n✔️ Step 3: Verifying data..." -ForegroundColor Yellow
$count = railway run psql -c "SELECT COUNT(*) FROM blacklist;"
Write-Host "Total records: $count" -ForegroundColor Green

Write-Host "`n🎉 Database setup completed!" -ForegroundColor Green
Write-Host "`nNext: Deploy API server to Railway" -ForegroundColor Cyan
