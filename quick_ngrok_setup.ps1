# INTERACTIVE NGROK SETUP
# This script will help you configure and start ngrok tunnel

$ngrokPath = "$env:LOCALAPPDATA\ngrok\ngrok.exe"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  FRAUDGUARD AI - NGROK SETUP" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan

# Check if auth token is configured
$configPath = "$env:USERPROFILE\.ngrok2\ngrok.yml"

if (Test-Path $configPath) {
    Write-Host "`nAuth token: CONFIGURED" -ForegroundColor Green
} else {
    Write-Host "`nAuth token: NOT CONFIGURED" -ForegroundColor Yellow
    Write-Host "`nYou need to configure your auth token first." -ForegroundColor White
    Write-Host "Get it from: https://dashboard.ngrok.com/get-started/your-authtoken" -ForegroundColor Cyan
    
    Write-Host "`nOptions:" -ForegroundColor Yellow
    Write-Host "  1. I have my token ready (paste it now)" -ForegroundColor White
    Write-Host "  2. Open website to get token" -ForegroundColor White
    Write-Host "  3. Exit and configure manually" -ForegroundColor White
    
    $choice = Read-Host "`nEnter choice (1/2/3)"
    
    if ($choice -eq "1") {
        $token = Read-Host "`nPaste your auth token here"
        if ($token) {
            Write-Host "`nConfiguring token..." -ForegroundColor Cyan
            & $ngrokPath config add-authtoken $token
            Write-Host "Token configured successfully!" -ForegroundColor Green
        } else {
            Write-Host "No token provided. Exiting..." -ForegroundColor Red
            exit 1
        }
    } elseif ($choice -eq "2") {
        Write-Host "`nOpening website..." -ForegroundColor Cyan
        Start-Process "https://dashboard.ngrok.com/get-started/your-authtoken"
        Write-Host "After getting your token, run this command:" -ForegroundColor Yellow
        Write-Host "  ngrok config add-authtoken YOUR_TOKEN" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Host "Exiting... Run 'ngrok config add-authtoken YOUR_TOKEN' manually" -ForegroundColor Yellow
        exit 0
    }
}

# Check backend
Write-Host "`nChecking backend server..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 3 -UseBasicParsing
    Write-Host "Backend: RUNNING on port 8080" -ForegroundColor Green
} catch {
    Write-Host "Backend: NOT RUNNING" -ForegroundColor Red
    Write-Host "Please start backend first in another terminal:" -ForegroundColor Yellow
    Write-Host "  cd E:\FraudGuard-AI\services\api-gateway" -ForegroundColor Gray
    Write-Host "  go run cmd\api\main.go" -ForegroundColor Gray
    exit 1
}

# Start ngrok
Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "  STARTING NGROK TUNNEL" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Green

Write-Host "`nStarting tunnel to localhost:8080..." -ForegroundColor Cyan
Write-Host "Dashboard will be available at: http://localhost:4040" -ForegroundColor Gray
Write-Host "`nPress Ctrl+C to stop the tunnel" -ForegroundColor Yellow
Write-Host "`n"

# Start ngrok and keep it running
& $ngrokPath http 8080
