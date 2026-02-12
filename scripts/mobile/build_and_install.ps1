#!/usr/bin/env pwsh
# AUTO_BUILD_AND_INSTALL.ps1 - Tự động build và install APK

Write-Host "`n🚀 ===== AUTO BUILD & INSTALL =====" -ForegroundColor Cyan
Write-Host "This will automatically build and install the app`n" -ForegroundColor Yellow

# Step 1: Clean old builds
Write-Host "[1/6] Cleaning old builds..." -ForegroundColor Cyan
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
    Write-Host "✅ Cleaned bin folder" -ForegroundColor Green
}
if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
    Write-Host "✅ Cleaned obj folder" -ForegroundColor Green
}

# Step 2: Restore packages
Write-Host "`n[2/6] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore FraudGuardAI.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore packages!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages restored" -ForegroundColor Green

# Step 3: Build Release APK
Write-Host "`n[3/6] Building Release APK..." -ForegroundColor Cyan
Write-Host "⏳ This may take 5-10 minutes..." -ForegroundColor Yellow

dotnet publish FraudGuardAI.csproj `
    -c Release `
    -f net8.0-android `
    -p:AndroidSdkDirectory="C:\Users\trinh\AppData\Local\Android\Sdk" `
    -p:AndroidPackageFormat=apk `
    -p:AndroidSigningKeyStore="" `
    -p:AndroidSigningStorePass="" `
    -p:AndroidSigningKeyAlias="" `
    -p:AndroidSigningKeyPass=""

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
    Write-Host "Try building from Visual Studio instead." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Build completed!" -ForegroundColor Green

# Step 4: Find APK
Write-Host "`n[4/6] Locating APK..." -ForegroundColor Cyan
$apkPath = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*.apk" -Recurse | 
    Where-Object { $_.Name -like "*Signed.apk" -or $_.Name -like "*com.fraudguard.ai*.apk" } | 
    Select-Object -First 1

if (-not $apkPath) {
    Write-Host "❌ APK not found!" -ForegroundColor Red
    Write-Host "Looking for any APK..." -ForegroundColor Yellow
    $apkPath = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*.apk" -Recurse | Select-Object -First 1
}

if (-not $apkPath) {
    Write-Host "❌ No APK found in build output!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found APK: $($apkPath.Name)" -ForegroundColor Green
Write-Host "   Size: $([math]::Round($apkPath.Length/1MB,2)) MB" -ForegroundColor White
Write-Host "   Path: $($apkPath.FullName)" -ForegroundColor Gray

# Step 5: Check ADB connection
Write-Host "`n[5/6] Checking device connection..." -ForegroundColor Cyan
$devices = adb devices | Select-String -Pattern "device$"
if ($devices.Count -eq 0) {
    Write-Host "⚠️  No device connected!" -ForegroundColor Yellow
    Write-Host "Connect your phone via USB and enable USB Debugging" -ForegroundColor Yellow
    Write-Host "`nAPK is ready at:" -ForegroundColor Cyan
    Write-Host "   $($apkPath.FullName)" -ForegroundColor White
    Write-Host "`nYou can install manually or run this script again after connecting device." -ForegroundColor Yellow
    exit 0
}

Write-Host "✅ Device connected" -ForegroundColor Green

# Step 6: Install APK
Write-Host "`n[6/6] Installing APK..." -ForegroundColor Cyan
adb install -r $apkPath.FullName

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ ===== INSTALLATION COMPLETE! =====" -ForegroundColor Green
    Write-Host "`n📱 Launch the app on your phone now!" -ForegroundColor Cyan
    Write-Host "   Package: com.fraudguard.ai" -ForegroundColor White
    Write-Host "   Name: FraudGuard AI" -ForegroundColor White
    Write-Host "`n💡 To see logs:" -ForegroundColor Yellow
    Write-Host "   adb logcat | Select-String 'fraudguard'" -ForegroundColor White
} else {
    Write-Host "`n❌ Installation failed!" -ForegroundColor Red
    Write-Host "You can install manually:" -ForegroundColor Yellow
    Write-Host "   adb install -r $($apkPath.FullName)" -ForegroundColor White
    exit 1
}

Write-Host "`n🎉 Done!" -ForegroundColor Green
