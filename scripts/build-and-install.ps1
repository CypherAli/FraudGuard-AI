#!/usr/bin/env pwsh
# build-and-install.ps1 - Build APK và install thẳng lên device qua ADB

Write-Host "`n=== AUTO BUILD & INSTALL ===" -ForegroundColor Cyan
Write-Host "This will automatically build and install the app`n" -ForegroundColor Yellow

$projectRoot = Split-Path $PSScriptRoot -Parent
$mobileDir = Join-Path $projectRoot "mobile\FraudGuard-AI"
Set-Location $mobileDir

# Step 1: Clean
Write-Host "[1/6] Cleaning old builds..." -ForegroundColor Cyan
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue; Write-Host "  Cleaned bin/" -ForegroundColor Green }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue; Write-Host "  Cleaned obj/" -ForegroundColor Green }

# Step 2: Restore
Write-Host "`n[2/6] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore FraudGuardAI.csproj
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Failed to restore packages!" -ForegroundColor Red; exit 1 }
Write-Host "  Packages restored" -ForegroundColor Green

# Step 3: Build Release APK
Write-Host "`n[3/6] Building Release APK..." -ForegroundColor Cyan
Write-Host "  This may take 5-10 minutes..." -ForegroundColor Yellow

$androidSdk = "$env:LOCALAPPDATA\Android\Sdk"
dotnet publish FraudGuardAI.csproj `
    -c Release `
    -f net8.0-android `
    -p:AndroidSdkDirectory="$androidSdk" `
    -p:AndroidPackageFormat=apk

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nERROR: Build failed!" -ForegroundColor Red
    Write-Host "Try building from Visual Studio instead." -ForegroundColor Yellow
    exit 1
}
Write-Host "  Build completed!" -ForegroundColor Green

# Step 4: Find APK
Write-Host "`n[4/6] Locating APK..." -ForegroundColor Cyan
$apkPath = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*Signed.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $apkPath) {
    $apkPath = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}
if (-not $apkPath) { Write-Host "ERROR: No APK found!" -ForegroundColor Red; exit 1 }

Write-Host "  Found: $($apkPath.Name)" -ForegroundColor Green
Write-Host "  Size: $([math]::Round($apkPath.Length/1MB,2)) MB" -ForegroundColor White

# Step 5: Check ADB device
Write-Host "`n[5/6] Checking device connection..." -ForegroundColor Cyan
$devices = adb devices 2>&1 | Select-String -Pattern "device$"
if ($devices.Count -eq 0) {
    Write-Host "  WARNING: No device connected!" -ForegroundColor Yellow
    Write-Host "  Connect your phone via USB with USB Debugging enabled" -ForegroundColor Yellow
    Write-Host "`n  APK ready at:" -ForegroundColor Cyan
    Write-Host "  $($apkPath.FullName)" -ForegroundColor White
    exit 0
}
Write-Host "  Device connected" -ForegroundColor Green

# Step 6: Install
Write-Host "`n[6/6] Installing APK..." -ForegroundColor Cyan
adb install -r $apkPath.FullName

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=== INSTALLATION COMPLETE! ===" -ForegroundColor Green
    Write-Host "  Package: com.fraudguard.ai" -ForegroundColor White
    Write-Host "  To view logs: adb logcat | Select-String 'fraudguard'" -ForegroundColor Yellow
} else {
    Write-Host "`nERROR: Installation failed!" -ForegroundColor Red
    Write-Host "  Manual install: adb install -r $($apkPath.FullName)" -ForegroundColor White
    exit 1
}
