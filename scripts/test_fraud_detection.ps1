#!/usr/bin/env pwsh
# Test script to verify fraud detection system
# Usage: .\test_fraud_detection.ps1

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "FraudGuard-AI Test Suite" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$SERVER_IP = "localhost"
$SERVER_PORT = 8080
$DEVICE_ID = "test_device_$(Get-Random -Maximum 9999)"

Write-Host "[INFO] Configuration:" -ForegroundColor Yellow
Write-Host "  Server: $SERVER_IP:$SERVER_PORT"
Write-Host "  Device ID: $DEVICE_ID"
Write-Host ""

# Test 1: Health Check
Write-Host "[TEST 1] Server Health Check" -ForegroundColor Green
try {
    $response = Invoke-WebRequest -Uri "http://${SERVER_IP}:${SERVER_PORT}/health" -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ Server is online" -ForegroundColor Green
    }
} catch {
    Write-Host "  ❌ Server is offline or unreachable" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: Check Fraud Detection Thresholds
Write-Host "[TEST 2] Fraud Detection Configuration" -ForegroundColor Green
Write-Host "  Expected Thresholds:" -ForegroundColor Yellow
Write-Host "    LOW: 20"
Write-Host "    MEDIUM: 40"
Write-Host "    HIGH: 60"
Write-Host "    CRITICAL: 80"
Write-Host ""
Write-Host "  💡 Check backend logs for:" -ForegroundColor Cyan
Write-Host "     '📊 [Config] Fraud detection config loaded'"
Write-Host ""

# Test 3: Fraud Keywords Test
Write-Host "[TEST 3] Fraud Keyword Detection" -ForegroundColor Green
Write-Host "  Testing Vietnamese fraud keywords:" -ForegroundColor Yellow

$fraudKeywords = @(
    @{ Keyword = "chuyển tiền"; Score = 50; Level = "CRITICAL" },
    @{ Keyword = "mã OTP"; Score = 45; Level = "CRITICAL" },
    @{ Keyword = "số tài khoản"; Score = 40; Level = "CRITICAL" },
    @{ Keyword = "công an"; Score = 25; Level = "WARNING" },
    @{ Keyword = "ngân hàng"; Score = 20; Level = "WARNING" },
    @{ Keyword = "gấp lắm"; Score = 25; Level = "SUSPICIOUS" },
    @{ Keyword = "trong 5 phút"; Score = 30; Level = "SUSPICIOUS" }
)

foreach ($item in $fraudKeywords) {
    Write-Host "    • '$($item.Keyword)' → +$($item.Score) points [$($item.Level)]"
}
Write-Host ""

# Test 4: Alert Threshold Scenarios
Write-Host "[TEST 4] Alert Threshold Scenarios" -ForegroundColor Green
Write-Host "  Scenario 1: Single critical keyword" -ForegroundColor Yellow
Write-Host "    Input: 'Anh cần chuyển tiền ngay'"
Write-Host "    Expected: Score = 50 → MEDIUM alert (threshold: 40)"
Write-Host ""

Write-Host "  Scenario 2: Multiple keywords" -ForegroundColor Yellow
Write-Host "    Input: 'Công an yêu cầu chuyển tiền qua số tài khoản'"
Write-Host "    Expected: Score = 50 + 25 + 40 = 115 → CRITICAL alert (threshold: 80)"
Write-Host ""

Write-Host "  Scenario 3: Urgency phrases" -ForegroundColor Yellow
Write-Host "    Input: 'Gấp lắm, trong 5 phút phải chuyển tiền'"
Write-Host "    Expected: Score = 25 + 30 + 50 = 105 → CRITICAL alert"
Write-Host ""

# Test 5: Log Verification Guide
Write-Host "[TEST 5] Log Verification Guide" -ForegroundColor Green
Write-Host "  Backend logs to check:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1️⃣ Fraud Analysis:" -ForegroundColor Cyan
Write-Host "     🔍 [device_id] ===== FRAUD ANALYSIS START ====="
Write-Host "     🔍 [device_id] Input text: 'chuyển tiền'"
Write-Host "     🔴 [device_id] Critical keyword detected: 'chuyển tiền' (+50 points)"
Write-Host "     🔍 [device_id] NEW accumulated score: 50"
Write-Host ""

Write-Host "  2️⃣ Alert Triggering:" -ForegroundColor Cyan
Write-Host "     🚨🚨🚨 [device_id] CRITICAL ALERT TRIGGERED! Score=50"
Write-Host "     📦 [device_id] Alert message created: Type=alert, AlertType=CRITICAL"
Write-Host "     📤 [device_id] Calling sendAlert function..."
Write-Host ""

Write-Host "  3️⃣ WebSocket Delivery:" -ForegroundColor Cyan
Write-Host "     📨 [device_id] ===== SENDING ALERT TO CLIENT ====="
Write-Host "     📝 [device_id] Alert JSON created (XXX bytes)"
Write-Host "     ✅✅✅ [device_id] Alert successfully queued to WebSocket channel"
Write-Host ""

Write-Host "  Mobile logs to check:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1️⃣ Message Reception:" -ForegroundColor Cyan
Write-Host "     [AudioService] ===== PROCESSING SERVER MESSAGE ====="
Write-Host "     [AudioService] ✅ Alert message detected!"
Write-Host ""

Write-Host "  2️⃣ Alert Parsing:" -ForegroundColor Cyan
Write-Host "     [AudioService] Alert parsed:"
Write-Host "       - AlertType: CRITICAL"
Write-Host "       - Confidence: 0.50 (50%)"
Write-Host ""

Write-Host "  3️⃣ UI Update:" -ForegroundColor Cyan
Write-Host "     [MainPage] ===== ALERT RECEIVED EVENT ====="
Write-Host "     [MainPage] 🚨 HIGH RISK ALERT - Triggering high risk handler"
Write-Host "     [MainPage] ✅ Alert handled successfully"
Write-Host ""

# Test 6: Manual Testing Instructions
Write-Host "[TEST 6] Manual Testing Instructions" -ForegroundColor Green
Write-Host ""
Write-Host "  Step 1: Start the backend server" -ForegroundColor Yellow
Write-Host "    cd services/api-gateway"
Write-Host "    go run cmd/main.go"
Write-Host ""

Write-Host "  Step 2: Start the mobile app" -ForegroundColor Yellow
Write-Host "    • Open in Visual Studio"
Write-Host "    • Deploy to Android device/emulator"
Write-Host "    • Enable 'Start Protection'"
Write-Host ""

Write-Host "  Step 3: Simulate fraud call" -ForegroundColor Yellow
Write-Host "    • Speak into phone: 'Anh cần chuyển tiền ngay'"
Write-Host "    • Or use text-to-speech app"
Write-Host ""

Write-Host "  Step 4: Verify alert" -ForegroundColor Yellow
Write-Host "    • Check mobile notification"
Write-Host "    • Check vibration"
Write-Host "    • Check UI alert banner"
Write-Host "    • Check backend logs"
Write-Host ""

# Test 7: Troubleshooting Guide
Write-Host "[TEST 7] Troubleshooting Guide" -ForegroundColor Green
Write-Host ""
Write-Host "  ❌ Problem: No alert triggered" -ForegroundColor Red
Write-Host "    ✅ Solution:" -ForegroundColor Green
Write-Host "      1. Check backend logs for 'FRAUD ANALYSIS START'"
Write-Host "      2. Verify keyword detected: 'Critical keyword detected'"
Write-Host "      3. Check accumulated score vs threshold"
Write-Host "      4. Verify 'Alert successfully queued'"
Write-Host "      5. Check mobile logs for 'PROCESSING SERVER MESSAGE'"
Write-Host ""

Write-Host "  ❌ Problem: Alert delayed (>5 seconds)" -ForegroundColor Red
Write-Host "    ✅ Solution:" -ForegroundColor Green
Write-Host "      1. Check Deepgram API response time"
Write-Host "      2. Look for 'Dropping stale audio buffer' warnings"
Write-Host "      3. Verify network latency"
Write-Host "      4. Check circuit breaker status"
Write-Host ""

Write-Host "  ❌ Problem: Connection drops frequently" -ForegroundColor Red
Write-Host "    ✅ Solution:" -ForegroundColor Green
Write-Host "      1. Check circuit breaker: 'CircuitBreaker:Deepgram OPENED'"
Write-Host "      2. Verify WebSocket ping/pong messages"
Write-Host "      3. Check firewall/NAT settings"
Write-Host "      4. Increase pongWait timeout"
Write-Host ""

# Summary
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Start backend server and mobile app"
Write-Host "  2. Perform manual fraud call test"
Write-Host "  3. Verify logs match expected patterns"
Write-Host "  4. Test with real Vietnamese fraud scenarios"
Write-Host ""
Write-Host "For detailed documentation, see:" -ForegroundColor Cyan
Write-Host "  docs/performance_tuning.md"
Write-Host ""
