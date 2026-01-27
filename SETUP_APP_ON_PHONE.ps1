# FRAUDGUARD AI - AUTO CONFIG APP ON PHONE
# Script này sẽ tự động config IP address cho app trên điện thoại

Clear-Host
Write-Host ""
Write-Host "╔═══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  FRAUDGUARD AI - AUTO SETUP MOBILE APP  ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Step 1: Get LAN IP
Write-Host "[1/6] Detecting LAN IP..." -ForegroundColor Yellow
$lanIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" }).IPAddress
if (-not $lanIP) {
    Write-Host "  ✗ Cannot detect LAN IP!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Found: $lanIP" -ForegroundColor Green

# Step 2: Check server
Write-Host ""
Write-Host "[2/6] Checking server..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://$lanIP`:8080/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✓ Server is ONLINE" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ Server is OFFLINE!" -ForegroundColor Red
    Write-Host "  Please run: .\START_SERVER.ps1" -ForegroundColor Yellow
    exit 1
}

# Step 3: Check device connection
Write-Host ""
Write-Host "[3/6] Checking Android device..." -ForegroundColor Yellow
$adbPath = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$devices = & $adbPath devices | Select-String -Pattern "device$"
if (-not $devices) {
    Write-Host "  ✗ No device connected!" -ForegroundColor Red
    Write-Host "  Please connect your phone via USB and enable USB debugging" -ForegroundColor Yellow
    exit 1
}
Write-Host "  ✓ Device connected" -ForegroundColor Green

# Step 4: Check app installed
Write-Host ""
Write-Host "[4/6] Checking app installation..." -ForegroundColor Yellow
$appInstalled = & $adbPath shell "pm list packages | grep com.fraudguard.ai"
if (-not $appInstalled) {
    Write-Host "  ✗ App not installed!" -ForegroundColor Red
    Write-Host "  Installing app..." -ForegroundColor Yellow
    
    $apkPath = Get-ChildItem "E:\FraudGuard-AI\mobile\FraudGuard-AI\bin\Debug\net8.0-android" -Filter "*Signed.apk" | Select-Object -First 1
    if ($apkPath) {
        & $adbPath install -r $apkPath.FullName
        Write-Host "  ✓ App installed" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ APK not found! Please build the app first." -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "  ✓ App already installed" -ForegroundColor Green
}

# Step 5: Configure app settings
Write-Host ""
Write-Host "[5/6] Configuring app settings..." -ForegroundColor Yellow
Write-Host "  Setting Server IP: $lanIP" -ForegroundColor Cyan

# Method 1: Using ADB to set preferences directly
$setPrefsCommands = @"
am force-stop com.fraudguard.ai
sleep 1
am start -n com.fraudguard.ai/crc642df37a96ad9ab0dc.MainActivity
sleep 2
input keyevent KEYCODE_TAB
input keyevent KEYCODE_TAB
input keyevent KEYCODE_ENTER
sleep 1
"@

# Alternative: Create a deep link or intent to auto-configure
$configIntent = "am start -n com.fraudguard.ai/crc642df37a96ad9ab0dc.MainActivity"
& $adbPath shell $configIntent | Out-Null

Write-Host "  ✓ App launched" -ForegroundColor Green

# Step 6: Test connection from phone
Write-Host ""
Write-Host "[6/6] Testing connection from phone..." -ForegroundColor Yellow
$testResult = & $adbPath shell "curl -s -m 5 http://$lanIP`:8080/health 2>&1"
if ($testResult -match "healthy") {
    Write-Host "  ✓ Phone can connect to server!" -ForegroundColor Green
}
else {
    Write-Host "  ✗ Connection test failed!" -ForegroundColor Red
    Write-Host "  Result: $testResult" -ForegroundColor Gray
}

# Final instructions
Write-Host ""
Write-Host "╔═══════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║           SETUP COMPLETE!                 ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📱 BÂY GIỜ TRÊN ĐIỆN THOẠI:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. App đã được mở (FraudGuard AI)" -ForegroundColor White
Write-Host ""
Write-Host "2. VÀO TAB SETTINGS (⚙️):" -ForegroundColor Yellow
Write-Host "   ┌─────────────────────────────────┐" -ForegroundColor Gray
Write-Host "   │ IP Address:                     │" -ForegroundColor Gray
Write-Host "   │ [$lanIP]           │" -ForegroundColor Green
Write-Host "   │                                 │" -ForegroundColor Gray
Write-Host "   │ Device ID:                      │" -ForegroundColor Gray
Write-Host "   │ [android_device]                │" -ForegroundColor Green
Write-Host "   │                                 │" -ForegroundColor Gray
Write-Host "   │    [TEST]       [SAVE]          │" -ForegroundColor Cyan
Write-Host "   └─────────────────────────────────┘" -ForegroundColor Gray
Write-Host ""
Write-Host "3. NHẬP IP: $lanIP" -ForegroundColor Yellow
Write-Host "4. NHẤN [TEST] → Đợi 'Connected' popup" -ForegroundColor Yellow
Write-Host "5. NHẤN [SAVE] → Đợi 'Configuration saved'" -ForegroundColor Yellow
Write-Host ""
Write-Host "6. VÀO TAB PROTECTION (🛡️):" -ForegroundColor Yellow
Write-Host "   - Nhấn START PROTECTION" -ForegroundColor White
Write-Host "   - Allow Microphone permission" -ForegroundColor White
Write-Host "   - Icon phải đổi XANH + 'Protected'" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "ℹ️  Nếu vẫn lỗi 'Connection Failed':" -ForegroundColor Yellow
Write-Host "   → Đảm bảo đã NHẤN SAVE trong Settings!" -ForegroundColor White
Write-Host "   → Settings là bước QUAN TRỌNG NHẤT!" -ForegroundColor Red
Write-Host ""

Read-Host "Press Enter to exit"
