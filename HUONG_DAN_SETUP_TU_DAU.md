# 🚀 HƯỚNG DẪN SETUP DỰ ÁN FRAUDGUARD-AI TỪ ĐẦU

**Dành cho người mới tải source code**

---

## 📋 MỤC LỤC

1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Cài Đặt Tools](#cài-đặt-tools)
3. [Setup Backend (Go)](#setup-backend-go)
4. [Setup Mobile App (.NET MAUI)](#setup-mobile-app-net-maui)
5. [Chạy Dự Án](#chạy-dự-án)
6. [Troubleshooting](#troubleshooting)

---

## 📦 YÊU CẦU HỆ THỐNG

### Phần Cứng
- **RAM**: Tối thiểu 8GB (khuyến nghị 16GB)
- **Ổ cứng**: 10GB trống
- **Điện thoại Android**: API Level 21+ (Android 5.0+)
- **Cáp USB**: Để kết nối điện thoại với máy tính

### Phần Mềm
- **Windows 10/11** (64-bit)
- **Git** (để clone project)
- Kết nối Internet ổn định

---

## 🔧 CÀI ĐẶT TOOLS

### 1. Cài Đặt Go (Backend)

**Bước 1**: Tải Go từ trang chính thức
- Truy cập: https://go.dev/dl/
- Tải file: `go1.23.x.windows-amd64.msi` (phiên bản mới nhất)

**Bước 2**: Cài đặt
- Chạy file MSI vừa tải
- Chọn **Next** → **Next** → **Install**
- Đợi hoàn tất → **Finish**

**Bước 3**: Kiểm tra
```powershell
go version
```
Kết quả mong đợi: `go version go1.23.x windows/amd64`

---

### 2. Cài Đặt .NET SDK (Mobile)

**Bước 1**: Tải .NET 8 SDK
- Truy cập: https://dotnet.microsoft.com/download/dotnet/8.0
- Tải: **SDK 8.0.x (Windows x64)**

**Bước 2**: Cài đặt
- Chạy file installer
- Chọn **Install**
- Đợi hoàn tất (có thể mất 5-10 phút)

**Bước 3**: Kiểm tra
```powershell
dotnet --version
```
Kết quả mong đợi: `8.0.xxx`

---

### 3. Cài Đặt .NET MAUI Workload

**Bước 1**: Mở PowerShell **với quyền Administrator**
- Nhấn Windows + X
- Chọn **Terminal (Admin)** hoặc **PowerShell (Admin)**

**Bước 2**: Cài workload
```powershell
dotnet workload install maui-android
```
**Lưu ý**: Quá trình này có thể mất 10-20 phút, cần Internet tốt

**Bước 3**: Kiểm tra
```powershell
dotnet workload list
```
Phải thấy: `maui-android` trong danh sách

---

### 4. Cài Đặt Android SDK & Platform Tools

**Tự động (Khuyến nghị)**: Visual Studio 2022 sẽ tự động cài khi build lần đầu

**Thủ công** (nếu cần):
1. Tải Android Command Line Tools: https://developer.android.com/studio#command-tools
2. Giải nén vào: `C:\Android\cmdline-tools`
3. Chạy:
```powershell
cd C:\Android\cmdline-tools\latest\bin
.\sdkmanager.bat "platform-tools" "platforms;android-34" "build-tools;34.0.0"
```

**Vị trí quan trọng**: 
- Platform Tools thường ở: `C:\Users\[YOUR_USERNAME]\AppData\Local\Android\Sdk\platform-tools\`

---

### 5. Cài Đặt PostgreSQL Database

**Bước 1**: Tải PostgreSQL 16
- Truy cập: https://www.postgresql.org/download/windows/
- Tải: **PostgreSQL 16.x for Windows x86-64**

**Bước 2**: Cài đặt
- Chạy installer
- Chọn components: PostgreSQL Server, pgAdmin 4
- Đặt password cho user `postgres` (ghi nhớ password này!)
- Port mặc định: `5432`
- Chọn locale: `Default`

**Bước 3**: Kiểm tra
```powershell
psql --version
```

---

### 6. Cài Đặt Visual Studio Code (Tùy chọn)

**Bước 1**: Tải VS Code
- Truy cập: https://code.visualstudio.com/
- Tải và cài đặt

**Bước 2**: Cài Extensions (Khuyến nghị)
- Go (Go Team)
- C# Dev Kit (Microsoft)
- REST Client (Huachao Mao)

---

## 📥 TẢI VÀ SETUP PROJECT

### 1. Clone Repository

```powershell
# Chọn thư mục lưu project (ví dụ: E:\)
cd E:\

# Clone project
git clone https://github.com/CypherAli/FraudGuard-AI.git

# Vào thư mục project
cd FraudGuard-AI
```

---

## 🔨 SETUP BACKEND (GO)

### Bước 1: Tạo File .env

```powershell
cd E:\FraudGuard-AI\services\api-gateway
```

Tạo file `.env` với nội dung:

```env
# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_USER=postgres
DB_PASSWORD=YOUR_POSTGRES_PASSWORD_HERE
DB_NAME=fraudguard_db
DB_SSLMODE=disable

# Deepgram API (Speech-to-Text)
DEEPGRAM_API_KEY=YOUR_DEEPGRAM_KEY_HERE

# Gemini AI (Optional - chưa tích hợp)
GEMINI_API_KEY=YOUR_GEMINI_KEY_HERE

# Server Configuration
PORT=8080
```

**⚠️ Quan trọng**: 
- Thay `YOUR_POSTGRES_PASSWORD_HERE` bằng password PostgreSQL của bạn
- Lấy Deepgram API Key miễn phí tại: https://deepgram.com/ (có $200 credit)

---

### Bước 2: Tạo Database

**Cách 1: Dùng Script (Khuyến nghị)**
```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\setup_database.ps1
```

**Cách 2: Thủ công**
```powershell
# Mở psql
psql -U postgres

# Trong psql console, chạy:
CREATE DATABASE fraudguard_db;
\c fraudguard_db
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
\q
```

---

### Bước 3: Cài Dependencies

```powershell
cd E:\FraudGuard-AI\services\api-gateway
go mod download
```

---

### Bước 4: Test Backend

```powershell
cd E:\FraudGuard-AI\services\api-gateway
go run cmd/api/main.go
```

**Kết quả mong đợi:**
```
 Starting FraudGuard AI API Gateway...
 Database connection established (Max: 25, Min: 5)
 Deepgram client initialized
 WebSocket hub started
 Server listening on 0.0.0.0:8080
```

Nhấn **Ctrl+C** để dừng server (test xong rồi)

---

## 📱 SETUP MOBILE APP (.NET MAUI)

### Bước 1: Bật Developer Mode Trên Điện Thoại

**Trên điện thoại Android:**
1. Vào **Settings** (Cài đặt)
2. Chọn **About phone** (Thông tin điện thoại)
3. Tìm **Build number** (Số bản dựng)
4. Nhấn liên tục 7 lần vào **Build number**
5. Thấy thông báo: "You are now a developer!"

---

### Bước 2: Bật USB Debugging

**Trên điện thoại:**
1. Vào **Settings** → **Developer options** (Tùy chọn nhà phát triển)
2. Bật **USB debugging**
3. Bật **Install via USB** (nếu có)

---

### Bước 3: Kết Nối Điện Thoại Với Máy Tính

1. Cắm cáp USB vào điện thoại và máy tính
2. **Trên điện thoại** sẽ có popup: **"Allow USB debugging?"**
3. Tích ✅ **"Always allow from this computer"**
4. Nhấn **OK**

---

### Bước 4: Kiểm Tra Kết Nối

```powershell
# Tìm đường dẫn adb.exe (thường là):
cd "C:\Users\[YOUR_USERNAME]\AppData\Local\Android\Sdk\platform-tools"

# Kiểm tra
.\adb.exe devices
```

**Kết quả mong đợi:**
```
List of devices attached
R58T80NYT3E     device
```

**⚠️ Nếu thấy `unauthorized`**: 
- Nhấn OK lại trên điện thoại
- Chạy lại lệnh `adb devices`

---

### Bước 5: Build Mobile App

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Build app (lần đầu mất 5-10 phút)
dotnet build -f net8.0-android
```

**Kết quả mong đợi:**
```
Build succeeded.
    0 Error(s)
```

---

## 🚀 CHẠY DỰ ÁN

### Phương Án 1: Chạy Trên Mạng Cục Bộ (LAN) - Nhanh Nhất

**✅ Yêu cầu**: Điện thoại và máy tính cùng WiFi

---

#### Bước 1: Khởi Động Backend Server

**Terminal 1** (giữ mở):
```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\start_server.ps1
```

Thấy: `Server listening on 0.0.0.0:8080` → **Thành công!**

---

#### Bước 2: Lấy Địa Chỉ IP Máy Tính

**Terminal 2** (mở terminal mới):
```powershell
(Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi*" | Select-Object -First 1).IPAddress
```

**Ví dụ kết quả**: `192.168.1.12`

---

#### Bước 3: Cài App Lên Điện Thoại

**Terminal 2**:
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Cài app
& "C:\Users\[YOUR_USERNAME]\AppData\Local\Android\Sdk\platform-tools\adb.exe" install -r "bin\Debug\net8.0-android\com.fraudguard.ai-Signed.apk"

# Mở app
& "C:\Users\[YOUR_USERNAME]\AppData\Local\Android\Sdk\platform-tools\adb.exe" shell monkey -p com.fraudguard.ai -c android.intent.category.LAUNCHER 1
```

**Hoặc dùng script nhanh**:
```powershell
.\deploy_app.ps1
```

---

#### Bước 4: Cấu Hình App Trên Điện Thoại

**Trên điện thoại:**

1. Mở app **FraudGuard AI**
2. Chọn tab **⚙️ Settings** (ở dưới cùng)
3. **Tắt USB Mode** (toggle từ xanh → xám)
4. Trong field **Server URL**, nhập:
   ```
   http://192.168.1.12:8080
   ```
   (Thay `192.168.1.12` bằng IP máy tính của bạn)
5. Nhấn **Save**
6. Nhấn **Test** → Thấy "✅ Connection successful!" → **OK!**

---

### Phương Án 2: Chạy Qua Internet (Ngrok) - Linh Hoạt Nhất

**✅ Ưu điểm**: Điện thoại có thể dùng 4G, không cần cùng WiFi

---

#### Bước 1: Cài Đặt Ngrok

**Đăng ký tài khoản**:
1. Truy cập: https://ngrok.com/
2. Đăng ký miễn phí
3. Lấy **Authtoken** từ dashboard

**Cài ngrok**:
1. Tải: https://ngrok.com/download
2. Giải nén `ngrok.exe` vào: `E:\FraudGuard-AI\services\api-gateway\`
3. Xác thực:
```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\ngrok.exe config add-authtoken YOUR_AUTHTOKEN_HERE
```

---

#### Bước 2: Khởi Động Backend Server

**Terminal 1** (giữ mở):
```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\start_server.ps1
```

---

#### Bước 3: Khởi Động Ngrok

**Terminal 2** (giữ mở):
```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\ngrok.exe http 8080
```

**Kết quả** sẽ hiện URL:
```
Forwarding   https://652ab192057a.ngrok-free.app -> http://localhost:8080
```

**Copy URL này**: `https://652ab192057a.ngrok-free.app`

---

#### Bước 4: Cài App (giống Phương Án 1)

**Terminal 3**:
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
.\deploy_app.ps1
```

---

#### Bước 5: Cấu Hình App

**Trên điện thoại:**
1. Mở app **FraudGuard AI**
2. Tab **⚙️ Settings**
3. **Tắt USB Mode**
4. Nhập **Server URL**:
   ```
   https://652ab192057a.ngrok-free.app
   ```
   (Thay bằng URL ngrok của bạn)
5. **Save** → **Test** → Thấy "✅ Connection successful!"

---

## ✅ TEST DỰ ÁN

### Test 1: Protection Tab

1. Vào tab **🛡️ Protection**
2. Nhấn **Start Protection** (nút xanh lá lớn)
3. Shield icon chuyển từ xám → xanh → đang hoạt động
4. Nói thử từ khóa lừa đảo: **"chuyển tiền ngay"**
5. Shield sẽ chuyển màu đỏ + rung → **Thành công!**

---

### Test 2: History Tab

1. Nhấn **Stop Protection**
2. Vào tab **📋 History**
3. Sẽ thấy call log vừa test với risk score
4. Pull-to-refresh để reload

---

## 🐛 TROUBLESHOOTING

### Lỗi 1: "go: command not found"

**Nguyên nhân**: Chưa cài Go hoặc chưa restart terminal

**Giải pháp**:
1. Cài lại Go từ https://go.dev/dl/
2. Đóng tất cả terminal
3. Mở terminal mới và thử lại

---

### Lỗi 2: "dotnet: command not found"

**Giải pháp**:
1. Cài .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Restart terminal

---

### Lỗi 3: "CGO_ENABLED=0" SQLite Warning

**Thông báo**:
```
Failed to connect to SQLite database: Binary was compiled with 'CGO_ENABLED=0'
```

**Giải thích**: 
- Đây chỉ là warning, **không ảnh hưởng chức năng chính**
- History sẽ không lưu vào SQLite, nhưng WebSocket vẫn hoạt động bình thường
- Có thể ignore

**Nếu muốn fix** (nâng cao):
```powershell
$env:CGO_ENABLED=1
go build -o bin/server.exe cmd/api/main.go
```

---

### Lỗi 4: "unauthorized" Khi Chạy adb devices

**Giải pháp**:
1. Trên điện thoại sẽ có popup "Allow USB debugging?"
2. Tích ✅ "Always allow from this computer"
3. Nhấn **OK**
4. Chạy lại: `adb devices`

---

### Lỗi 5: "Connection Failed" Trên App

**Nguyên nhân**: Server chưa chạy hoặc sai IP

**Giải pháp**:

**Kiểm tra server đang chạy**:
```powershell
Test-NetConnection -ComputerName localhost -Port 8080 -InformationLevel Quiet
```
Phải thấy: `True`

**Kiểm tra IP đúng**:
```powershell
(Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi*").IPAddress
```

**Kiểm tra Firewall**:
```powershell
# Cho phép port 8080
New-NetFirewallRule -DisplayName "FraudGuard Backend" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow
```

---

### Lỗi 6: "Cannot find the path specified" Khi Build App

**Giải pháp**:
```powershell
# Đảm bảo đúng thư mục
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Restore packages trước
dotnet restore

# Build lại
dotnet build -f net8.0-android
```

---

### Lỗi 7: Ngrok Bị Chặn "ERR_NGROK_..."

**Giải pháp**:
1. Trên điện thoại, khi mở app lần đầu qua ngrok
2. Ngrok sẽ hiện trang cảnh báo
3. Nhấn **Visit Site** để tiếp tục
4. App sẽ kết nối được

---

## 📞 HỖ TRỢ

### Tài Liệu Tham Khảo
- Backend API: `services/api-gateway/README.md`
- Mobile Setup: `mobile/FraudGuard-AI/README.md`
- Test Report: `TEST_REPORT.md`
- Checklist: `UPDATED_CHECKLIST.md`

### Scripts Hữu Ích

**Backend:**
- `START_SERVER.ps1` - Khởi động server
- `setup_ngrok.ps1` - Setup ngrok
- `test_ngrok.ps1` - Test ngrok connection
- `setup_database.ps1` - Tạo database

**Mobile:**
- `deploy_app.ps1` - Build và cài app lên điện thoại
- `DEBUG_CONNECTION.ps1` - Debug kết nối
- `TEST_CONNECTION.ps1` - Test ADB connection

---

## 🎉 HOÀN THÀNH!

Bây giờ bạn đã có:
- ✅ Backend server chạy với Go
- ✅ PostgreSQL database
- ✅ Mobile app Android với .NET MAUI
- ✅ Real-time fraud detection qua WebSocket
- ✅ Deepgram AI speech-to-text
- ✅ Ngrok tunneling (tùy chọn)

**Chúc bạn phát triển thành công!** 🚀

---

## 📝 GHI CHÚ QUAN TRỌNG

1. **Deepgram API Key**: 
   - Bắt buộc để speech-to-text hoạt động
   - Miễn phí $200 credit tại https://deepgram.com/

2. **PostgreSQL Password**: 
   - Ghi nhớ password khi cài đặt
   - Cập nhật vào file `.env`

3. **Ngrok Authtoken**: 
   - Cần để dùng ngrok miễn phí
   - Lấy tại https://dashboard.ngrok.com/

4. **Firewall**: 
   - Nếu không kết nối được, kiểm tra Windows Firewall
   - Cho phép port 8080 inbound

5. **WiFi**: 
   - Khi dùng LAN mode, điện thoại và máy tính **PHẢI cùng WiFi**
   - Nếu không cùng WiFi → Dùng Ngrok

---

**Version**: 1.0.0  
**Last Updated**: February 2, 2026  
**Author**: FraudGuard AI Team
