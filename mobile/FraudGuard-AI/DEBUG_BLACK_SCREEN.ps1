#!/usr/bin/env pwsh
# DEBUG_BLACK_SCREEN.ps1 - Script để debug màn hình đen trong app

Write-Host "`n🔍 ===== DEBUG BLACK SCREEN ISSUE =====" -ForegroundColor Cyan
Write-Host "This script will help identify why the app shows a black screen`n" -ForegroundColor Yellow

# Step 1: Check if device is connected
Write-Host "`n[1] Checking ADB connection..." -ForegroundColor Cyan
adb devices
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ADB not found or device not connected!" -ForegroundColor Red
    Write-Host "Make sure your phone is connected via USB with USB Debugging enabled`n" -ForegroundColor Yellow
    exit 1
}

# Step 2: Clear app data and logcat
Write-Host "`n[2] Clearing old app data and logcat..." -ForegroundColor Cyan
adb shell pm clear com.fraudguard.ai 2>$null
adb logcat -c

# Step 3: Install latest APK
Write-Host "`n[3] Installing latest APK..." -ForegroundColor Cyan
$apkPath = "bin\Release\net8.0-android\com.fraudguard.ai-Signed.apk"
if (-not (Test-Path $apkPath)) {
    Write-Host "❌ APK not found at $apkPath" -ForegroundColor Red
    Write-Host "Please build the app first using BUILD_APK_FIX_CRASH.ps1`n" -ForegroundColor Yellow
    exit 1
}

adb install -r $apkPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to install APK!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ APK installed successfully`n" -ForegroundColor Green

# Step 4: Start logcat in background
Write-Host "[4] Starting logcat capture..." -ForegroundColor Cyan
Write-Host "📱 Now launch the app on your phone..." -ForegroundColor Yellow
Write-Host "🔍 Watching for crashes and errors...\n" -ForegroundColor Yellow

# Create log file
$logFile = "debug_black_screen_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

Write-Host "💾 Full log will be saved to: $logFile`n" -ForegroundColor Cyan

# Filter for our app and common errors
adb logcat -v time | Tee-Object -FilePath $logFile | Select-String -Pattern "fraudguard|FraudGuard|FATAL|AndroidRuntime|crash|Exception" -CaseSensitive:$false

Write-Host "`n✅ Debug session ended. Check $logFile for full details." -ForegroundColor Green
