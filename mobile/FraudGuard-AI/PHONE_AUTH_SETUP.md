# 🔐 Hướng dẫn Setup Firebase Phone Authentication

## ✅ Checklist Setup (Quan trọng!)

### 1️⃣ **Thêm SHA-1 và SHA-256 vào Firebase**

```powershell
# Chạy script để lấy fingerprints
cd mobile\FraudGuard-AI
.\get_sha1_keys.ps1
```

**Kết quả:**
```
SHA-1: 21:56:A3:0D:E9:61:23:C7:F6:B9:D6:32:53:30:FD:81:4E:45:1F:87
SHA-256: 5F:80:48:F4:6B:C0:2F:82:17:FC:54:C1:E7:8E:A4:CD:A6:C8:51:D7:EA:63:43:E7:5B:92:53:A4:F8:48:50:EC
```

**Thêm vào Firebase Console:**

1. Mở https://console.firebase.google.com
2. **Project Settings** → **Your apps** → Android app (`com.fraudguard.ai`)
3. Kéo xuống **"SHA certificate fingerprints"**
4. **Click "Add fingerprint"**:
   - Thêm **SHA-1** (bắt buộc cho reCAPTCHA)
   - Thêm **SHA-256** (bắt buộc cho Play Integrity API)
5. **Click "Save"**

---

### 2️⃣ **Kích hoạt Phone Authentication**

1. Vào **Firebase Console** → **Authentication**
2. Tab **"Sign-in method"**
3. **Bật "Phone"** provider
4. Click **"Save"**

---

### 3️⃣ **Cấu hình vùng (Optional - khuyến nghị)**

Để tránh lạm dụng SMS:

1. Vào **Authentication** → **Settings** tab
2. Mục **"SMS regions"**
3. **Chọn "Allow specific regions"**
4. Chỉ chọn: **Vietnam (+84)**
5. Click **"Save"**

---

### 4️⃣ **Tải lại google-services.json**

Sau khi thêm SHA-1/SHA-256:

1. Firebase Console → **Project Settings**
2. Cuộn xuống Android app
3. Click icon ⚙️ → **"Download google-services.json"**
4. Copy file mới vào: `mobile\FraudGuard-AI\Platforms\Android\`
5. **Thay thế file cũ**

---

### 5️⃣ **Xác minh Package Name**

Đảm bảo khớp 100%:

**Trong Firebase:**
```
com.fraudguard.ai
```

**Trong FraudGuardAI.csproj:**
```xml
<ApplicationId>com.fraudguard.ai</ApplicationId>
```

✅ **Đã khớp!**

---

## 🔥 Cách hoạt động của Firebase Phone Auth

### **Quy trình xác thực:**

```
1. User nhập số điện thoại (+84xxxxxxxxx)
   ↓
2. App gửi yêu cầu đến Firebase
   ↓
3. Firebase xác thực app bằng:
   • Play Integrity API (nếu có Google Play Services)
   • reCAPTCHA (fallback nếu không có Play Services)
   ↓
4. Firebase gửi SMS chứa mã OTP 6 chữ số
   ↓
5. User nhập mã OTP
   ↓
6. App gửi mã để xác thực
   ↓
7. Firebase trả về Firebase User Token
   ↓
8. User đã đăng nhập thành công! ✅
```

---

## 📱 Test Phone Authentication

### **Sử dụng số điện thoại test (không tốn SMS):**

1. Vào **Firebase Console** → **Authentication**
2. Tab **"Sign-in method"** → **Phone**
3. Mở accordion **"Phone numbers for testing"**
4. Thêm số test:
   ```
   Phone: +84 650-555-3434
   Code: 654321
   ```
5. Click **"Add"**

**Lưu ý:**
- ✅ Không gửi SMS thật (miễn phí)
- ✅ Không giới hạn số lần test
- ✅ Dùng cho development/testing
- ⚠️ **Phải xóa trước khi release production!**

---

## 🛠️ Troubleshooting

### ❌ **Lỗi: "This app is not authorized to use Firebase Authentication"**

**Nguyên nhân:** SHA-1 hoặc SHA-256 chưa được thêm vào Firebase

**Giải pháp:**
1. Chạy `.\get_sha1_keys.ps1` để lấy fingerprints
2. Thêm cả SHA-1 và SHA-256 vào Firebase Console
3. Tải lại `google-services.json`
4. Clean + Rebuild app

---

### ❌ **Lỗi: "The SMS verification code used to create the phone auth credential is invalid"**

**Nguyên nhân:** Mã OTP sai hoặc đã hết hạn (60 giây)

**Giải pháp:**
- Nhập đúng mã OTP từ SMS
- Nếu hết hạn, click "Gửi lại"

---

### ❌ **Lỗi: "The SMS quota for the project has been exceeded"**

**Nguyên nhân:** Vượt quá giới hạn SMS miễn phí của Firebase

**Giải pháp:**
- Sử dụng số điện thoại test (xem phần Test ở trên)
- Hoặc nâng cấp Firebase plan
- Hoặc chờ reset quota (24h)

---

### ❌ **Lỗi: "Missing Activity for reCAPTCHA verification"**

**Nguyên nhân:** Plugin.Firebase < v21.2.0 yêu cầu Activity reference

**Giải pháp:**
- ✅ Đã fix trong `MainActivity.cs`
- Activity được pass vào `VerifyPhoneNumberAsync()`

---

### ❌ **reCAPTCHA luôn xuất hiện (không tự động verify)**

**Nguyên nhân:**
- Thiếu SHA-256 (Play Integrity API cần SHA-256)
- Thiết bị không có Google Play Services
- App không được phân phối qua Google Play Store

**Giải pháp:**
1. Thêm SHA-256 vào Firebase (bắt buộc!)
2. Test trên thiết bị thật có Google Play Services
3. reCAPTCHA là fallback bình thường, không phải lỗi

---

## 🚀 Usage trong Code

### **1. Gửi OTP:**

```csharp
var authService = new FirebaseAuthService(secureStorage);
var verificationId = await authService.SendOtpAsync("+84xxxxxxxxx");
```

### **2. Verify OTP:**

```csharp
var success = await authService.VerifyOtpAsync(verificationId, "123456");
if (success)
{
    // User authenticated!
    await Shell.Current.GoToAsync("//MainPage");
}
```

### **3. Logout:**

```csharp
await authService.LogoutAsync();
```

---

## 📊 Giới hạn Firebase Phone Auth (Free Plan)

| Feature | Limit |
|---------|-------|
| **SMS/day** | 10,000 messages |
| **Verifications/day** | 10,000 attempts |
| **Test phone numbers** | Max 10 numbers |
| **OTP timeout** | 60 seconds |

**Để tăng giới hạn:** Nâng cấp lên Firebase Blaze Plan (pay-as-you-go)

---

## 🔒 Bảo mật

### **Các lớp bảo mật:**

1. ✅ **Play Integrity API** - Xác thực app thật (Google)
2. ✅ **reCAPTCHA** - Ngăn chặn bot
3. ✅ **SMS OTP** - Xác thực số điện thoại
4. ✅ **Firebase Rules** - Kiểm soát quyền truy cập database

### **Best Practices:**

- ⚠️ Phone Auth kém an toàn hơn Email/Password
- ✅ Kết hợp với Multi-Factor Authentication (MFA)
- ✅ Giới hạn vùng SMS (chỉ Vietnam)
- ✅ Monitor Firebase Usage để phát hiện abuse
- ⚠️ **Không hardcode số điện thoại test trong production!**

---

## 📖 Tài liệu tham khảo

- **Firebase Phone Auth (Android):** https://firebase.google.com/docs/auth/android/phone-auth
- **Plugin.Firebase:** https://github.com/TobiasBuchholz/Plugin.Firebase
- **Play Integrity API:** https://developer.android.com/google/play/integrity

---

## ✅ Kiểm tra cuối cùng

Trước khi build production:

- [ ] SHA-1 và SHA-256 đã được thêm vào Firebase
- [ ] google-services.json đã được cập nhật
- [ ] Package name khớp: `com.fraudguard.ai`
- [ ] Phone authentication đã được bật trong Firebase Console
- [ ] Đã xóa tất cả số điện thoại test
- [ ] Đã test trên thiết bị thật
- [ ] reCAPTCHA hoạt động (fallback)
- [ ] SMS auto-retrieval hoạt động (nếu có)

---

🎉 **Hoàn tất setup Firebase Phone Authentication!**
