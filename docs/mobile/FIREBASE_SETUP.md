# Firebase Phone Authentication Setup Guide

## 📋 Hướng dẫn cấu hình Firebase cho FraudGuard-AI

### Bước 1: Tạo Firebase Project

1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Click **"Add project"** hoặc **"Thêm dự án"**
3. Nhập tên project: `FraudGuard-AI`
4. Tắt Google Analytics (không bắt buộc)
5. Click **"Create project"**

### Bước 2: Thêm Android App vào Firebase Project

1. Trong Firebase Console, click biểu tượng Android
2. Nhập **Android package name**: `com.fraudguard.ai`
   - ⚠️ Phải khớp với `ApplicationId` trong file `FraudGuardAI.csproj`
3. Nhập **App nickname** (tùy chọn): `FraudGuard AI Android`
4. **SHA-1 certificate fingerprint** (BẮT BUỘC cho Phone Auth):
   
   **Cách lấy SHA-1:**
   
   **Windows (PowerShell):**
   ```powershell
   # Debug keystore
   keytool -list -v -keystore "C:\Users\<YourUsername>\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
   
   # Hoặc dùng Java keytool
   & "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe" -list -v -keystore "$env:USERPROFILE\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
   ```
   
   **Tìm dòng:**
   ```
   SHA1: AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD
   ```
   
   Copy SHA-1 và paste vào Firebase Console

5. Click **"Register app"**

### Bước 3: Download google-services.json

1. Click **"Download google-services.json"**
2. Copy file vào thư mục:
   ```
   e:\FraudGuard-AI\mobile\FraudGuard-AI\Platforms\Android\
   ```
3. ✅ File đã tồn tại, hãy **REPLACE** bằng file mới download

### Bước 4: Enable Phone Authentication

1. Trong Firebase Console, vào **Authentication** → **Sign-in method**
2. Click **"Phone"**
3. Toggle **Enable**
4. Click **"Save"**

### Bước 5: Cấu hình Test Phone Numbers (Tùy chọn - cho Development)

Để test mà không tốn SMS quota:

1. Trong **Authentication** → **Sign-in method** → **Phone**
2. Scroll xuống **"Phone numbers for testing"**
3. Thêm số test:
   - Phone number: `+84123456789`
   - Verification code: `123456`
4. Click **"Add"**

⚠️ **Lưu ý:** Test phone numbers chỉ hoạt động trong development, không dùng được trong production.

### Bước 6: Cấu hình SMS Quota (QUAN TRỌNG)

Firebase cung cấp **MIỄN PHÍ** OTP SMS với quota:

#### Free Tier (Spark Plan):
- **10 SMS/ngày** - MIỄN PHÍ HOÀN TOÀN
- Đủ cho development và testing
- Không cần thẻ tín dụng

#### Paid Tier (Blaze Plan):
- **Unlimited SMS** với giá:
  - **$0.01 - $0.06 per SMS** tùy quốc gia
  - Việt Nam: ~$0.02/SMS
- Cần thẻ tín dụng
- Chỉ trả tiền khi vượt quota free

**Để nâng cấp lên Blaze Plan:**
1. Vào **Settings** → **Usage and billing**
2. Click **"Modify plan"**
3. Chọn **"Blaze"**
4. Thêm payment method

### Bước 7: Verify Configuration

1. Build project:
   ```powershell
   cd e:\FraudGuard-AI\mobile\FraudGuard-AI
   dotnet build -f net8.0-android
   ```

2. Run trên emulator hoặc thiết bị thật:
   ```powershell
   dotnet build -t:Run -f net8.0-android
   ```

3. Test flow:
   - Mở app → Màn hình đăng nhập
   - Nhập số điện thoại: `+84xxxxxxxxx`
   - Click "Đăng nhập"
   - Nhận OTP qua SMS
   - Nhập OTP
   - Vào màn hình chính

### Bước 8: Troubleshooting

#### Lỗi: "This app is not authorized to use Firebase Authentication"
**Giải pháp:**
- Kiểm tra `ApplicationId` trong `.csproj` khớp với package name trong Firebase
- Kiểm tra SHA-1 đã được thêm vào Firebase Console
- Rebuild project

#### Lỗi: "SMS quota exceeded"
**Giải pháp:**
- Dùng test phone numbers cho development
- Hoặc nâng cấp lên Blaze Plan

#### Lỗi: "Invalid phone number"
**Giải pháp:**
- Đảm bảo số điện thoại có country code: `+84xxxxxxxxx`
- Không có khoảng trắng hoặc ký tự đặc biệt

#### Lỗi: "Network error"
**Giải pháp:**
- Kiểm tra internet connection
- Kiểm tra `google-services.json` đã được copy đúng vị trí
- Clean và rebuild project

### Bước 9: Production Deployment

Khi deploy production:

1. **Tạo Release Keystore:**
   ```powershell
   keytool -genkey -v -keystore fraudguard-release.keystore -alias fraudguard -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Lấy SHA-1 của Release Keystore:**
   ```powershell
   keytool -list -v -keystore fraudguard-release.keystore -alias fraudguard
   ```

3. **Thêm SHA-1 vào Firebase Console:**
   - Settings → Your apps → Android app
   - Click "Add fingerprint"
   - Paste SHA-1 của release keystore

4. **Build Release APK:**
   ```powershell
   dotnet publish -f net8.0-android -c Release
   ```

### 📊 Chi phí ước tính

**Scenario 1: Development (10 users/day)**
- 10 SMS/day × 30 days = 300 SMS/month
- Cost: **$0** (trong free tier)

**Scenario 2: Small Production (100 users/day)**
- 100 SMS/day × 30 days = 3,000 SMS/month
- Cost: ~**$60/month** ($0.02/SMS)

**Scenario 3: Medium Production (1000 users/day)**
- 1000 SMS/day × 30 days = 30,000 SMS/month
- Cost: ~**$600/month**

**💡 Tip để giảm chi phí:**
- Cache authentication tokens (đã implement)
- Tăng token expiry time
- Implement rate limiting
- Dùng email authentication cho một số users

### ✅ Checklist

- [ ] Tạo Firebase project
- [ ] Thêm Android app với đúng package name
- [ ] Lấy và thêm SHA-1 fingerprint
- [ ] Download và replace `google-services.json`
- [ ] Enable Phone Authentication
- [ ] (Optional) Thêm test phone numbers
- [ ] Build và test trên thiết bị thật
- [ ] Verify OTP được gửi thành công
- [ ] Test login/logout flow
- [ ] (Production) Thêm release keystore SHA-1

### 🎯 Next Steps

Sau khi hoàn thành setup:
1. Test registration flow
2. Test login flow  
3. Test persistent login
4. Test logout
5. Deploy to production

---

**Hỗ trợ:** Nếu gặp vấn đề, check [Firebase Documentation](https://firebase.google.com/docs/auth/android/phone-auth) hoặc [Plugin.Firebase Documentation](https://github.com/TobiasBuchholz/Plugin.Firebase)
