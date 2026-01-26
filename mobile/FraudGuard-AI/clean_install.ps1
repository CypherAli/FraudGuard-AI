# ========================================
# CLEAN INSTALL - Xóa app cũ và cài mới
# ========================================

Write-Host "`n=== FRAUDGUARD AI - CLEAN INSTALL ===" -ForegroundColor Cyan

$packageName = "com.fraudguard.fraudguardai"

# Tìm ADB
$adbPaths = @(
    "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "$env:ANDROID_HOME\platform-tools\adb.exe"
)

$adb = $null
foreach ($path in $adbPaths) {
    if (Test-Path $path) {
        $adb = $path
        break
    }
}

if (-not $adb) {
    Write-Host "Cannot find ADB. Trying system PATH..." -ForegroundColor Yellow
    $adb = "adb"
}

Write-Host "`n[1/4] Checking device connection..." -ForegroundColor Yellow
& $adb devices

Write-Host "`n[2/4] Uninstalling old app..." -ForegroundColor Yellow
& $adb uninstall $packageName 2>$null
Write-Host "Old app removed (if existed)" -ForegroundColor Green

Write-Host "`n[3/4] Building and deploying new version..." -ForegroundColor Yellow
Write-Host "This will take 2-5 minutes..." -ForegroundColor Gray

Push-Location "E:\FraudGuard-AI\mobile\FraudGuard-AI"

$dotnet = "C:\Program Files\dotnet\dotnet.exe"

# Clean build
& $dotnet clean -f net8.0-android

# Build and deploy
& $dotnet build -t:Run -f net8.0-android -c Debug

Pop-Location

Write-Host "`n[4/4] Getting Ngrok URL..." -ForegroundColor Yellow
$tunnels = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels" -UseBasicParsing
$publicUrl = $tunnels.tunnels[0].public_url
$domain = $publicUrl -replace "https://", ""

Write-Host "`n=== DEPLOY COMPLETE ===" -ForegroundColor Green
Write-Host "`nNext steps on your phone:" -ForegroundColor Cyan
Write-Host "1. Open FraudGuard AI app" -ForegroundColor White
Write-Host "2. Go to Settings tab" -ForegroundColor White
Write-Host "3. Enter Server URL: " -NoNewline -ForegroundColor White
Write-Host $domain -ForegroundColor Yellow
Write-Host "4. Tap Connect" -ForegroundColor White
Write-Host "5. Test with number: 0911333444" -ForegroundColor Red
Write-Host ""
