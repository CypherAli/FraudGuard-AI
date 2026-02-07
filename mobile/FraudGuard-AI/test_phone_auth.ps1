# ====================================
# Test Firebase Phone Authentication
# ====================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST FIREBASE PHONE AUTHENTICATION" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

# Check prerequisites
Write-Host "[1] Kiem tra SHA certificates..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

$keytoolPath = "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe"
$debugKeystore = "$env:USERPROFILE\.android\debug.keystore"

if (-Not (Test-Path $keytoolPath)) {
    Write-Host "[ERROR] Khong tim thay keytool!" -ForegroundColor Red
    exit 1
}

if (-Not (Test-Path $debugKeystore)) {
    Write-Host "[ERROR] Khong tim thay debug.keystore!" -ForegroundColor Red
    exit 1
}

# Get SHA-1
Write-Host "SHA-1 Certificate:" -ForegroundColor Yellow
$sha1 = & $keytoolPath -list -v -keystore $debugKeystore -alias androiddebugkey -storepass android -keypass android 2>$null | Select-String "SHA1:"
if ($sha1) {
    Write-Host "$sha1" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Khong lay duoc SHA-1!" -ForegroundColor Red
}

# Get SHA-256
Write-Host "`nSHA-256 Certificate:" -ForegroundColor Yellow
$sha256 = & $keytoolPath -list -v -keystore $debugKeystore -alias androiddebugkey -storepass android -keypass android 2>$null | Select-String "SHA256:"
if ($sha256) {
    Write-Host "$sha256" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Khong lay duoc SHA-256!" -ForegroundColor Red
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Check google-services.json
Write-Host "[2] Kiem tra google-services.json..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

$googleServicesPath = "Platforms\Android\google-services.json"

if (Test-Path $googleServicesPath) {
    Write-Host "[OK] File ton tai: $googleServicesPath" -ForegroundColor Green
    
    # Check file size
    $fileInfo = Get-Item $googleServicesPath
    Write-Host "Kich thuoc: $($fileInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "Ngay cap nhat: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
    
    # Check package name in file
    $content = Get-Content $googleServicesPath -Raw
    if ($content -match '"package_name":\s*"([^"]+)"') {
        $packageName = $matches[1]
        Write-Host "Package name: $packageName" -ForegroundColor Cyan
        
        if ($packageName -eq "com.fraudguard.ai") {
            Write-Host "[OK] Package name khop!" -ForegroundColor Green
        } else {
            Write-Host "[WARNING] Package name KHONG khop!" -ForegroundColor Yellow
            Write-Host "Expected: com.fraudguard.ai" -ForegroundColor Gray
            Write-Host "Found: $packageName" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "[ERROR] Khong tim thay google-services.json!" -ForegroundColor Red
    Write-Host "Path: $googleServicesPath" -ForegroundColor Gray
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Check FraudGuardAI.csproj
Write-Host "[3] Kiem tra FraudGuardAI.csproj..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

$csprojPath = "FraudGuardAI.csproj"

if (Test-Path $csprojPath) {
    $content = Get-Content $csprojPath -Raw
    
    # Check ApplicationId
    if ($content -match '<ApplicationId>([^<]+)</ApplicationId>') {
        $appId = $matches[1]
        Write-Host "ApplicationId: $appId" -ForegroundColor Cyan
        
        if ($appId -eq "com.fraudguard.ai") {
            Write-Host "[OK] ApplicationId khop!" -ForegroundColor Green
        } else {
            Write-Host "[WARNING] ApplicationId KHONG khop!" -ForegroundColor Yellow
        }
    }
    
    # Check Plugin.Firebase.Auth
    if ($content -match 'Plugin\.Firebase\.Auth') {
        Write-Host "[OK] Plugin.Firebase.Auth da duoc cai dat" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Plugin.Firebase.Auth chua duoc cai dat!" -ForegroundColor Yellow
    }
    
    # Check GoogleServicesJson
    if ($content -match '<GoogleServicesJson') {
        Write-Host "[OK] GoogleServicesJson da duoc config" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] GoogleServicesJson chua duoc config!" -ForegroundColor Yellow
    }
} else {
    Write-Host "[ERROR] Khong tim thay FraudGuardAI.csproj!" -ForegroundColor Red
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Check Services
Write-Host "[4] Kiem tra Services..." -ForegroundColor Cyan
Write-Host "----------------------------------------`n" -ForegroundColor Gray

$servicesPath = "Services\FirebaseAuthService.cs"
if (Test-Path $servicesPath) {
    Write-Host "[OK] FirebaseAuthService.cs ton tai" -ForegroundColor Green
} else {
    Write-Host "[ERROR] FirebaseAuthService.cs khong ton tai!" -ForegroundColor Red
}

$authPagePath = "Pages\PhoneAuthPage.xaml"
if (Test-Path $authPagePath) {
    Write-Host "[OK] PhoneAuthPage.xaml ton tai" -ForegroundColor Green
} else {
    Write-Host "[ERROR] PhoneAuthPage.xaml khong ton tai!" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "    CAC BUOC TIEP THEO" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "
1. DAM BAO DA THEM SHA-1 VA SHA-256 VAO FIREBASE:
   - Vao Firebase Console > Project Settings
   - Them CA HAI ma SHA phia tren vao 'SHA certificate fingerprints'
   - SHA-1 cho reCAPTCHA
   - SHA-256 cho Play Integrity API

2. KICH HOAT PHONE AUTHENTICATION:
   - Vao Firebase Console > Authentication
   - Tab 'Sign-in method'
   - Bat 'Phone' provider
   - (Optional) Gioi han vung: chi Vietnam (+84)

3. TAI LAI google-services.json:
   - Firebase Console > Project Settings
   - Download google-services.json moi
   - Copy vao: Platforms\Android\
   - Thay the file cu

4. THEM SO DIEN THOAI TEST (khong ton SMS):
   - Firebase Console > Authentication > Sign-in method
   - Phone > 'Phone numbers for testing'
   - Them: +84 650-555-3434 | Code: 654321
   - Dung cho development, XOA truoc khi release!

5. BUILD VA TEST:
   - Visual Studio: Clean Solution
   - Rebuild Project
   - Deploy to Android device/emulator
   - Mo app > Dang nhap bang so dien thoai
   - Nhap so test: +84 650-555-3434
   - Nhap OTP: 654321
   - Success! ✅

" -ForegroundColor White

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    TROUBLESHOOTING" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "
LOI THUONG GAP:

❌ 'This app is not authorized...'
   → SHA-1/SHA-256 chua duoc them vao Firebase
   → Hoac google-services.json chua cap nhat

❌ 'The SMS quota has been exceeded'
   → Dung so dien thoai test thay vi so that
   → Hoac cho 24h de reset quota

❌ reCAPTCHA luon xuat hien
   → Binh thuong neu thieu SHA-256
   → Them SHA-256 de dung Play Integrity API

❌ 'Missing Activity for reCAPTCHA'
   → Da fix trong MainActivity.cs
   → Rebuild app

" -ForegroundColor Gray

Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Doc chi tiet tai: PHONE_AUTH_SETUP.md`n" -ForegroundColor Cyan

Write-Host "[DONE] Kiem tra hoan tat!" -ForegroundColor Green
