#!/usr/bin/env pwsh
# setup-ngrok.ps1 - Tạo public tunnel để test mobile app qua internet
#
# Yêu cầu: Backend server đang chạy (scripts/start-server.ps1)

$ErrorActionPreference = "Stop"

Write-Host "`n=== FraudGuard AI - Ngrok Tunnel ===" -ForegroundColor Cyan

# Step 1: Check ngrok installed
Write-Host "`n[1/5] Checking ngrok..." -ForegroundColor Cyan
$ngrokCmd = Get-Command ngrok -ErrorAction SilentlyContinue
if (-not $ngrokCmd) {
    Write-Host "  ngrok not found! Install options:" -ForegroundColor Yellow
    Write-Host "  - Chocolatey: choco install ngrok" -ForegroundColor White
    Write-Host "  - Manual:     https://ngrok.com/download" -ForegroundColor White
    exit 1
}
Write-Host "  Found: $($ngrokCmd.Source)" -ForegroundColor Green

# Step 2: Check auth token
Write-Host "`n[2/5] Checking ngrok auth token..." -ForegroundColor Cyan
$ngrokConfig = "$env:USERPROFILE\.ngrok2\ngrok.yml"
if (-not (Test-Path $ngrokConfig)) {
    Write-Host "  No auth token found!" -ForegroundColor Yellow
    Write-Host "  1. Sign up at: https://dashboard.ngrok.com/signup" -ForegroundColor White
    Write-Host "  2. Get token:  https://dashboard.ngrok.com/get-started/your-authtoken" -ForegroundColor White
    Write-Host "  3. Run: ngrok config add-authtoken YOUR_TOKEN" -ForegroundColor White
    exit 1
}
Write-Host "  Auth token configured" -ForegroundColor Green

# Step 3: Check backend running
Write-Host "`n[3/5] Checking backend on localhost:8080..." -ForegroundColor Cyan
try {
    Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop | Out-Null
    Write-Host "  Backend is running" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Backend not running!" -ForegroundColor Red
    Write-Host "  Start it first: .\scripts\start-server.ps1" -ForegroundColor Yellow
    exit 1
}

# Step 4: Kill old ngrok, start new
Write-Host "`n[4/5] Starting ngrok tunnel..." -ForegroundColor Cyan
Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$ngrokJob = Start-Job -ScriptBlock { ngrok http 8080 --log=stdout }
Start-Sleep -Seconds 3

# Step 5: Get URL
Write-Host "`n[5/5] Retrieving public URL..." -ForegroundColor Cyan
try {
    $info = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels"
    $publicUrl = $info.tunnels | Where-Object { $_.proto -eq "https" } | Select-Object -First 1 -ExpandProperty public_url
    $domain = $publicUrl -replace "https://", ""

    Write-Host "`n=========================================" -ForegroundColor Green
    Write-Host "  TUNNEL READY" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "  HTTPS:     $publicUrl" -ForegroundColor Cyan
    Write-Host "  WebSocket: wss://$domain/ws" -ForegroundColor Yellow
    Write-Host "`n  In mobile app Settings, enter:" -ForegroundColor White
    Write-Host "  $domain" -ForegroundColor White -BackgroundColor DarkGreen
    Write-Host "`n  Dashboard: http://localhost:4040" -ForegroundColor Gray
    Write-Host "  NOTE: Free ngrok URL changes each restart (40 conn/min, 2h limit)" -ForegroundColor Yellow
    Write-Host "`n  Ctrl+C to stop tunnel" -ForegroundColor Red

    while ($true) {
        Start-Sleep -Seconds 5
        if (-not (Get-Process ngrok -ErrorAction SilentlyContinue)) {
            Write-Host "`n  Ngrok stopped unexpectedly!" -ForegroundColor Red; break
        }
    }
} catch {
    Write-Host "  ERROR: Could not get tunnel URL. Check http://localhost:4040" -ForegroundColor Red
} finally {
    Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Remove-Job -Job $ngrokJob -Force -ErrorAction SilentlyContinue
    Write-Host "`n  Tunnel stopped." -ForegroundColor Gray
}
