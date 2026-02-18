# Deploy FraudGuard-AI to Android Device
# This script uses the full path to dotnet.exe to avoid PATH issues

$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$adbPath = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"

Write-Host "=== FraudGuard-AI Mobile Deployment ===" -ForegroundColor Cyan
Write-Host ""

# Check device connection
Write-Host "Checking device connection..." -ForegroundColor Yellow
& $adbPath devices
Write-Host ""

# Build and deploy
Write-Host "Building and deploying app to Android device..." -ForegroundColor Yellow
Write-Host "This may take a few minutes on first build..." -ForegroundColor Gray
Write-Host ""

& $dotnetPath build -t:Run -f net8.0-android

Write-Host ""
Write-Host "=== Deployment Complete! ===" -ForegroundColor Green
Write-Host "Check your phone for the FraudGuard-AI app!" -ForegroundColor Cyan
