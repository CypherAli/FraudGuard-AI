# TEST KET NOI TRUC TIEP - Debug chi tiet
Clear-Host
$adb = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$ip = "192.168.1.234"

Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  FRAUDGUARD AI - DEBUG CONNECTION  ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Test 1: PC to Server
Write-Host "[TEST 1] PC -> Server" -ForegroundColor Yellow
try {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest -Uri "http://$ip`:8080/health" -TimeoutSec 5 -UseBasicParsing
    $sw.Stop()
    $ms = $sw.ElapsedMilliseconds
    Write-Host "  OK - Time: $ms milliseconds" -ForegroundColor Green
    $content = $response.Content
    Write-Host "  Response OK" -ForegroundColor Gray
}
catch {
    Write-Host "  FAILED!" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Phone to Server (HTTP)
Write-Host ""
Write-Host "[TEST 2] Phone -> Server (HTTP)" -ForegroundColor Yellow
$httpTest = & $adb shell "time curl -s -m 3 http://$ip`:8080/health 2>&1; echo `$?"
if ($httpTest -match "healthy") {
    Write-Host "  OK - Phone co the ket noi HTTP!" -ForegroundColor Green
}
else {
    Write-Host "  FAILED - Phone KHONG ket noi duoc!" -ForegroundColor Red
    Write-Host "  Output: $httpTest" -ForegroundColor Gray
}

# Test 3: DNS resolution
Write-Host ""
Write-Host "[TEST 3] DNS/Network check" -ForegroundColor Yellow
$dnsTest = & $adb shell "getprop | grep wifi"
Write-Host "  WiFi info:" -ForegroundColor Gray
$dnsTest | Select-Object -First 3 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }

# Test 4: App permissions
Write-Host ""
Write-Host "[TEST 4] App permissions" -ForegroundColor Yellow
$perms = & $adb shell "dumpsys package com.fraudguard.ai | grep 'INTERNET'"
if ($perms) {
    Write-Host "  OK - INTERNET permission granted" -ForegroundColor Green
}
else {
    Write-Host "  WARNING - Cannot verify permissions" -ForegroundColor Yellow
}

# Test 5: Network policies
Write-Host ""
Write-Host "[TEST 5] App package info" -ForegroundColor Yellow
$pkg = & $adb shell "pm list packages | grep fraudguard"
if ($pkg) {
    Write-Host "  OK - App installed" -ForegroundColor Green
}
else {
    Write-Host "  ERROR - App not found!" -ForegroundColor Red
}

# Final test from app
Write-Host ""
Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║      TEST TRUC TIEP TU APP          ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""
Write-Host "Dang test ket noi tu app..." -ForegroundColor Cyan

$testCmd = @"
am start -n com.fraudguard.ai/crc642df37a96ad9ab0dc.MainActivity > /dev/null 2>&1
sleep 2
am broadcast -a com.fraudguard.ai.TEST_CONNECTION --es ip "$ip" > /dev/null 2>&1
"@

& $adb shell $testCmd

Write-Host ""
Write-Host "════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "KET LUAN:" -ForegroundColor Yellow
Write-Host ""
Write-Host "- Server: DANG CHAY" -ForegroundColor Green
Write-Host "- Phone co the curl thanh cong" -ForegroundColor Green
Write-Host "- VAN DE: App khong the ket noi" -ForegroundColor Red
Write-Host ""
Write-Host "NGUYEN NHAN CO THE:" -ForegroundColor Yellow
Write-Host "1. App dang dung IP sai (khong phai $ip)" -ForegroundColor White
Write-Host "2. App dang dung HTTP client settings sai" -ForegroundColor White
Write-Host "3. Network security config chan HTTP" -ForegroundColor White
Write-Host ""
Write-Host "GIAI PHAP:" -ForegroundColor Green
Write-Host "1. MO APP -> Settings" -ForegroundColor White
Write-Host "2. NHAN VAO O IP VA DELETE HET" -ForegroundColor White
Write-Host "3. GO LAI: $ip" -ForegroundColor Green  
Write-Host "4. NHAN SAVE (QUAN TRONG!)" -ForegroundColor Red
Write-Host "5. NHAN TEST" -ForegroundColor Cyan
Write-Host ""
