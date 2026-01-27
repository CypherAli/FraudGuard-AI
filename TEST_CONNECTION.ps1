# FRAUDGUARD AI - TEST KET NOI
Clear-Host
$adb = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" }).IPAddress

Write-Host "=== FRAUDGUARD AI - QUICK TEST ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Server
Write-Host "[1] Server:" -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri "http://$ip`:8080/health" -TimeoutSec 3 -UseBasicParsing | Out-Null
    Write-Host "    OK - Server ONLINE at $ip" -ForegroundColor Green
}
catch {
    Write-Host "    FAILED - Server OFFLINE!" -ForegroundColor Red
    exit 1
}

# Test 2: Device  
Write-Host "[2] Device:" -ForegroundColor Yellow
$dev = & $adb devices | Select-String -Pattern "device$" | Select-Object -First 1
if ($dev) {
    Write-Host "    OK - Connected" -ForegroundColor Green
}
else {
    Write-Host "    FAILED - No device!" -ForegroundColor Red
    exit 1
}

# Test 3: Phone to Server
Write-Host "[3] Phone -> Server:" -ForegroundColor Yellow
$test = & $adb shell "curl -s -m 3 http://$ip`:8080/health 2>&1"
if ($test -match "healthy") {
    Write-Host "    OK - Phone CAN connect!" -ForegroundColor Green
}
else {
    Write-Host "    FAILED - Cannot connect!" -ForegroundColor Red
}

# Launch app
Write-Host ""
Write-Host "Launching app on phone..." -ForegroundColor Yellow
& $adb shell "am start -n com.fraudguard.ai/crc642df37a96ad9ab0dc.MainActivity" | Out-Null
Start-Sleep -Seconds 2
Write-Host "App launched!" -ForegroundColor Green

# Instructions
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "TREN DIEN THOAI:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. VAO TAB SETTINGS" -ForegroundColor White
Write-Host "   Nhap IP: $ip" -ForegroundColor Green
Write-Host "   Nhan TEST -> Doi 'Connected'" -ForegroundColor Cyan
Write-Host "   Nhan SAVE -> Doi 'saved'" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. VAO TAB PROTECTION" -ForegroundColor White
Write-Host "   Nhan START PROTECTION" -ForegroundColor Cyan
Write-Host "   Allow Microphone" -ForegroundColor Cyan
Write-Host "   Doi icon XANH" -ForegroundColor Green
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
