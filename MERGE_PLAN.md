# 🚨 KẾ HOẠCH MERGE AN TOÀN 

## ⚠️ VẤN ĐỀ PHÁT HIỆN

Nhánh `honqlee-dev` **XÓA** file backend quan trọng:
- ❌ `services/api-gateway/internal/handlers/auth_handler.go` 

Nếu merge trực tiếp → **MẤT BACKEND CODE!**

---

## ✅ PHƯƠNG ÁN ĐỀ XUẤT: Merge có chọn lọc

### Bước 1: Backup main
```powershell
git checkout -b main-backup
git checkout main
```

### Bước 2: Merge honqlee-dev
```powershell
git merge honqlee-dev --no-commit --no-ff
```

### Bước 3: KHÔI PHỤC auth_handler.go
```powershell
# Restore file backend từ main
git checkout main -- services/api-gateway/internal/handlers/auth_handler.go
```

### Bước 4: Kiểm tra & commit
```powershell
git status
git commit -m "Merge honqlee-dev (Gmail OTP) nhưng giữ lại backend auth_handler.go"
```

---

## 📊 NHỮNG GÌ SẼ THAY ĐỔI

### ✅ FILE THÊM MỚI (OK)
- `BrevoEmailService.cs` - Gửi OTP qua Brevo
- `AppConfig.cs` - Đọc config Brevo  
- `FirebasePhoneAuthService.cs` - Firebase phone auth
- `appsettings.json` - Config Brevo API

### ✏️ FILE CẬP NHẬT (OK)
- `EmailOtpAuthService.cs` - Dùng Brevo
- `SecureStorageService.cs` - Lưu email/OTP
- `MauiProgram.cs` - Register services
- `LoginPage.xaml/.cs` - UI đăng nhập Gmail
- `OtpVerificationPage.xaml/.cs` - UI nhập OTP
- Nhiều file khác...

### ❌ FILE SẼ BỊ XÓA (CẦN KHÔI PHỤC)
- ❌ **auth_handler.go** (BACKEND - PHẢI GIỮ LẠI!)
- `PhoneAuthPage.xaml/.cs` (Mobile - OK, không dùng)
- `PhoneAuthHandler.cs` (Android - OK, không dùng)
- Các file `obj/` (Build artifacts - OK)

---

## 🎯 KẾT QUẢ MONG MUỐN

✅ Có Gmail OTP từ honqlee-dev  
✅ Backend auth_handler.go vẫn còn  
✅ Code hoạt động bình thường  
✅ Có thể rollback dễ dàng

---

## 🔧 THỰC THI NGAY

Chạy các lệnh sau:

```powershell
# 1. Tạo backup
git branch main-backup

# 2. Merge không commit (để kiểm tra)
git merge honqlee-dev --no-commit --no-ff

# 3. Khôi phục backend
git checkout main -- services/api-gateway/internal/handlers/auth_handler.go

# 4. Kiểm tra
git status

# 5. Nếu OK, commit
git commit -m "feat: Merge Gmail OTP from honqlee-dev, keep backend untouched"

# 6. Nếu có vấn đề, hủy merge
# git merge --abort
# git checkout main-backup
```

---

## 📞 Rollback nếu cần

```powershell
# Quay về trước khi merge
git reset --hard main-backup

# Sau khi test OK, xóa backup
git branch -D main-backup
```

---

**Làm theo các bước trên để merge AN TOÀN!** 🎉
