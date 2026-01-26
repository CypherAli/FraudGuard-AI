# NGROK AUTO-INSTALLER FOR WINDOWS
$ErrorActionPreference = "Stop"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  NGROK AUTO-INSTALLER" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan

# Configuration
$ngrokUrl = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip"
$downloadPath = "$env:TEMP\ngrok.zip"
$extractPath = "$env:LOCALAPPDATA\ngrok"
$ngrokExe = Join-Path $extractPath "ngrok.exe"

# Step 1: Download ngrok
Write-Host "`n[1/4] Downloading ngrok..." -ForegroundColor Cyan
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $ngrokUrl -OutFile $downloadPath -UseBasicParsing
    Write-Host "Downloaded successfully" -ForegroundColor Green
} catch {
    Write-Host "Download failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Extract
Write-Host "`n[2/4] Extracting..." -ForegroundColor Cyan
if (-not (Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
}
try {
    Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force
    Write-Host "Extracted to: $extractPath" -ForegroundColor Green
} catch {
    Write-Host "Extraction failed" -ForegroundColor Red
    exit 1
}

# Step 3: Add to PATH
Write-Host "`n[3/4] Adding to PATH..." -ForegroundColor Cyan
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$extractPath*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$extractPath", "User")
    $env:Path += ";$extractPath"
    Write-Host "Added to PATH" -ForegroundColor Green
} else {
    Write-Host "Already in PATH" -ForegroundColor Green
}

# Step 4: Verify
Write-Host "`n[4/4] Verifying..." -ForegroundColor Cyan
Start-Sleep -Seconds 1
if (Test-Path $ngrokExe) {
    & $ngrokExe version
    Write-Host "`nNgrok installed successfully!" -ForegroundColor Green
} else {
    Write-Host "Verification failed" -ForegroundColor Red
    exit 1
}

# Cleanup
Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "  INSTALLATION COMPLETE" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Green

Write-Host "`nNEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Get auth token: https://dashboard.ngrok.com/get-started/your-authtoken" -ForegroundColor White
Write-Host "  2. Configure: ngrok config add-authtoken YOUR_TOKEN" -ForegroundColor White
Write-Host "  3. Start: cd services\api-gateway && .\setup_ngrok.ps1" -ForegroundColor White
Write-Host "`nRESTART PowerShell for PATH changes!" -ForegroundColor Red
Write-Host ""
