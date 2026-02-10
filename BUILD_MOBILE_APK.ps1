#!/usr/bin/env pwsh
# BUILD_MOBILE_APK.ps1
# Quick build script for FraudGuard-AI mobile app from root directory

Write-Host "`n=== FraudGuard-AI Mobile APK Builder ===" -ForegroundColor Cyan
Write-Host "Building from root directory...`n" -ForegroundColor Yellow

# Navigate to mobile project directory
$mobileDir = Join-Path $PSScriptRoot "mobile\FraudGuard-AI"

if (-not (Test-Path $mobileDir)) {
    Write-Host "ERROR: Mobile project directory not found at: $mobileDir" -ForegroundColor Red
    exit 1
}

Set-Location $mobileDir
Write-Host "Changed to: $mobileDir" -ForegroundColor Green

# Check if project file exists
if (-not (Test-Path "FraudGuardAI.csproj")) {
    Write-Host "ERROR: FraudGuardAI.csproj not found in current directory" -ForegroundColor Red
    exit 1
}

# Clean previous builds
Write-Host "`nStep 1: Cleaning..." -ForegroundColor Cyan
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue }
Write-Host "Clean complete" -ForegroundColor Green

# Restore packages
Write-Host "`nStep 2: Restoring packages..." -ForegroundColor Cyan
dotnet restore FraudGuardAI.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed" -ForegroundColor Red
    exit 1
}

# Build Release APK
Write-Host "`nStep 3: Building Release APK..." -ForegroundColor Cyan
Write-Host "This will take 5-10 minutes. Please wait...`n" -ForegroundColor Yellow

$startTime = Get-Date
dotnet publish FraudGuardAI.csproj -c Release -f net8.0-android

$duration = (Get-Date) - $startTime

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=== BUILD SUCCESS ===" -ForegroundColor Green
    Write-Host "Build completed in $([math]::Round($duration.TotalMinutes,1)) minutes`n" -ForegroundColor Green
    
    # Find and display APK location
    $apkPath = Get-ChildItem -Path "bin\Release" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($apkPath) {
        Write-Host "APK Location:" -ForegroundColor Cyan
        Write-Host $apkPath.FullName -ForegroundColor Yellow
        Write-Host "`nAPK Size: $([math]::Round($apkPath.Length/1MB,2)) MB" -ForegroundColor Cyan
    }
} else {
    Write-Host "`n=== BUILD FAILED ===" -ForegroundColor Red
    Write-Host "Build failed after $([math]::Round($duration.TotalMinutes,1)) minutes" -ForegroundColor Red
    exit 1
}
