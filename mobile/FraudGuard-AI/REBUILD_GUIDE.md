# 🔧 HƯỚNG DẪN REBUILD APP ĐỂ THẤY THAY ĐỔI

## ⚠️ QUAN TRỌNG
Các thay đổi UI trong file `.xaml` và `.xaml.cs` **CẦN REBUILD APP** để có hiệu lực!

---

## 📦 CÁC THAY ĐỔI ĐÃ COMMIT

### Commit: `9f403e0` - "fix hardcode"

#### 1. **MainPage.xaml** - Đã xóa hardcode:
- ❌ ~~`Text="Tỷ lệ chặn: 98.5%"`~~ → `Text="Chưa có dữ liệu"`
- ❌ ~~`Text="98.5%"`~~ → `Text="0%"`
- ❌ ~~`Text="Warning"`~~ → `Text=""`
- ❌ ~~`Text="Risk: 95%"`~~ → `Text=""`
- ❌ ~~`Text="↑ +12 tuần này"`~~ → `Text=""` + `IsVisible="False"`
- ❌ ~~`Text="↑ +2.3%"`~~ → `Text=""` + `IsVisible="False"`

#### 2. **SettingsPage.xaml** - Đã xóa hardcode:
- ❌ ~~`Text="wss://fraudguard-ai-j1j1.onrender.com/ws"`~~ → `Text=""` (dynamic)
- ❌ ~~`Text="Đã kết nối"`~~ → `Text=""` (dynamic)
- ✅ **THÊM MỚI:** Toggle "Bảo vệ tự động" (Auto Protection)

#### 3. **SettingsPage.xaml.cs** - Đã thêm logic:
- ✅ `PREF_AUTO_PROTECTION` constant
- ✅ `OnAutoProtectionToggled()` handler
- ✅ `IsAutoProtectionEnabled()` public method

#### 4. **MainPage.xaml.cs** - Đã thêm:
- ✅ `AutoStartProtectionIfEnabledAsync()` - Tự động bật khi mở app

#### 5. **HistoryPage.xaml** - Đổi tiếng Anh → Việt:
- ❌ ~~`"Call History"`~~ → `"Lịch sử cuộc gọi"`
- ❌ ~~`"No history yet"`~~ → `"Chưa có lịch sử"`
- ❌ ~~`"Evidence"`~~ → `"Bằng chứng"`
- ❌ ~~`"Unable to load history"`~~ → `"Không thể tải lịch sử"`
- ❌ ~~`"Try Again"`~~ → `"Thử lại"`

---

## 🔨 CÁCH REBUILD APP

### Trên Windows (Visual Studio / VS Code):

#### Option 1: Clean & Rebuild (KHUYẾN NGHỊ)
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Clean project
dotnet clean

# Rebuild
dotnet build -c Debug
```

#### Option 2: Visual Studio
1. Mở `FraudGuardAI.sln`
2. Menu: **Build** → **Clean Solution**
3. Menu: **Build** → **Rebuild Solution**
4. Nhấn **F5** hoặc nút ▶ để chạy

#### Option 3: Uninstall & Reinstall
```powershell
# Uninstall app trên Android/Emulator
adb uninstall com.fraudguardai.app

# Rebuild và deploy
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
dotnet build -t:Run -f net8.0-android
```

### Trên Android Device/Emulator:
1. **Xóa app cũ** từ device
2. **Rebuild** từ VS/VS Code
3. **Deploy** lại

---

## ✅ SAU KHI REBUILD, BẠN SẼ THẤY:

### 1. Trang chủ (MainPage):
```
┌─────────────────────────────┐
│        🛡 Shield            │
│    "Chưa kích hoạt"         │
│  "Chưa có dữ liệu"  ← ĐỔI   │
│                             │
│  [Kích hoạt bảo vệ] ← NÚT   │
└─────────────────────────────┘

Stats Cards:
- "0" (thay vì "98.5%")
- Không còn "+12 tuần này"
- "0%" (thay vì "98.5%")
```

### 2. Settings:
```
┌─────────────────────────────┐
│ 🛡 Bảo vệ tự động    [ON] ← MỚI
│   "Tự động kích hoạt..."    │
│                             │
│ 🔗 Kết nối Server           │
│   ● "" ← Trống (sẽ check)  │
│                             │
│ Cấu hình hiện tại:          │
│ "" ← Trống (dynamic)        │
└─────────────────────────────┘
```

### 3. History:
```
┌─────────────────────────────┐
│ "Lịch sử cuộc gọi" ← Việt   │
│ "Cuộc gọi đã được..."       │
│                             │
│ "Chưa có lịch sử" ← Việt    │
└─────────────────────────────┘
```

---

## 🎯 TÍNH NĂNG MỚI: AUTO PROTECTION

### Cách sử dụng:
1. Mở **Settings**
2. Tìm section **"GIAO DIỆN"**
3. Toggle **"Bảo vệ tự động"** → **ON** (màu xanh)
4. ✅ App sẽ tự động kích hoạt bảo vệ khi mở

### Logic:
```csharp
// Khi mở app
if (SettingsPage.IsAutoProtectionEnabled() && !_isProtectionActive)
{
    await StartProtectionAsync();
}
```

---

## 🐛 NẾU VẪN KHÔNG THẤY THAY ĐỔI:

### Bước 1: Xác nhận code đã pull
```powershell
cd E:\FraudGuard-AI
git log --oneline -1
# Phải thấy: "9f403e0 fix hardcode"
```

### Bước 2: Kiểm tra file đã thay đổi
```powershell
git show HEAD:mobile/FraudGuard-AI/MainPage.xaml | Select-String "98.5"
# Không được có kết quả nào!
```

### Bước 3: Force rebuild
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Xóa cache
Remove-Item -Recurse -Force bin/
Remove-Item -Recurse -Force obj/

# Rebuild
dotnet restore
dotnet clean
dotnet build -c Debug
```

### Bước 4: Uninstall app trên device
```powershell
# Kiểm tra device
adb devices

# Uninstall
adb uninstall com.fraudguardai.app

# Deploy lại
dotnet build -t:Run -f net8.0-android
```

---

## 📝 CHECKLIST

- [ ] Đã pull code mới nhất (`git pull origin UImobile`)
- [ ] Đã xóa cache (`bin/` và `obj/`)
- [ ] Đã clean solution
- [ ] Đã rebuild solution
- [ ] Đã uninstall app cũ trên device
- [ ] Đã deploy app mới

---

## 🆘 NẾU VẪN CÓ VẤN ĐỀ

Chạy script tự động:
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Uninstall, clean, rebuild, deploy
adb uninstall com.fraudguardai.app
Remove-Item -Recurse -Force bin/, obj/ -ErrorAction SilentlyContinue
dotnet clean
dotnet build -t:Run -f net8.0-android
```

Hoặc liên hệ để debug!
