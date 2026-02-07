# ===================================================================
# BUILD APK - FIX CRASH VERSION
# Script build APK với các fix để tránh crash khi khởi động
# ===================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BUILD APK - CRASH FIX VERSION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Đường dẫn project
$projectPath = "E:\FraudGuard-AI\mobile\FraudGuard-AI"
Set-Location $projectPath

Write-Host "[1/6] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Debug

Write-Host ""
Write-Host "[2/6] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

Write-Host ""
Write-Host "[3/6] Building Debug APK..." -ForegroundColor Yellow
Write-Host "Note: Debug build is faster and includes logging for troubleshooting" -ForegroundColor Gray

# Build with detailed verbosity to catch errors
dotnet build -f net8.0-android -c Debug --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ BUILD FAILED!" -ForegroundColor Red
    Write-Host "Check the errors above" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[4/6] Locating APK file..." -ForegroundColor Yellow

# Tìm file APK
$apkPath = Get-ChildItem -Path "bin\Debug\net8.0-android" -Filter "*.apk" -Recurse | Select-Object -First 1

if ($null -eq $apkPath) {
    Write-Host "❌ APK file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ APK found: $($apkPath.FullName)" -ForegroundColor Green
Write-Host "   Size: $([math]::Round($apkPath.Length / 1MB, 2)) MB" -ForegroundColor Gray

Write-Host ""
Write-Host "[5/6] Copying to easy-to-find location..." -ForegroundColor Yellow

# Copy APK ra thư mục gốc với tên dễ nhận
$outputPath = "$projectPath\FraudGuard-AI-FIXED.apk"
Copy-Item $apkPath.FullName $outputPath -Force

Write-Host "✅ APK copied to: $outputPath" -ForegroundColor Green

Write-Host ""
Write-Host "[6/6] Generating installation instructions..." -ForegroundColor Yellow

# Tạo file hướng dẫn cài đặt
$instructions = @"
📱 HƯỚNG DẪN CÀI ĐẶT APK - FIXED VERSION
========================================

File APK: FraudGuard-AI-FIXED.apk
Build time: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")

🔧 CÁC FIX ĐÃ THỰC HIỆN:
✅ Thêm try-catch toàn diện để tránh crash
✅ Khởi tạo Firebase đúng cách với error handling
✅ Xử lý lỗi gracefully thay vì crash
✅ Thêm error page khi có vấn đề
✅ Logging chi tiết để debug

📲 CÁCH CÀI ĐẶT:

1. Gỡ phiên bản cũ (nếu có):
   Settings → Apps → FraudGuard AI → Uninstall

2. Bật cài đặt từ nguồn không xác định:
   Settings → Security → Install unknown apps
   → Chọn trình duyệt/File Manager → Bật ON

3. Tải file FraudGuard-AI-FIXED.apk về điện thoại

4. Mở file APK và nhấn Install

5. Mở app lần đầu:
   - Nếu bị crash, kiểm tra LogCat (xem bên dưới)
   - App sẽ hiển thị error page nếu có vấn đề
   - Nhấn "Thử lại" nếu thấy error page

6. Vào Settings tab:
   - Tắt "USB Mode" 
   - Nhập Server URL: https://fraudguard-ai-jljl.onrender.com
   - Nhấn "Test Connection"
   - Nhấn "Save"

7. Cấp quyền khi được hỏi:
   - Microphone (BẮT BUỘC)
   - Notifications (Khuyến nghị)

🐛 NẾU VẪN BỊ CRASH:

1. Kiểm tra LogCat:
   - Kết nối điện thoại qua USB
   - Chạy: adb logcat | Select-String "FraudGuard"

2. Các lỗi phổ biến:

   ❌ Firebase initialization failed
   → Kiểm tra file google-services.json
   → SHA-1 fingerprint đã đăng ký chưa?

   ❌ Native library not found
   → Build lại: dotnet clean → dotnet build
   → Kiểm tra AndroidManifest.xml

   ❌ Java.Lang.ClassNotFoundException
   → ProGuard conflict - đã disable trong .csproj
   → Kiểm tra PackageReferences version

3. Gửi log cho developer:
   adb logcat -d > crash_log.txt

📞 HỖ TRỢ:
- Check file: FIREBASE_SETUP.md
- Check file: REBUILD_GUIDE.md
- View logs trong app với adb logcat

🎯 TEST APP:
1. Mở app → Vào Settings
2. Test Connection → phải thấy "✅ Đã kết nối"
3. Vào Protection → Start Protection
4. Gọi điện thoại → App phải bắt đầu ghi âm

"@

$instructionsPath = "$projectPath\INSTALL_INSTRUCTIONS.txt"
$instructions | Out-File -FilePath $instructionsPath -Encoding UTF8

Write-Host "✅ Instructions saved to: $instructionsPath" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ BUILD COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 APK Location:" -ForegroundColor White
Write-Host "   $outputPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "📄 Installation Guide:" -ForegroundColor White
Write-Host "   $instructionsPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "📱 Next Steps:" -ForegroundColor White
Write-Host "   1. Copy APK to your phone" -ForegroundColor Gray
Write-Host "   2. Install and test" -ForegroundColor Gray
Write-Host "   3. Check logs if crash: adb logcat | Select-String 'FraudGuard'" -ForegroundColor Gray
Write-Host ""

# Mở folder chứa APK
explorer.exe $projectPath
