# Script to verify MAUI installation and prepare for Android deployment
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"

Write-Host "=== Verifying .NET MAUI Installation ===" -ForegroundColor Cyan
& $dotnetPath workload list

Write-Host "`n=== Checking for ADB (Android Debug Bridge) ===" -ForegroundColor Cyan
$adbPath = Get-ChildItem -Path "$env:LOCALAPPDATA\Android\Sdk\platform-tools" -Filter "adb.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($adbPath) {
    Write-Host "ADB found at: $($adbPath.FullName)" -ForegroundColor Green
    & $adbPath.FullName devices
} else {
    Write-Host "ADB not found in standard location. Checking dotnet SDK location..." -ForegroundColor Yellow
    $adbPath = Get-ChildItem -Path "C:\Program Files\dotnet" -Filter "adb.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($adbPath) {
        Write-Host "ADB found at: $($adbPath.FullName)" -ForegroundColor Green
        & $adbPath.FullName devices
    } else {
        Write-Host "ADB not found. It should be installed with MAUI workload." -ForegroundColor Red
    }
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Restart VS Code completely" -ForegroundColor Yellow
Write-Host "2. Open the mobile project folder: e:\FraudGuard-AI\mobile\FraudGuard-AI" -ForegroundColor Yellow
Write-Host "3. Connect your Android device via USB" -ForegroundColor Yellow
Write-Host "4. Enable USB Debugging on your device" -ForegroundColor Yellow
Write-Host "5. Run: dotnet build -t:Run -f net8.0-android" -ForegroundColor Yellow
