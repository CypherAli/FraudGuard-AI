# ✅ HOÀN TẤT: Firebase Phone Authentication Setup

## 📋 Tóm tắt kết quả kiểm tra

### ✅ **1. SHA Certificates**
```
SHA-1:   21:56:A3:0D:E9:61:23:C7:F6:B9:D6:32:53:30:FD:81:4E:45:1F:87
SHA-256: 5F:80:48:F4:6B:C0:2F:82:17:FC:54:C1:E7:8E:A4:CD:A6:C8:51:D7:EA:63:43:E7:5B:92:53:A4:F8:48:50:EC
```

**Vai trò:**
- **SHA-1**: Bắt buộc cho reCAPTCHA verification
- **SHA-256**: Bắt buộc cho Play Integrity API (tự động verify, không cần reCAPTCHA)

### ✅ **2. Package Name**
- **Firebase**: `com.fraudguard.ai` ✅
- **Code**: `com.fraudguard.ai` ✅
- **Trạng thái**: KHỚP HOÀN TOÀN

### ✅ **3. google-services.json**
- ✅ File tồn tại
- ✅ Package name khớp
- ✅ Đã cấu hình trong `.csproj`

### ✅ **4. Plugin.Firebase.Auth**
- ✅ Đã được cài đặt (v3.1.1)
- ✅ FirebaseAuthService.cs đã được tạo
- ✅ PhoneAuthPage.xaml/cs đã được tạo

---

## 🚀 HÀNH ĐỘNG TIẾP THEO (Bắt buộc!)

### **Bước 1: Thêm SHA vào Firebase Console**

1. Mở https://console.firebase.google.com
2. **Project Settings** → Tab **"Your apps"** → Android app
3. Kéo xuống **"SHA certificate fingerprints"**
4. Click **"Add fingerprint"** và thêm **SHA-1**:
   ```
   21:56:A3:0D:E9:61:23:C7:F6:B9:D6:32:53:30:FD:81:4E:45:1F:87
   ```
5. Click **"Add fingerprint"** lần nữa và thêm **SHA-256**:
   ```
   5F:80:48:F4:6B:C0:2F:82:17:FC:54:C1:E7:8E:A4:CD:A6:C8:51:D7:EA:63:43:E7:5B:92:53:A4:F8:48:50:EC
   ```
6. Click **"Save"** 💾

---

### **Bước 2: Kích hoạt Phone Authentication**

1. Firebase Console → **Authentication** → Tab **"Sign-in method"**
2. Click **"Phone"**
3. Toggle **"Enable"** ✅
4. Click **"Save"**

---

### **Bước 3: (Optional) Giới hạn vùng SMS**

Để tránh lạm dụng:

1. **Authentication** → Tab **"Settings"**
2. Mục **"SMS regions"**
3. Chọn **"Allow specific regions"**
4. Chỉ chọn: **Vietnam (+84)**
5. Click **"Save"**

---

### **Bước 4: Tải lại google-services.json**

⚠️ **BẮT BUỘC** sau khi thêm SHA certificates!

1. Firebase Console → **Project Settings**
2. Cuộn xuống Android app → Click ⚙️
3. **"Download google-services.json"**
4. Copy vào: `mobile\FraudGuard-AI\Platforms\Android\`
5. **Thay thế file cũ**

---

### **Bước 5: Thêm số điện thoại test (Development)**

Để test mà không tốn SMS:

1. Firebase Console → **Authentication** → **Sign-in method**
2. Click **"Phone"** → Mở **"Phone numbers for testing"**
3. Thêm:
   ```
   Phone number: +84 650-555-3434
   Verification code: 654321
   ```
4. Click **"Add"**

**Lưu ý:** ⚠️ Phải xóa trước khi release production!

---

### **Bước 6: Build & Test**

```powershell
# Visual Studio
# 1. Clean Solution
# 2. Rebuild Solution
# 3. Deploy to Android Device/Emulator
```

**Test flow:**
1. Mở app → Chọn "Đăng nhập bằng số điện thoại"
2. Nhập: `+84 650-555-3434`
3. Click "Gửi mã OTP"
4. Nhập OTP: `654321`
5. Click "Xác thực"
6. ✅ **Thành công!**

---

## 📂 Files đã được tạo/cập nhật

| File | Mô tả |
|------|-------|
| [`get_sha1_keys.ps1`](get_sha1_keys.ps1) | Script lấy SHA-1 và SHA-256 |
| [`test_phone_auth.ps1`](test_phone_auth.ps1) | Script kiểm tra setup |
| [`PHONE_AUTH_SETUP.md`](PHONE_AUTH_SETUP.md) | Hướng dẫn chi tiết đầy đủ |
| [`Pages/PhoneAuthPage.xaml`](Pages/PhoneAuthPage.xaml) | UI đăng nhập số điện thoại |
| [`Pages/PhoneAuthPage.xaml.cs`](Pages/PhoneAuthPage.xaml.cs) | Logic xác thực |
| [`Platforms/Android/MainActivity.cs`](Platforms/Android/MainActivity.cs) | Cập nhật xử lý callbacks |
| [`Services/FirebaseAuthService.cs`](Services/FirebaseAuthService.cs) | Service xác thực (đã có sẵn) |

---

## 🔥 Quy trình xác thực

```
User nhập số điện thoại (+84xxxxxxxxx)
           ↓
App → Firebase: Xin gửi OTP
           ↓
Firebase xác thực app:
  • Play Integrity API (nếu có SHA-256) ← ƯU TIÊN
  • reCAPTCHA (fallback nếu không có Play Integrity)
           ↓
Firebase → User: SMS với mã OTP 6 số
           ↓
User nhập OTP vào app
           ↓
App → Firebase: Verify OTP
           ↓
Firebase → App: User Token + UID
           ↓
✅ ĐĂNG NHẬP THÀNH CÔNG!
```

---

## ⚠️ Lưu ý quan trọng

### **Tại sao cần CẢ HAI SHA-1 và SHA-256?**

| Certificate | Mục đích | Khi nào dùng? |
|-------------|----------|---------------|
| **SHA-1** | reCAPTCHA verification | Fallback khi Play Integrity không khả dụng |
| **SHA-256** | Play Integrity API | Xác thực tự động (không cần reCAPTCHA) |

**Kịch bản:**
- ✅ **Có SHA-256**: Play Integrity tự động verify → Không hiện reCAPTCHA
- ⚠️ **Chỉ có SHA-1**: Luôn hiện reCAPTCHA cho user (trải nghiệm kém)
- ❌ **Không có cả 2**: Firebase chặn app → Lỗi authentication

→ **Kết luận:** Thêm CẢ HAI để đảm bảo hoạt động tốt nhất!

---

## 🛠️ Troubleshooting nhanh

| Lỗi | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| "This app is not authorized..." | SHA chưa thêm vào Firebase | Thêm SHA-1 + SHA-256, tải lại google-services.json |
| "SMS quota exceeded" | Vượt giới hạn SMS | Dùng số test (+84 650-555-3434) |
| reCAPTCHA luôn xuất hiện | Thiếu SHA-256 | Thêm SHA-256 để bật Play Integrity |
| "Invalid verification code" | OTP sai hoặc hết hạn | Nhập đúng mã, click "Gửi lại" nếu hết hạn |

---

## 📚 Tài liệu tham khảo

- [Firebase Phone Auth (Android)](https://firebase.google.com/docs/auth/android/phone-auth)
- [Plugin.Firebase.Auth](https://github.com/TobiasBuchholz/Plugin.Firebase)
- [Play Integrity API](https://developer.android.com/google/play/integrity)
- [PHONE_AUTH_SETUP.md](PHONE_AUTH_SETUP.md) - Hướng dẫn chi tiết

---

## ✅ Checklist cuối cùng

Trước khi chạy app:

- [ ] SHA-1 đã thêm vào Firebase Console
- [ ] SHA-256 đã thêm vào Firebase Console
- [ ] Phone authentication đã được bật
- [ ] google-services.json đã được tải lại và thay thế
- [ ] Số điện thoại test đã được thêm (nếu cần)
- [ ] Visual Studio đã Clean + Rebuild
- [ ] Deploy lên thiết bị Android có Google Play Services

---

🎉 **Sẵn sàng để test Firebase Phone Authentication!**

**Run:** `.\test_phone_auth.ps1` để kiểm tra lại bất cứ lúc nào.
