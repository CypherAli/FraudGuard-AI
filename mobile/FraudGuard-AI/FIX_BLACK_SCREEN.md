# 🐛 HƯỚNG DẪN SỬA LỖI MÀN HÌNH ĐEN

## ❓ Vấn đề
- **File đỏ:** `audio_processor.go` có thay đổi chưa commit ✅ **ĐÃ FIX**
- **Màn hình đen:** App khởi động nhưng chỉ hiển thị màn hình đen

## ✅ Đã làm gì

### 1. Commit file đỏ
```bash
git commit -m "♻️ Refactor: Move buffer pool comment to correct location"
```

### 2. Thêm Error Handling vào App.xaml.cs
- ✅ Try-catch trong constructor để catch crash
- ✅ Tạo Error Page thay vì màn hình đen
- ✅ Thêm debug logs để track initialization

## 🔍 Cách Debug Màn Hình Đen

### Phương pháp 1: Chạy script debug (KHUYẾN NGHỊ)

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
.\DEBUG_BLACK_SCREEN.ps1
```

Script này sẽ:
1. Clear app data cũ
2. Install APK mới
3. Capture logcat để xem crash log
4. Lưu log vào file `debug_black_screen_YYYYMMDD_HHmmss.log`

### Phương pháp 2: Debug thủ công

```powershell
# 1. Kết nối điện thoại qua USB
adb devices

# 2. Clear app data cũ
adb shell pm clear com.fraudguard.ai

# 3. Install APK mới
adb install -r bin\Release\net8.0-android\com.fraudguard.ai-Signed.apk

# 4. Xem logcat khi launch app
adb logcat | Select-String "fraudguard|FATAL|crash|Exception"
```

## 🔧 Các nguyên nhân tiềm ẩn

### A. Firebase Initialization Error
**Triệu chứng:** Logcat hiển thị lỗi Firebase
**Nguyên nhân:** `google-services.json` không đúng hoặc Firebase không init
**Giải pháp:**
```powershell
# Kiểm tra file Firebase config
Test-Path Platforms\Android\google-services.json
```

### B. Service Injection Error  
**Triệu chứng:** `_authService` is null, không thể login
**Nguyên nhân:** `MauiProgram.cs` không register service đúng
**Giải pháp:** Kiểm tra [MauiProgram.cs](MauiProgram.cs#L23-L26)

### C. XAML Parse Error
**Triệu chứng:** Màn hình đen, không crash log rõ ràng
**Nguyên nhân:** XAML có lỗi syntax
**Giải pháp:** Build lại và xem build errors

### D. App Permissions
**Triệu chứng:** App không khởi động sau install
**Nguyên nhân:** Missing permissions Android 13+
**Giải pháp:** Check `AndroidManifest.xml`

## 📊 Thông tin APK hiện tại

- **File:** `com.fraudguard.ai-Signed.apk`
- **Size:** 139 MB
- **Build time:** 11:18 AM (Hôm nay)
- **Location:** `bin\Release\net8.0-android\`

## 🚀 Build lại app với fix mới

```powershell
# Clean build để apply fix error handling
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
.\BUILD_APK_FIX_CRASH.ps1
```

## 📱 Test flow

1. **Uninstall app cũ** (nếu có):
   ```powershell
   adb uninstall com.fraudguard.ai
   ```

2. **Install APK mới**:
   ```powershell
   adb install bin\Release\net8.0-android\com.fraudguard.ai-Signed.apk
   ```

3. **Launch app** trên điện thoại

4. **Quan sát:**
   - ✅ **Thành công:** Hiển thị LoginPage với logo 🛡️
   - ❌ **Vẫn đen:** Xem script debug ở trên
   - ⚠️ **Error page:** Hiện lỗi cụ thể (có nút "Khởi động lại")

## 🆘 Nếu vẫn lỗi

1. **Xem log file:** Mở `debug_black_screen_*.log`
2. **Tìm FATAL EXCEPTION:**
   ```powershell
   Select-String "FATAL" debug_black_screen_*.log
   ```
3. **Copy error message** và báo lại để fix tiếp

## 📝 Checklist

- [x] ✅ Commit file đỏ (audio_processor.go)
- [x] ✅ Thêm error handling vào App.xaml.cs
- [x] ✅ Tạo Error Page thay vì màn hình đen
- [x] ✅ Tạo DEBUG_BLACK_SCREEN.ps1
- [ ] ⏳ Build lại app
- [ ] ⏳ Test trên điện thoại thật
- [ ] ⏳ Xem logcat để tìm root cause

## 💡 Tips

- **Luôn xem logcat** khi debug màn hình đen
- **Clear app data** trước mỗi lần test
- **Build Release APK** để test chính xác
- **Không build Debug** vì sẽ gặp linking errors

---

**Created:** 2026-02-10
**Last Updated:** 2026-02-10
**Status:** Waiting for test results
