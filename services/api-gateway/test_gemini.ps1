# Test Gemini AI integration
Write-Host "=== Testing Gemini AI Integration ===" -ForegroundColor Cyan
Write-Host ""

# Set directory
Set-Location $PSScriptRoot
Write-Host "📁 Current directory: $(Get-Location)" -ForegroundColor Gray

# Check .env file
if (Test-Path ".env") {
    Write-Host "✅ .env file found" -ForegroundColor Green
    $geminiKey = Get-Content ".env" | Select-String "GEMINI_API_KEY"
    if ($geminiKey) {
        Write-Host "✅ GEMINI_API_KEY configured in .env" -ForegroundColor Green
    } else {
        Write-Host "❌ GEMINI_API_KEY not found in .env" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ .env file not found" -ForegroundColor Red
    exit 1
}

# Build server
Write-Host ""
Write-Host "🔨 Building server..." -ForegroundColor Yellow
go build -o bin/api.exe ./cmd/api
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Start server and capture output
Write-Host ""
Write-Host "🚀 Starting server..." -ForegroundColor Yellow
Write-Host "👀 Watch for Gemini initialization message:" -ForegroundColor Cyan
Write-Host ""

# Run server and show output
.\bin\api.exe 2>&1 | ForEach-Object {
    $line = $_
    if ($line -match "Gemini") {
        Write-Host $line -ForegroundColor Green
    } elseif ($line -match "✅") {
        Write-Host $line -ForegroundColor Green
    } elseif ($line -match "⚠️|Warning") {
        Write-Host $line -ForegroundColor Yellow
    } elseif ($line -match "❌|Error|Failed") {
        Write-Host $line -ForegroundColor Red
    } else {
        Write-Host $line
    }
}
