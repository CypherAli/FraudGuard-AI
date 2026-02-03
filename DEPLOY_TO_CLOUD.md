# 🚀 DEPLOY FRAUDGUARD AI LÊN CLOUD (PRODUCTION)

## 🎯 MỤC TIÊU
Người dùng tải APK → Cài đặt → Dùng luôn (KHÔNG CẦN USB, KHÔNG CẦN CẤU HÌNH)

---

## 📋 CHECKLIST HOÀN CHỈNH

- [ ] Deploy PostgreSQL Database lên cloud
- [ ] Deploy Go API Server lên cloud
- [ ] Test API endpoint từ internet
- [ ] Update app với Production Server URL
- [ ] Build APK mới (Production version)
- [ ] Upload APK lên Google Drive/Firebase
- [ ] Test từ điện thoại người dùng khác

---

## 🛠️ PHƯƠNG ÁN 1: RAILWAY (KHUYẾN NGHỊ)

### Ưu điểm:
- ✅ Miễn phí $5 credit/tháng (đủ dùng)
- ✅ PostgreSQL built-in
- ✅ Deploy từ GitHub 1 click
- ✅ Auto SSL/HTTPS
- ✅ Public URL cố định

### Bước 1: Tạo tài khoản Railway

1. Truy cập: https://railway.app/
2. Sign up with GitHub
3. Xác nhận email

### Bước 2: Deploy Database

```bash
# Railway Dashboard
1. Click "New Project"
2. Chọn "Provision PostgreSQL"
3. Đợi khởi tạo (30 giây)
4. Click vào PostgreSQL service
5. Tab "Variables" → Copy các giá trị:
   - PGHOST
   - PGPORT
   - PGDATABASE
   - PGUSER
   - PGPASSWORD
```

**Lưu lại thông tin này:**
```
PGHOST: containers-us-west-xxx.railway.app
PGPORT: 5432
PGDATABASE: railway
PGUSER: postgres
PGPASSWORD: [password_của_bạn]
```

### Bước 3: Import dữ liệu Blacklist vào Database

**Cách 1: Dùng Railway CLI**
```powershell
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# Link project
railway link

# Connect to database
railway connect postgres

# Import seed data
\i E:/FraudGuard-AI/services/api-gateway/seed_data.sql
\q
```

**Cách 2: Dùng pgAdmin/DBeaver (GUI)**
```
1. Mở pgAdmin 4
2. Create new Server:
   - Host: [PGHOST từ Railway]
   - Port: 5432
   - Database: railway
   - Username: postgres
   - Password: [PGPASSWORD]
3. Mở Query Tool
4. Paste nội dung seed_data.sql
5. Execute (F5)
```

### Bước 4: Deploy Go API Server

**4.1. Chuẩn bị code**
```powershell
cd E:\FraudGuard-AI\services\api-gateway

# Tạo file railway.json (config Railway)
@"
{
  "`$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "./bin/api-gateway",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
"@ | Out-File -FilePath railway.json -Encoding UTF8

# Tạo Procfile (Railway detect)
echo "web: ./bin/api-gateway" > Procfile
```

**4.2. Update database connection**

File: `services/api-gateway/cmd/api-gateway/main.go`
```go
// Thay đổi connection string để dùng environment variables
dbConnStr := fmt.Sprintf(
    "host=%s port=%s user=%s password=%s dbname=%s sslmode=require",
    os.Getenv("PGHOST"),
    os.Getenv("PGPORT"),
    os.Getenv("PGUSER"),
    os.Getenv("PGPASSWORD"),
    os.Getenv("PGDATABASE"),
)
```

**4.3. Push to GitHub**
```powershell
cd E:\FraudGuard-AI

# Commit changes
git add .
git commit -m "Prepare for Railway deployment"
git push origin UImobile
```

**4.4. Deploy trên Railway**
```
1. Railway Dashboard → "New Project"
2. Chọn "Deploy from GitHub repo"
3. Chọn repository: CypherAli/FraudGuard-AI
4. Root Directory: services/api-gateway
5. Add Environment Variables:
   - PGHOST: [giá trị từ bước 2]
   - PGPORT: 5432
   - PGDATABASE: railway
   - PGUSER: postgres
   - PGPASSWORD: [password]
   - DEEPGRAM_API_KEY: [key của bạn]
   - PORT: 8080
6. Click "Deploy"
```

**4.5. Lấy Public URL**
```
1. Click vào API service
2. Tab "Settings" → Generate Domain
3. Copy URL: https://fraudguard-api-production.up.railway.app
```

### Bước 5: Test API từ Internet

```powershell
# Test health endpoint
curl https://fraudguard-api-production.up.railway.app/health

# Kết quả mong đợi:
# {"status":"healthy","database":"connected"}
```

---

## 📱 CẬP NHẬT APP VỚI PRODUCTION SERVER

### Bước 6: Hardcode Production Server URL vào App

**File:** `mobile/FraudGuard-AI/Constants/AppConstants.cs`

Thêm vào cuối class:
```csharp
#region Server Configuration

// Production Server URL - THAY ĐỔI SAU KHI DEPLOY
public const string PRODUCTION_SERVER_URL = "https://fraudguard-api-production.up.railway.app";

// Default mode
public const bool USE_PRODUCTION_SERVER = true; // true = dùng cloud, false = local

#endregion
```

**File:** `mobile/FraudGuard-AI/SettingsPage.xaml.cs`

Tìm method `OnAppearing()` và thêm:
```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    
    // Load saved settings
    UsbModeSwitch.IsToggled = Preferences.Get("UsbMode", false);
    
    // Set default server URL if empty
    var savedUrl = Preferences.Get("ServerUrl", "");
    if (string.IsNullOrEmpty(savedUrl))
    {
        savedUrl = AppConstants.PRODUCTION_SERVER_URL;
        Preferences.Set("ServerUrl", savedUrl);
    }
    ServerUrlEntry.Text = savedUrl;
}
```

### Bước 7: Build APK Production

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Clean
dotnet clean

# Build với Production config
dotnet build -f net8.0-android -c Release /p:AndroidPackageFormat=apk

# APK sẽ ở:
# bin\Release\net8.0-android\com.fraudguard.ai-Signed.apk
```

**Nếu Release build lỗi (như trước), dùng Debug:**
```powershell
dotnet build -f net8.0-android -c Debug /p:AndroidPackageFormat=apk

# APK: bin\Debug\net8.0-android\com.fraudguard.ai-Signed.apk
```

---

## 📦 PHÂN PHỐI APK

### Bước 8: Upload lên Google Drive

```
1. Mở Google Drive
2. Upload file: com.fraudguard.ai-Signed.apk
3. Chuột phải → Share → Anyone with the link
4. Copy link chia sẻ
```

### Bước 9: Gửi cho người dùng

**Tin nhắn mẫu:**

```
🛡️ FRAUDGUARD AI - BẢO VỆ CHỐNG LỪA ĐẢO CUỘC GỌI

📥 DOWNLOAD APP:
https://drive.google.com/file/d/xxxxx/view?usp=sharing

📱 HƯỚNG DẪN CÀI ĐẶT:

1️⃣ Tải file APK từ link trên
2️⃣ Mở Settings → Security → Bật "Install unknown apps"
3️⃣ Mở file APK → Nhấn Install
4️⃣ Mở app FraudGuard AI
5️⃣ Cấp quyền Phone và Microphone
6️⃣ Vào tab Protection → Nhấn "Start Protection"

✅ XONG! App tự động bảo vệ bạn khỏi cuộc gọi lừa đảo

❓ Server URL đã được cài sẵn, bạn KHÔNG CẦN cấu hình gì thêm!
```

---

## 🧪 TEST TỪ ĐIỆN THOẠI NGƯỜI DÙNG

### Bước 10: Kiểm tra hoạt động

**Test checklist:**
```
□ Tải APK thành công
□ Cài đặt không lỗi
□ Mở app lần đầu
□ Vào Settings → Server URL hiển thị đúng
□ Click "Test Connection" → Thành công
□ Vào Protection → Start Protection
□ Nhận cuộc gọi test → Alert hiển thị
□ Check History → Log cuộc gọi xuất hiện
```

---

## 🔧 TROUBLESHOOTING

### Lỗi: "Cannot connect to server"
```
Nguyên nhân: API server chưa chạy hoặc URL sai
Giải pháp:
1. Kiểm tra Railway logs
2. Test curl từ máy tính
3. Verify URL trong app đúng với Railway
```

### Lỗi: "Database connection failed"
```
Nguyên nhân: PostgreSQL credentials sai
Giải pháp:
1. Railway → PostgreSQL → Variables
2. Copy lại đúng PGHOST, PGPASSWORD
3. Restart API service
```

### Lỗi: "No fraud data"
```
Nguyên nhân: Chưa import seed_data.sql
Giải pháp:
1. Connect tới Railway PostgreSQL bằng pgAdmin
2. Chạy lại seed_data.sql
3. Verify: SELECT COUNT(*) FROM blacklist;
```

---

## 💰 CHI PHÍ DỰ KIẾN

### Railway Free Tier:
- $5 credit/tháng (miễn phí)
- Đủ cho:
  - PostgreSQL database (1GB)
  - API server (500MB RAM)
  - ~1000 requests/ngày
  
### Khi cần scale (nhiều user):
- Railway Pro: $20/tháng (unlimited credit)
- Hoặc chuyển sang DigitalOcean: $5-10/tháng

---

## 📊 MONITORING

### Railway Dashboard:
- CPU/RAM usage
- Request logs
- Database connections
- Error tracking

### App Analytics (optional):
- Firebase Analytics
- Crashlytics

---

## 🎉 KẾT QUẢ CUỐI CÙNG

**Trước:**
```
User → Tải APK → Cài đặt → Cần nhập server URL 
→ Cần chạy local server → Cần Ngrok → PHỨC TẠP ❌
```

**Sau:**
```
User → Tải APK → Cài đặt → Dùng luôn → HOÀN HẢO ✅
```

---

**Tạo bởi:** FraudGuard AI Team  
**Ngày:** February 3, 2026  
**Version:** Production Deployment Guide v1.0
