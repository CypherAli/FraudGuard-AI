# ====================================
# Script lấy SHA-1 cho Firebase
# ====================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    LAY MA SHA-1 CHO FIREBASE" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

# Đường dẫn keytool
$keytoolPath = "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe"

if (-Not (Test-Path $keytoolPath)) {
    Write-Host "[ERROR] Khong tim thay keytool tai: $keytoolPath" -ForegroundColor Red
    Write-Host "Hay kiem tra lai duong dan Java JDK!" -ForegroundColor Yellow
    exit 1
}

Write-Host "[1] Lay SHA-1 cho DEBUG keystore..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

# DEBUG keystore
$debugKeystore = "$env:USERPROFILE\.android\debug.keystore"

if (Test-Path $debugKeystore) {
    Write-Host "Keystore: $debugKeystore" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "SHA-1 (bat buoc cho reCAPTCHA):" -ForegroundColor Yellow
    & $keytoolPath -list -v -keystore $debugKeystore -alias androiddebugkey -storepass android -keypass android | Select-String "SHA1:"
    
    Write-Host "`nSHA-256 (bat buoc cho Play Integrity API):" -ForegroundColor Yellow
    & $keytoolPath -list -v -keystore $debugKeystore -alias androiddebugkey -storepass android -keypass android | Select-String "SHA256:"
    
    Write-Host "`n[SUCCESS] Da lay SHA-1 va SHA-256 cho DEBUG!" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Khong tim thay debug.keystore!" -ForegroundColor Yellow
    Write-Host "Path: $debugKeystore" -ForegroundColor Gray
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# ====================================
# RELEASE keystore (nếu có)
# ====================================
Write-Host "[2] Lay SHA-1 cho RELEASE keystore..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

$releaseKeystore = "$PSScriptRoot\fraudguard-release.keystore"

if (Test-Path $releaseKeystore) {
    Write-Host "Keystore: $releaseKeystore" -ForegroundColor Green
    Write-Host ""
    
    $storePass = Read-Host "Nhap mat khau keystore (Enter de bo qua)"
    
    if ($storePass) {
        Write-Host "SHA-1:" -ForegroundColor Yellow
        & $keytoolPath -list -v -keystore $releaseKeystore -storepass $storePass | Select-String "SHA1:"
        Write-Host "`nSHA-256:" -ForegroundColor Yellow
        & $keytoolPath -list -v -keystore $releaseKeystore -storepass $storePass | Select-String "SHA256:"
        Write-Host "`n[SUCCESS] Da lay SHA-1 va SHA-256 cho RELEASE!" -ForegroundColor Green
    } else {
        Write-Host "[SKIPPED] Bo qua release keystore." -ForegroundColor Yellow
    }
} else {
    Write-Host "[INFO] Chua co release keystore." -ForegroundColor Yellow
    Write-Host "Ban chi can them SHA-1 cua DEBUG vao Firebase la du!" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "    CAC BUOC TIEP THEO" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "
1. COPY CA HAI MA SHA-1 VA SHA-256 phia tren

2. VAO FIREBASE CONSOLE:
   - Mo Firebase Console > Project Settings
   - Chon tab 'Your apps' > Android app
   - Keo xuong phan 'SHA certificate fingerprints'
   
3. THEM SHA-1 (Bat buoc cho reCAPTCHA):
   - Click 'Add fingerprint'
   - Dan ma SHA-1 vao (VD: 21:56:A3:0D:E9:61:...)
   - Click 'Save'

4. THEM SHA-256 (Bat buoc cho Play Integrity API):
   - Click 'Add fingerprint' lan nua
   - Dan ma SHA-256 vao (dai hon SHA-1)
   - Click 'Save'

5. TAI LAI google-services.json:
   - Tai file moi tu Firebase
   - Copy vao: mobile\FraudGuard-AI\Platforms\Android\
   - Thay the file cu

6. REBUILD APP:
   - Clean solution
   - Rebuild project
   - Chay lai app

" -ForegroundColor White

Write-Host "========================================`n" -ForegroundColor Cyan

# Mở thư mục debug keystore
Write-Host "Ban muon mo thu muc chua debug keystore? (Y/N): " -ForegroundColor Cyan -NoNewline
$openFolder = Read-Host

if ($openFolder -eq "Y" -or $openFolder -eq "y") {
    Start-Process "explorer.exe" -ArgumentList "$env:USERPROFILE\.android"
}

Write-Host "`n[DONE] Hoan thanh!" -ForegroundColor Green
