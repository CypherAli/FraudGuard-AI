# Cấu hình Gmail OTP cho FraudGuard AI

## ✅ Đã hoàn thành

Merge nhánh `honqlee-dev` thành công với Gmail OTP authentication sử dụng Brevo API.

### Những gì đã thêm vào:

1. **Email OTP Service** - `BrevoEmailService.cs`
   - Gửi OTP qua Brevo SMTP API
   - HTTP client để gọi api.brevo.com

2. **Configuration Helper** - `AppConfig.cs`
   - Đọc cấu hình từ appsettings.json
   - Cache credentials trong memory

3. **Updated Services**:
   - `EmailOtpAuthService.cs` - Sử dụng Brevo thay vì mock
   - `IAuthenticationService.cs` - Interface mới
   - `SecureStorageService.cs` - Lưu email/OTP

4. **Updated UI Pages**:
   - `LoginPage.xaml/.cs` - Email input
   - `OtpVerificationPage.xaml/.cs` - OTP verification
   - `RegisterPage.xaml/.cs` - Registration flow

5. **Backend giữ nguyên**:
   - `auth_handler.go` - Đã được restore từ main

---

## 🔧 Cấu hình Brevo API

### Bước 1: Tạo file cấu hình

File `appsettings.json` đã được tạo tại:
```
mobile/FraudGuard-AI/Resources/Raw/appsettings.json
```

**Nội dung mẫu:**
```json
{
  "Brevo": {
    "ApiKey": "YOUR_BREVO_API_KEY_HERE",
    "FromEmail": "your-email@gmail.com",
    "FromName": "FraudGuard AI"
  }
}
```

⚠️ **Lưu ý:** Thay `YOUR_BREVO_API_KEY_HERE` bằng API key thực từ Brevo Dashboard.

### Bước 2: Verify Brevo Account

1. Truy cập [Brevo Dashboard](https://app.brevo.com)
2. Kiểm tra email `a2020lehong@gmail.com` đã được verify
3. Kiểm tra API key còn active

### Bước 3: Test Authentication

Khi build app, flow sẽ là:
1. User nhập email → `LoginPage`
2. Brevo gửi OTP qua email → `BrevoEmailService`
3. User nhập OTP → `OtpVerificationPage`
4. Verify thành công → Dashboard

---

## 📊 Git Status

```
Branch: main
Commits ahead of origin: 4

Recent commits:
- a5cec41: docs: Add appsettings template
- cf5a93c: feat: Merge Gmail OTP from honqlee-dev
- (backup: main-backup)
```

---

## 🚀 Bước tiếp theo

### Option 1: Build với Visual Studio
```powershell
# Cần cài Android SDK trước
# Mở FraudGuardAI.csproj trong Visual Studio
# Build và deploy lên Android device/emulator
```

### Option 2: Push lên GitHub
```powershell
git push origin main
```

### Option 3: Rollback nếu cần
```powershell
# Nếu có vấn đề, quay lại main cũ:
git checkout main-backup
```

---

## ⚠️ Lưu ý bảo mật

- ✅ File `appsettings.json` đã được gitignore (không push API key lên GitHub)
- ✅ Template `appsettings.example.json` đã được commit cho người khác tham khảo
- ⚠️ **KHÔNG** share API key công khai

Nếu API key bị lộ:
1. Revoke key cũ tại Brevo Dashboard
2. Tạo key mới
3. Cập nhật lại appsettings.json

---

## 📞 Support

- Backend API: `auth_handler.go` trong `services/api-gateway/`
- Mobile Auth: `EmailOtpAuthService.cs`
- Config: `appsettings.json`

**Merge hoàn tất! 🎉**
