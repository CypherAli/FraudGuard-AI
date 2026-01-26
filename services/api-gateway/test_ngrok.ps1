# ============================================
# TEST NGROK CONNECTION
# Quick script to verify tunnel is working
# ============================================

$ErrorActionPreference = "Stop"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  NGROK CONNECTION TEST" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan

# ============================================
# 1. TEST LOCAL BACKEND
# ============================================
Write-Host "`n[1/4] Testing local backend (localhost:8080)..." -ForegroundColor Cyan

try {
    $localResponse = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 5 -UseBasicParsing
    Write-Host " ✅ Local backend is running (Status: $($localResponse.StatusCode))" -ForegroundColor Green
    
    $localJson = $localResponse.Content | ConvertFrom-Json
    Write-Host "     Status: $($localJson.status)" -ForegroundColor Gray
} catch {
    Write-Host " ❌ Local backend is NOT running!" -ForegroundColor Red
    Write-Host "    Start it first: cd services\api-gateway && go run cmd/api/main.go" -ForegroundColor Yellow
    exit 1
}

# ============================================
# 2. CHECK NGROK PROCESS
# ============================================
Write-Host "`n[2/4] Checking ngrok process..." -ForegroundColor Cyan

$ngrokProcess = Get-Process ngrok -ErrorAction SilentlyContinue

if ($ngrokProcess) {
    Write-Host " ✅ Ngrok is running (PID: $($ngrokProcess.Id))" -ForegroundColor Green
} else {
    Write-Host " ❌ Ngrok is NOT running!" -ForegroundColor Red
    Write-Host "    Start it: ngrok http 8080" -ForegroundColor Yellow
    exit 1
}

# ============================================
# 3. GET NGROK PUBLIC URL
# ============================================
Write-Host "`n[3/4] Getting ngrok public URL..." -ForegroundColor Cyan

try {
    $tunnelInfo = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels" -Method Get
    
    $httpsUrl = $tunnelInfo.tunnels | Where-Object { $_.proto -eq "https" } | Select-Object -First 1 -ExpandProperty public_url
    $httpUrl = $tunnelInfo.tunnels | Where-Object { $_.proto -eq "http" } | Select-Object -First 1 -ExpandProperty public_url
    
    if ($httpsUrl) {
        Write-Host " ✅ HTTPS Tunnel: $httpsUrl" -ForegroundColor Green
        $wsUrl = $httpsUrl -replace "https://", "wss://"
        Write-Host " ✅ WebSocket: $wsUrl/ws" -ForegroundColor Green
        
        $publicDomain = $httpsUrl -replace "https://", ""
        
    } else {
        Write-Host " ❌ No HTTPS tunnel found!" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host " ❌ Cannot connect to ngrok API!" -ForegroundColor Red
    Write-Host "    Is ngrok running? Check: http://localhost:4040" -ForegroundColor Yellow
    exit 1
}

# ============================================
# 4. TEST PUBLIC URL
# ============================================
Write-Host "`n[4/4] Testing public URL accessibility..." -ForegroundColor Cyan

try {
    $publicResponse = Invoke-WebRequest -Uri "$httpsUrl/health" -TimeoutSec 10 -UseBasicParsing
    Write-Host " ✅ Public URL is accessible (Status: $($publicResponse.StatusCode))" -ForegroundColor Green
    
    $publicJson = $publicResponse.Content | ConvertFrom-Json
    Write-Host "     Status: $($publicJson.status)" -ForegroundColor Gray
    Write-Host "     Timestamp: $($publicJson.timestamp)" -ForegroundColor Gray
    
} catch {
    Write-Host " ❌ Public URL test failed!" -ForegroundColor Red
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
    exit 1
}

# ============================================
# SUCCESS SUMMARY
# ============================================
Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "  ✅ ALL TESTS PASSED!" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Green

Write-Host "`n📱 MOBILE APP CONFIGURATION:" -ForegroundColor Cyan
Write-Host "`n   Server IP: $publicDomain" -ForegroundColor White -BackgroundColor DarkGreen
Write-Host "`n   Steps:" -ForegroundColor Yellow
Write-Host "   1. Open Mobile App (on 4G)" -ForegroundColor White
Write-Host "   2. Go to Settings tab" -ForegroundColor White
Write-Host "   3. Enter: $publicDomain" -ForegroundColor Yellow
Write-Host "   4. Save and test connection" -ForegroundColor White

Write-Host "`n🔍 MONITORING:" -ForegroundColor Cyan
Write-Host "   Dashboard: http://localhost:4040" -ForegroundColor White
Write-Host "   View real-time requests and responses" -ForegroundColor Gray

Write-Host "`n⚠️  REMEMBER:" -ForegroundColor Yellow
Write-Host "   - Keep ngrok running while using the app" -ForegroundColor Gray
Write-Host "   - Free plan: URL changes every restart" -ForegroundColor Gray
Write-Host "   - Session expires after 2 hours" -ForegroundColor Gray

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  Connection test completed!" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
