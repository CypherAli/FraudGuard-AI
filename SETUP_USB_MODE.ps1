#!/usr/bin/env pwsh
# ============================================
# FraudGuard AI - USB Mode Setup
# ============================================
# Khi cắm USB, chạy script này ĐỂ KHÔNG CẦN NHẬP IP!
# App sẽ connect qua localhost (port forwarding)
# ============================================

Write-Host @"

╔═══════════════════════════════════════════╗
║     USB MODE - TỰ ĐỘNG KẾT NỐI          ║
╚═══════════════════════════════════════════╝

"@ -ForegroundColor Cyan

# ADB Path
$adb = "C:\Users\trinh\AppData\Local\Android\Sdk\platform-tools\adb.exe"

# 1. Kiểm tra device
Write-Host "[1/4] Kiểm tra điện thoại..." -ForegroundColor Yellow
$devices = & $adb devices | Select-String "device$"
if (-not $devices) {
    Write-Host "❌ Không tìm thấy điện thoại!" -ForegroundColor Red
    Write-Host "   Hãy:" -ForegroundColor Yellow
    Write-Host "   - Cắm USB" -ForegroundColor White
    Write-Host "   - Bật USB Debugging" -ForegroundColor White
    Write-Host "   - Chấp nhận popup trên điện thoại" -ForegroundColor White
    exit 1
}
Write-Host "✅ Điện thoại đã kết nối!" -ForegroundColor Green

# 2. Setup ADB Reverse (port forwarding)
Write-Host "`n[2/4] Setup port forwarding..." -ForegroundColor Yellow
$result = & $adb reverse tcp:8080 tcp:8080 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Port forwarding: device:8080 → PC:8080" -ForegroundColor Green
} else {
    Write-Host "⚠️ Warning: $result" -ForegroundColor Yellow
}

# 3. Kiểm tra server đang chạy
Write-Host "`n[3/4] Kiểm tra server..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 2 -UseBasicParsing
    Write-Host "✅ Server đang chạy!" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Server CHƯA CHẠY!" -ForegroundColor Red
    Write-Host "   Chạy lệnh này trong tab khác:" -ForegroundColor Yellow
    Write-Host "   cd E:\FraudGuard-AI\services\api-gateway" -ForegroundColor White
    Write-Host "   go run .\cmd\api\main.go" -ForegroundColor White
    Write-Host "`n   Hoặc:" -ForegroundColor Yellow
    Write-Host "   .\START_SERVER.ps1" -ForegroundColor White
    exit 1
}

# 4. Hướng dẫn sử dụng
Write-Host "`n[4/4] Cách sử dụng:" -ForegroundColor Yellow
Write-Host @"

┌─────────────────────────────────────────┐
│  TRONG APP (Settings tab):             │
├─────────────────────────────────────────┤
│  1. Bật "USB Mode" toggle               │
│  2. IP sẽ tự động = localhost           │
│  3. Nhấn TEST → Success!                │
│  4. Nhấn SAVE                           │
└─────────────────────────────────────────┘

"@ -ForegroundColor White

Write-Host "✅ HOÀN TẤT! App có thể kết nối qua USB!" -ForegroundColor Green
Write-Host "   • Không cần nhập IP thủ công" -ForegroundColor Cyan
Write-Host "   • Tự động dùng localhost" -ForegroundColor Cyan
Write-Host "   • Chạy script này MỖI KHI cắm USB" -ForegroundColor Yellow

# Keep-alive: Giữ port forwarding
Write-Host "`n⏳ Đang giữ kết nối... (Nhấn Ctrl+C để thoát)" -ForegroundColor Yellow
Write-Host "   Port forwarding sẽ tự động hủy khi ngắt USB" -ForegroundColor Gray

# Monitor connection
try {
    while ($true) {
        Start-Sleep -Seconds 5
        $check = & $adb devices | Select-String "device$"
        if (-not $check) {
            Write-Host "`n⚠️ Điện thoại đã ngắt kết nối!" -ForegroundColor Yellow
            break
        }
    }
} catch {
    Write-Host "`n👋 Dừng giám sát" -ForegroundColor Yellow
}

Write-Host "`n✅ Script kết thúc" -ForegroundColor Green
