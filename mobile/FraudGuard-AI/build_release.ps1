# ========================================
# BUILD RELEASE APK - FRAUDGUARD AI
# Dung de phan phoi cho nguoi dung
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building FraudGuard AI Release APK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Clean previous builds
Write-Host "`n[1/4] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release | Out-Null

# Restore packages
Write-Host "[2/4] Restoring packages..." -ForegroundColor Yellow
dotnet restore | Out-Null

# Build release APK (single architecture for smaller size)
Write-Host "[3/4] Building Release APK (ARM64)..." -ForegroundColor Yellow
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk -p:RuntimeIdentifier=android-arm64

# Find the APK
Write-Host "[4/4] Packaging..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

$apkPath = Get-ChildItem -Path "bin\Release\net8.0-android\*\publish\*-Signed.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($apkPath) {
    $fileSize = [math]::Round($apkPath.Length / 1MB, 2)
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputName = "FraudGuardAI_v1.0_$timestamp.apk"
    
    Copy-Item $apkPath.FullName -Destination $outputName -Force
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  ✅ BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nAPK File: $outputName" -ForegroundColor Cyan
    Write-Host "Size: $fileSize MB" -ForegroundColor Yellow
    Write-Host "Location: $(Get-Location)\$outputName" -ForegroundColor White
    
    Write-Host "`n📱 PHAN PHOI APP:" -ForegroundColor Magenta
    Write-Host "1. Upload file APK len Google Drive/Dropbox" -ForegroundColor White
    Write-Host "2. Tao link chia se (Anyone with link)" -ForegroundColor White
    Write-Host "3. Gui link cho nguoi dung" -ForegroundColor White
    Write-Host "4. Nguoi dung download va cai dat" -ForegroundColor White
    Write-Host "`n⚠️  Luu y: Nguoi dung can bat 'Install from unknown sources'" -ForegroundColor Yellow
    
} else {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "  ❌ BUILD FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Khong tim thay APK file. Check errors tren." -ForegroundColor Yellow
}
