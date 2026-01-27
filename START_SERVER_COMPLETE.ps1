# ============================================
# FRAUDGUARD AI - COMPLETE SERVER STARTUP
# ============================================

Write-Host "`n" -NoNewline
Write-Host "███████╗██████╗  █████╗ ██╗   ██╗██████╗  ██████╗ ██╗   ██╗ █████╗ ██████╗ ██████╗ " -ForegroundColor Cyan
Write-Host "██╔════╝██╔══██╗██╔══██╗██║   ██║██╔══██╗██╔════╝ ██║   ██║██╔══██╗██╔══██╗██╔══██╗" -ForegroundColor Cyan
Write-Host "█████╗  ██████╔╝███████║██║   ██║██║  ██║██║  ███╗██║   ██║███████║██████╔╝██║  ██║" -ForegroundColor Cyan
Write-Host "██╔══╝  ██╔══██╗██╔══██║██║   ██║██║  ██║██║   ██║██║   ██║██╔══██║██╔══██╗██║  ██║" -ForegroundColor Cyan
Write-Host "██║     ██║  ██║██║  ██║╚██████╔╝██████╔╝╚██████╔╝╚██████╔╝██║  ██║██║  ██║██████╔╝" -ForegroundColor Cyan
Write-Host "╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝  ╚═════╝  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ " -ForegroundColor Cyan
Write-Host ""
Write-Host "AI-Powered Fraud Detection System" -ForegroundColor Yellow
Write-Host "====================================`n" -ForegroundColor Yellow

# Step 1: Check Docker
Write-Host "[1/4] Checking Docker..." -ForegroundColor Cyan
try {
    docker ps | Out-Null
    Write-Host "  ✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Docker is NOT running!" -ForegroundColor Red
    Write-Host "  → Please start Docker Desktop first" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 2: Check/Start Database
Write-Host "`n[2/4] Checking PostgreSQL Database..." -ForegroundColor Cyan
$dbCheck = docker ps --filter "name=fraudguard" --format "{{.Names}}"

if ($dbCheck -match "fraudguard-db") {
    Write-Host "  OK Database is already running" -ForegroundColor Green
} 
else {
    Write-Host "  -> Starting database container..." -ForegroundColor Yellow
    Set-Location E:\FraudGuard-AI\services\api-gateway
    docker-compose up -d
    Start-Sleep -Seconds 5
    Write-Host "  OK Database started" -ForegroundColor Green
}

# Step 3: Get LAN IP
Write-Host "`n[3/4] Detecting your LAN IP address..." -ForegroundColor Cyan
try {
    $lanIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" } | Select-Object -First 1).IPAddress
}
catch {
    $lanIP = $null
}

if ($lanIP) {
    Write-Host "  OK Your LAN IP: $lanIP" -ForegroundColor Green
    Write-Host "  -> Use this IP in your mobile app Settings" -ForegroundColor Yellow
} 
else {
    $lanIP = "192.168.1.234"
    Write-Host "  ! Could not detect LAN IP, using default: $lanIP" -ForegroundColor Yellow
}

# Step 4: Start API Server
Write-Host "`n[4/4] Starting API Server..." -ForegroundColor Cyan
Set-Location E:\FraudGuard-AI\services\api-gateway

Write-Host "  -> Server will listen on:" -ForegroundColor Yellow
Write-Host "    - Local:   http://localhost:8080" -ForegroundColor White
Write-Host "    - Network: http://$lanIP:8080" -ForegroundColor White
Write-Host "    - WebSocket: ws://$lanIP:8080/ws" -ForegroundColor White
Write-Host ""

# Start server
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "SERVER IS STARTING..." -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

go run ./cmd/main.go
