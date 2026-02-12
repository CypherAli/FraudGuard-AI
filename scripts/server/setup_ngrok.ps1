# ============================================
# NGROK SETUP & LAUNCH SCRIPT
# FraudGuard AI - Public Internet Tunnel
# ============================================

$ErrorActionPreference = "Stop"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  FRAUDGUARD AI - NGROK TUNNEL SETUP" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan

# Check if running from correct directory
$currentPath = Get-Location
if (-not (Test-Path ".\cmd\api\main.go")) {
    Write-Host "`n ERROR: Must run from api-gateway directory!" -ForegroundColor Red
    Write-Host " Current: $currentPath" -ForegroundColor Yellow
    Write-Host " Expected: E:\FraudGuard-AI\services\api-gateway" -ForegroundColor Yellow
    exit 1
}

# ============================================
# STEP 1: CHECK IF NGROK IS INSTALLED
# ============================================
Write-Host "`n[1/5] Checking Ngrok installation..." -ForegroundColor Cyan

$ngrokPath = Get-Command ngrok -ErrorAction SilentlyContinue

if (-not $ngrokPath) {
    Write-Host " Ngrok not found! Installing via Chocolatey..." -ForegroundColor Yellow
    
    # Check if Chocolatey is installed
    $chocoPath = Get-Command choco -ErrorAction SilentlyContinue
    
    if (-not $chocoPath) {
        Write-Host "`n OPTION 1: Install Chocolatey first:" -ForegroundColor Yellow
        Write-Host "   Run PowerShell as Administrator and execute:" -ForegroundColor White
        Write-Host "   Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))" -ForegroundColor Gray
        Write-Host "`n OPTION 2: Manual Download:" -ForegroundColor Yellow
        Write-Host "   1. Download from: https://ngrok.com/download" -ForegroundColor White
        Write-Host "   2. Extract to: C:\Program Files\ngrok\" -ForegroundColor White
        Write-Host "   3. Add to PATH environment variable" -ForegroundColor White
        Write-Host "   4. Restart PowerShell and run this script again" -ForegroundColor White
        exit 1
    }
    
    Write-Host " Installing ngrok via Chocolatey..." -ForegroundColor Gray
    choco install ngrok -y
    
    # Refresh environment
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    
    Write-Host " Ngrok installed successfully!" -ForegroundColor Green
} else {
    Write-Host " Ngrok found at: $($ngrokPath.Source)" -ForegroundColor Green
}

# ============================================
# STEP 2: CHECK NGROK AUTH TOKEN
# ============================================
Write-Host "`n[2/5] Checking Ngrok authentication..." -ForegroundColor Cyan

$ngrokConfigPath = "$env:USERPROFILE\.ngrok2\ngrok.yml"

if (Test-Path $ngrokConfigPath) {
    Write-Host " Ngrok config found: $ngrokConfigPath" -ForegroundColor Green
} else {
    Write-Host " No Ngrok auth token configured!" -ForegroundColor Yellow
    Write-Host "`n TO GET YOUR AUTH TOKEN:" -ForegroundColor Yellow
    Write-Host "   1. Sign up FREE at: https://dashboard.ngrok.com/signup" -ForegroundColor White
    Write-Host "   2. Go to: https://dashboard.ngrok.com/get-started/your-authtoken" -ForegroundColor White
    Write-Host "   3. Copy your authtoken" -ForegroundColor White
    Write-Host "   4. Run: ngrok config add-authtoken YOUR_TOKEN_HERE" -ForegroundColor White
    Write-Host "`n Example:" -ForegroundColor Gray
    Write-Host "   ngrok config add-authtoken 2abcdefghijk123456789_abcdefghijklmnopqrstuvwxyz" -ForegroundColor Gray
    
    $response = Read-Host "`n Do you have your auth token ready? (y/n)"
    
    if ($response -eq 'y') {
        $authToken = Read-Host " Enter your Ngrok auth token"
        
        Write-Host " Configuring Ngrok..." -ForegroundColor Gray
        ngrok config add-authtoken $authToken
        
        Write-Host " Auth token configured!" -ForegroundColor Green
    } else {
        Write-Host "`n Please configure Ngrok auth token first and run this script again." -ForegroundColor Yellow
        exit 0
    }
}

# ============================================
# STEP 3: CHECK IF BACKEND IS RUNNING
# ============================================
Write-Host "`n[3/5] Checking if Backend is running on localhost:8080..." -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
    Write-Host " Backend is RUNNING (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host " Backend is NOT running!" -ForegroundColor Red
    Write-Host "`n TO START BACKEND:" -ForegroundColor Yellow
    Write-Host "   1. Open a NEW PowerShell window" -ForegroundColor White
    Write-Host "   2. Navigate to: E:\FraudGuard-AI\services\api-gateway" -ForegroundColor White
    Write-Host "   3. Run: go run cmd/api/main.go" -ForegroundColor White
    Write-Host "`n After backend is running, run this script again." -ForegroundColor Yellow
    exit 1
}

# ============================================
# STEP 4: START NGROK TUNNEL
# ============================================
Write-Host "`n[4/5] Starting Ngrok tunnel..." -ForegroundColor Cyan

Write-Host " Starting tunnel to localhost:8080..." -ForegroundColor Gray
Write-Host " Press Ctrl+C to stop the tunnel" -ForegroundColor Yellow

# Kill any existing ngrok processes
Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Start ngrok in background and capture output
$ngrokJob = Start-Job -ScriptBlock {
    ngrok http 8080 --log=stdout
}

# Wait for tunnel to be established
Write-Host " Waiting for tunnel to establish..." -ForegroundColor Gray
Start-Sleep -Seconds 3

# ============================================
# STEP 5: GET PUBLIC URL
# ============================================
Write-Host "`n[5/5] Retrieving public URL..." -ForegroundColor Cyan

try {
    # Get tunnel info from ngrok API
    $tunnelInfo = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels" -Method Get
    
    $publicUrl = $tunnelInfo.tunnels | Where-Object { $_.proto -eq "https" } | Select-Object -First 1 -ExpandProperty public_url
    
    if ($publicUrl) {
        $wsUrl = $publicUrl -replace "https://", "wss://"
        
        Write-Host "`n=========================================" -ForegroundColor Green
        Write-Host "  TUNNEL ESTABLISHED SUCCESSFULLY!" -ForegroundColor White
        Write-Host "=========================================" -ForegroundColor Green
        
        Write-Host "`n HTTP URL:  $publicUrl" -ForegroundColor Cyan
        Write-Host " WebSocket: $wsUrl/ws" -ForegroundColor Yellow
        
        Write-Host "`n=========================================" -ForegroundColor Magenta
        Write-Host "  MOBILE APP CONFIGURATION" -ForegroundColor White
        Write-Host "=========================================" -ForegroundColor Magenta
        
        # Extract just the domain without protocol
        $domain = $publicUrl -replace "https://", "" -replace "http://", ""
        
        Write-Host "`n In Mobile App Settings Tab, enter:" -ForegroundColor Yellow
        Write-Host "   Server IP: $domain" -ForegroundColor White -BackgroundColor DarkGreen
        Write-Host "   (No need to include https:// or port)" -ForegroundColor Gray
        
        Write-Host "`n=========================================" -ForegroundColor Cyan
        Write-Host "  TEST YOUR SETUP" -ForegroundColor White
        Write-Host "=========================================" -ForegroundColor Cyan
        
        Write-Host "`n 1. Open Mobile App on your phone (using 4G, not WiFi)" -ForegroundColor White
        Write-Host " 2. Go to Settings tab" -ForegroundColor White
        Write-Host " 3. Enter: $domain" -ForegroundColor Yellow
        Write-Host " 4. Tap 'Save Settings'" -ForegroundColor White
        Write-Host " 5. Go to Protection tab and tap 'Start Listening'" -ForegroundColor White
        
        Write-Host "`n Dashboard: http://localhost:4040" -ForegroundColor Cyan
        Write-Host " (View real-time traffic and debugging info)" -ForegroundColor Gray
        
        Write-Host "`n=========================================" -ForegroundColor Red
        Write-Host "  IMPORTANT NOTES" -ForegroundColor White
        Write-Host "=========================================" -ForegroundColor Red
        
        Write-Host "`n Free Ngrok Limitations:" -ForegroundColor Yellow
        Write-Host "   - URL changes every time you restart" -ForegroundColor Gray
        Write-Host "   - 40 connections/minute limit" -ForegroundColor Gray
        Write-Host "   - Session expires after 2 hours" -ForegroundColor Gray
        
        Write-Host "`n Keep this window OPEN while using the app!" -ForegroundColor Red
        Write-Host " Press Ctrl+C to stop the tunnel" -ForegroundColor Yellow
        
        # Keep monitoring
        Write-Host "`n Tunnel is running... (monitoring)" -ForegroundColor Green
        
        while ($true) {
            Start-Sleep -Seconds 5
            
            # Check if ngrok is still running
            $ngrokProcess = Get-Process ngrok -ErrorAction SilentlyContinue
            
            if (-not $ngrokProcess) {
                Write-Host "`n Ngrok process stopped unexpectedly!" -ForegroundColor Red
                break
            }
        }
        
    } else {
        Write-Host " Failed to retrieve public URL!" -ForegroundColor Red
        Write-Host " Check ngrok dashboard at: http://localhost:4040" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host " Failed to connect to ngrok API!" -ForegroundColor Red
    Write-Host " Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "`n Try opening: http://localhost:4040" -ForegroundColor Cyan
}

# Cleanup on exit
Write-Host "`n Stopping ngrok..." -ForegroundColor Yellow
Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Job -Job $ngrokJob -Force -ErrorAction SilentlyContinue

Write-Host " Tunnel stopped." -ForegroundColor Gray
