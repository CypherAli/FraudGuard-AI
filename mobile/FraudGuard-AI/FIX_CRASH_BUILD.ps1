#!/usr/bin/env pwsh
# Fix crash build script - Disable AOT to prevent GUID mismatch

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host " FIX CRASH BUILD APK " -ForegroundColor Yellow  
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Issue: GUID mismatch in AndroidX.AppCompat" -ForegroundColor Red
Write-Host "Solution: Build with Interpreter mode (no AOT)`n" -ForegroundColor Green

# Step 1: Uninstall old app
Write-Host "Step 1: Uninstalling old app from device..." -ForegroundColor Cyan
$env:Path += ";C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools"
adb uninstall com.fraudguard.ai 2>&1 | Out-Null
Write-Host "✓ Uninstalled`n" -ForegroundColor Green

# Step 2: Clean
Write-Host "Step 2: Cleaning..." -ForegroundColor Cyan
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue }
Write-Host "✓ Cleaned`n" -ForegroundColor Green

# Step 3: Restore
Write-Host "Step 3: Restoring packages..." -ForegroundColor Cyan
dotnet restore FraudGuardAI.csproj --force | Out-Null
Write-Host "✓ Restored`n" -ForegroundColor Green

# Step 4: Build with Interpreter mode
Write-Host "Step 4: Building APK (Interpreter mode, no AOT)..." -ForegroundColor Cyan
Write-Host "This will take 5-10 minutes. Please wait...`n" -ForegroundColor Yellow

$buildParams = @(
    "publish"
    "FraudGuardAI.csproj"
    "-c", "Release"
    "-f", "net8.0-android"
    "-p:UseInterpreter=true"
    "-p:RunAOTCompilation=false"
    "-p:AndroidLinkMode=None"
    "-p:AndroidUseAssemblyStore=false"
)

$startTime = Get-Date
dotnet @buildParams

$duration = (Get-Date) - $startTime

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ BUILD SUCCESS in $([math]::Round($duration.TotalMinutes,1)) minutes!`n" -ForegroundColor Green
    
    # Find APK
    $apkPath = Get-ChildItem -Path "bin\Release\net8.0-android\publish\" -Filter "*.apk" -Recurse | Select-Object -First 1
    
    if ($apkPath) {
        $apkSize = [math]::Round($apkPath.Length / 1MB, 1)
        Write-Host "APK Location: $($apkPath.FullName)" -ForegroundColor Cyan
        Write-Host "APK Size: ${apkSize}MB`n" -ForegroundColor Cyan
        
        # Step 5: Install
        Write-Host "Step 5: Installing to device..." -ForegroundColor Cyan
        adb install -r $apkPath.FullName
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✓ INSTALLED SUCCESSFULLY!" -ForegroundColor Green
            Write-Host "`nYou can now open the app on your phone." -ForegroundColor Yellow
        } else {
            Write-Host "`n✗ Installation failed" -ForegroundColor Red
        }
    }
} else {
    Write-Host "`n✗ BUILD FAILED after $([math]::Round($duration.TotalMinutes,1)) minutes`n" -ForegroundColor Red
    Write-Host "Check errors above." -ForegroundColor Yellow
}
