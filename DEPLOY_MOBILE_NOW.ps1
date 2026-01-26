# ========================================
# QUICK START: CHẠY APP TRÊN ĐIỆN THOẠI
# ========================================

Write-Host "`n==================================" -ForegroundColor Cyan
Write-Host "   FRAUDGUARD AI - MOBILE DEPLOY" -ForegroundColor Cyan
Write-Host "==================================`n" -ForegroundColor Cyan

# Lấy Ngrok URL
$tunnels = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels" -UseBasicParsing
$publicUrl = $tunnels.tunnels[0].public_url
$domain = $publicUrl -replace "https://", ""

Write-Host "Server URL: $domain`n" -ForegroundColor Yellow -BackgroundColor DarkBlue

Write-Host "BƯỚC 1: MỞ VISUAL STUDIO" -ForegroundColor Green
Write-Host "  cd E:\FraudGuard-AI\mobile\FraudGuard-AI" -ForegroundColor Gray
Write-Host "  start FraudGuardAI.sln`n" -ForegroundColor Gray

Write-Host "BƯỚC 2: KẾT NỐI ĐIỆN THOẠI" -ForegroundColor Green
Write-Host "  - Bật USB Debugging trên Android" -ForegroundColor Gray
Write-Host "  - Cắm USB vào máy tính`n" -ForegroundColor Gray

Write-Host "BƯỚC 3: DEPLOY" -ForegroundColor Green
Write-Host "  - Chọn target: Android (không phải emulator)" -ForegroundColor Gray
Write-Host "  - Chọn device của bạn" -ForegroundColor Gray
Write-Host "  - Nhấn F5 hoặc nút Play`n" -ForegroundColor Gray

Write-Host "BƯỚC 4: CẤU HÌNH APP" -ForegroundColor Green
Write-Host "  - Mở app trên điện thoại" -ForegroundColor Gray
Write-Host "  - Tab Settings" -ForegroundColor Gray
Write-Host "  - Nhập Server URL: $domain" -ForegroundColor Yellow
Write-Host "  - Tap Connect`n" -ForegroundColor Gray

Write-Host "BƯỚC 5: TEST" -ForegroundColor Green
Write-Host "  - Tab Protection" -ForegroundColor Gray
Write-Host "  - Gọi số test: 0911333444" -ForegroundColor Red
Write-Host "  - Shield phải chuyển RED và rung`n" -ForegroundColor Gray

Write-Host "==================================`n" -ForegroundColor Cyan

$choice = Read-Host "Mở Visual Studio ngay? (Y/N)"
if ($choice -eq "Y" -or $choice -eq "y") {
    Write-Host "Opening Visual Studio..." -ForegroundColor Yellow
    Start-Process "E:\FraudGuard-AI\mobile\FraudGuard-AI\FraudGuardAI.sln"
}
