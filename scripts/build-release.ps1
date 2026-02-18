#!/usr/bin/env pwsh
# build-release.ps1 - Build Release APK (ARM64) để phân phối cho người dùng

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building FraudGuard AI Release APK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$projectRoot = Split-Path $PSScriptRoot -Parent
$mobileDir = Join-Path $projectRoot "mobile\FraudGuard-AI"
Set-Location $mobileDir

Write-Host "`n[1/4] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release | Out-Null

Write-Host "[2/4] Restoring packages..." -ForegroundColor Yellow
dotnet restore | Out-Null

Write-Host "[3/4] Building Release APK (ARM64)..." -ForegroundColor Yellow
$androidSdk = "$env:LOCALAPPDATA\Android\Sdk"
dotnet publish -f net8.0-android -c Release `
    -p:AndroidPackageFormat=apk `
    -p:RuntimeIdentifier=android-arm64 `
    -p:AndroidSdkDirectory="$androidSdk"

Write-Host "[4/4] Packaging..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

$apkPath = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*-Signed.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($apkPath) {
    $fileSize = [math]::Round($apkPath.Length / 1MB, 2)
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputName = "FraudGuardAI_v1.0_$timestamp.apk"
    $outputPath = Join-Path $projectRoot "build\$outputName"

    New-Item -ItemType Directory -Path (Join-Path $projectRoot "build") -Force | Out-Null
    Copy-Item $apkPath.FullName -Destination $outputPath -Force

    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nAPK: $outputPath" -ForegroundColor Cyan
    Write-Host "Size: $fileSize MB" -ForegroundColor Yellow
} else {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "  BUILD FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
