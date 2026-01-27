# FRAUDGUARD AI - QUICK CONNECTION TEST
# Test nhanh kết nối giữa phone và server

param(
    [switch]$ClearLogs
)

Clear-Host
$adbPath = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$lanIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" }).IPAddress

Write-Host "╔════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  FRAUDGUARD AI - QUICK TEST      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Test 1: Server
Write-Host "[1] Server Check:" -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "http://$lanIP`:8080/health" -TimeoutSec 3 -UseBasicParsing
    Write-Host "    ✓ Server ONLINE at $lanIP" -ForegroundColor Green
}
catch {
    Write-Host "    ✗ Server OFFLINE!" -ForegroundColor Red
    Write-Host "    → Run: .\START_SERVER.ps1" -ForegroundColor Yellow
    exit 1
}

# Test 2: Device
Write-Host ""
Write-Host "[2] Device Check:" -ForegroundColor Yellow
$device = & $adbPath devices | Select-String -Pattern "device$" | Select-Object -First 1
if ($device) {
    Write-Host "    ✓ Device connected: $($device -replace '\s+device$', '')" -ForegroundColor Green
}
else {
    Write-Host "    ✗ No device connected!" -ForegroundColor Red
    exit 1
}

# Test 3: Network from phone
Write-Host ""
Write-Host "[3] Phone → Server:" -ForegroundColor Yellow
$curlTest = & $adbPath shell "curl -s -m 3 http://$lanIP`:8080/health 2>&1"
if ($curlTest -match "healthy") {
    Write-Host "    ✓ Phone CAN connect to server!" -ForegroundColor Green
}
else {
    Write-Host "    ✗ Phone CANNOT connect!" -ForegroundColor Red
    Write-Host "    Response: $curlTest" -ForegroundColor Gray
}

# Test 4: WebSocket
Write-Host ""
Write-Host "[4] WebSocket Check:" -ForegroundColor Yellow
$wsTest = Test-NetConnection -ComputerName $lanIP -Port 8080 -InformationLevel Quiet
if ($wsTest) {
    Write-Host "    ✓ Port 8080 is OPEN" -ForegroundColor Green
}
else {
    Write-Host "    ✗ Port 8080 is CLOSED!" -ForegroundColor Red
}

# Clear logs if requested
if ($ClearLogs) {
    Write-Host ""
    Write-Host "[5] Clearing old logs..." -ForegroundColor Yellow
    & $adbPath logcat -c
    Write-Host "    ✓ Logs cleared" -ForegroundColor Green
}

# Instructions
Write-Host ""
Write-Host "════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📱 TRÊN ĐIỆN THOẠI:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. MỞ APP FraudGuard AI" -ForegroundColor White
Write-Host ""
Write-Host "2. VÀO SETTINGS TAB (⚙️)" -ForegroundColor White
Write-Host "   • Nhập: $lanIP" -ForegroundColor Green
Write-Host "   • Nhấn TEST (đợi 'Connected')" -ForegroundColor Cyan
Write-Host "   • Nhấn SAVE (đợi 'saved')" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. VÀO PROTECTION TAB (🛡️)" -ForegroundColor White  
Write-Host "   • START PROTECTION" -ForegroundColor Cyan
Write-Host "   • Allow Microphone" -ForegroundColor Cyan
Write-Host "   • Đợi icon XANH" -ForegroundColor Green
Write-Host ""
Write-Host "════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Option to capture logs
Write-Host "Muốn capture logs từ app? (y/n): " -ForegroundColor Yellow -NoNewline
$response = Read-Host
if ($response -eq 'y') {
    Write-Host ""
    Write-Host "Đang capture logs trong 20 giây..." -ForegroundColor Cyan
    Write-Host "Hãy thao tác trên app ngay!" -ForegroundColor Yellow
    Start-Sleep -Seconds 20
    
    Write-Host ""
    Write-Host "════ APP LOGS ════" -ForegroundColor Cyan
    & $adbPath logcat -d | Select-String -Pattern "FraudGuard|AudioService|HistoryService|Settings|WebSocket" | Select-Object -Last 30
}
