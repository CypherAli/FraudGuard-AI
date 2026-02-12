# FRAUDGUARD AI - Simple Server Startup Script
# Version: 1.0

Clear-Host
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " FRAUDGUARD AI - SERVER STARTUP" -ForegroundColor Cyan  
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check Docker
Write-Host "[1/4] Checking Docker..." -ForegroundColor Yellow
try {
    $dockerCheck = docker ps 2>&1
    Write-Host "  [OK] Docker is running" -ForegroundColor Green
} 
catch {
    Write-Host "  [ERROR] Docker is NOT running!" -ForegroundColor Red
    Write-Host "  Please start Docker Desktop first" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 2: Check Database
Write-Host ""
Write-Host "[2/4] Checking PostgreSQL Database..." -ForegroundColor Yellow
$dbCheck = docker ps --filter "name=fraudguard" --format "{{.Names}}"

if ($dbCheck -match "fraudguard-db") {
    Write-Host "  [OK] Database is already running" -ForegroundColor Green
} 
else {
    Write-Host "  Starting database container..." -ForegroundColor Cyan
    Set-Location E:\FraudGuard-AI\services\api-gateway
    docker-compose up -d
    Start-Sleep -Seconds 5
    Write-Host "  [OK] Database started" -ForegroundColor Green
}

# Step 3: Get LAN IP
Write-Host ""
Write-Host "[3/4] Detecting your LAN IP address..." -ForegroundColor Yellow
try {
    $lanIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" } | Select-Object -First 1).IPAddress
}
catch {
    $lanIP = $null
}

if ($lanIP) {
    Write-Host "  [OK] Your LAN IP: $lanIP" -ForegroundColor Green
    Write-Host "  Use this IP in your mobile app Settings!" -ForegroundColor Cyan
} 
else {
    $lanIP = "192.168.1.234"
    Write-Host "  [WARNING] Could not detect LAN IP" -ForegroundColor Yellow
    Write-Host "  Using default: $lanIP" -ForegroundColor Yellow
}

# Step 4: Start Server
Write-Host ""
Write-Host "[4/4] Starting API Server..." -ForegroundColor Yellow
Set-Location E:\FraudGuard-AI\services\api-gateway

Write-Host ""
Write-Host "  Server will listen on:" -ForegroundColor Cyan
Write-Host "    - Local:    http://localhost:8080" -ForegroundColor White
Write-Host "    - Network:  http://$lanIP:8080" -ForegroundColor White
Write-Host "    - WebSocket: ws://$lanIP:8080/ws" -ForegroundColor White
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " SERVER IS STARTING..." -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

$env:CGO_ENABLED = "0"
go run cmd\api\main.go
