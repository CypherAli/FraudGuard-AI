# 🚀 HƯỚNG DẪN CHẠY FRAUDGUARD AI - ĐẦY ĐỦ TỪ A-Z

> **Hướng dẫn chi tiết cho người mới bắt đầu**  
> Tài liệu này sẽ hướng dẫn bạn từ cài đặt đến chạy thành công toàn bộ hệ thống FraudGuard AI

**Ngày cập nhật:** 31/01/2026  
**Thời gian hoàn thành:** ~30-45 phút  
**Hệ điều hành:** Windows 10/11

---

## 📋 MỤC LỤC

1. [Giới thiệu](#giới-thiệu)
2. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
3. [Bước 1: Cài đặt công cụ cần thiết](#bước-1-cài-đặt-công-cụ-cần-thiết)
4. [Bước 2: Thiết lập Database](#bước-2-thiết-lập-database)
5. [Bước 3: Khởi động API Server](#bước-3-khởi-động-api-server)
6. [Bước 4: Cài đặt và chạy Ngrok](#bước-4-cài-đặt-và-chạy-ngrok)
7. [Bước 5: Kiểm tra hệ thống](#bước-5-kiểm-tra-hệ-thống)
8. [Bước 6: Chạy ứng dụng Mobile](#bước-6-chạy-ứng-dụng-mobile)
9. [Xử lý lỗi thường gặp](#xử-lý-lỗi-thường-gặp)
10. [Tổng kết](#tổng-kết)

---

## 🎯 GIỚI THIỆU

**FraudGuard AI** là hệ thống phát hiện cuộc gọi lừa đảo theo thời gian thực sử dụng AI. Hệ thống gồm:

- **Backend API Server** (Go): Xử lý logic và kết nối AI
- **Database** (PostgreSQL): Lưu trữ blacklist và lịch sử cuộc gọi
- **Mobile App** (.NET MAUI): Ứng dụng Android để người dùng sử dụng
- **Ngrok**: Tunnel để expose server ra internet (cho testing)

---

## 💻 YÊU CẦU HỆ THỐNG

### Phần cứng tối thiểu:
- **CPU:** Intel Core i5 hoặc tương đương
- **RAM:** 8GB trở lên (khuyến nghị 16GB)
- **Ổ cứng:** 10GB dung lượng trống
- **Mạng:** Kết nối internet ổn định

### Phần mềm cần cài đặt:
- ✅ Windows 10/11 (64-bit)
- ✅ Docker Desktop
- ✅ Go (Golang) phiên bản 1.22+
- ✅ Git
- ✅ Visual Studio 2022 (cho mobile app)
- ✅ Ngrok (optional, cho public testing)

---

## 🛠️ BƯỚC 1: CÀI ĐẶT CÔNG CỤ CẦN THIẾT

### 1.1. Cài đặt Git

1. Tải Git từ: https://git-scm.com/download/win
2. Chạy file cài đặt, chọn **Next** với các tùy chọn mặc định
3. Hoàn tất cài đặt

**Kiểm tra:**
```powershell
git --version
```
**Kết quả mong đợi:** `git version 2.x.x`

### 1.2. Cài đặt Docker Desktop

1. Tải Docker Desktop từ: https://www.docker.com/products/docker-desktop
2. Chạy file **Docker Desktop Installer.exe**
3. Chờ cài đặt hoàn tất (khoảng 5-10 phút)
4. Khởi động lại máy tính nếu được yêu cầu
5. Mở **Docker Desktop** và đợi Docker khởi động hoàn toàn

**Kiểm tra:**
```powershell
docker --version
docker ps
```
**Kết quả mong đợi:**
```
Docker version 28.x.x
CONTAINER ID   IMAGE     COMMAND   CREATED   STATUS    PORTS     NAMES
```

### 1.3. Cài đặt Go (Golang)

1. Tải Go từ: https://go.dev/dl/
2. Tải file **go1.25.x.windows-amd64.msi** (phiên bản mới nhất)
3. Chạy file MSI và làm theo hướng dẫn
4. Khởi động lại PowerShell

**Kiểm tra:**
```powershell
go version
```
**Kết quả mong đợi:** `go version go1.25.x windows/amd64`

### 1.4. Cài đặt Ngrok

1. Tạo tài khoản miễn phí tại: https://ngrok.com/
2. Tải Ngrok cho Windows: https://ngrok.com/download
3. Giải nén file ZIP vào thư mục bất kỳ (ví dụ: `C:\Users\<tên-bạn>\Downloads\`)
4. Mở PowerShell tại thư mục chứa ngrok.exe
5. Lấy auth token từ dashboard ngrok và chạy:

```powershell
.\ngrok.exe config add-authtoken <YOUR_AUTH_TOKEN>
```

**Kiểm tra:**
```powershell
ngrok config check
```
**Kết quả mong đợi:** `Valid configuration file at...`

### 1.5. Clone Source Code

```powershell
cd E:\
git clone https://github.com/CypherAli/FraudGuard-AI.git
cd FraudGuard-AI
```

---

## 🗄️ BƯỚC 2: THIẾT LẬP DATABASE

### 2.1. Khởi động Docker Desktop

1. Mở **Docker Desktop**
2. Đợi Docker chạy hoàn toàn (icon Docker trên taskbar không còn animation)

### 2.2. Khởi động PostgreSQL Container

Mở **PowerShell** và chạy:

```powershell
cd E:\FraudGuard-AI\services\api-gateway
docker-compose up -d
```

**Giải thích:**
- `docker-compose up`: Khởi động các service được định nghĩa trong `docker-compose.yml`
- `-d`: Chạy ở chế độ background (detached)

**Kết quả mong đợi:**
```
[+] Running 2/2
 ✔ Network api-gateway_default  Created
 ✔ Container fraudguard-db      Started
```

### 2.3. Kiểm tra Database đang chạy

```powershell
docker ps --filter "name=fraudguard"
```

**Kết quả mong đợi:**
```
CONTAINER ID   IMAGE                COMMAND                  STATUS
xxxxx          postgres:16-alpine   "docker-entrypoint..."   Up X minutes (healthy)
```

**Lưu ý:** Cột **STATUS** phải có `Up` và `healthy`

### 2.4. Khởi tạo dữ liệu mẫu (Optional)

Database sẽ tự động tạo tables khi server khởi động lần đầu. Nếu muốn thêm dữ liệu mẫu:

```powershell
cd E:\FraudGuard-AI\services\api-gateway
Get-Content seed_data.sql | docker exec -i fraudguard-db psql -U fraudguard -d fraudguard_db
```

---

## ⚡ BƯỚC 3: KHỞI ĐỘNG API SERVER

### 3.1. Chuyển đến thư mục API Gateway

```powershell
cd E:\FraudGuard-AI\services\api-gateway
```

### 3.2. Cài đặt Go Dependencies

```powershell
go mod download
```

**Chờ tải các package cần thiết (khoảng 1-2 phút)**

### 3.3. Khởi động Server

**Cách 1: Chạy trực tiếp trong PowerShell hiện tại**
```powershell
go run .\cmd\api\main.go
```

**Cách 2: Chạy trong cửa sổ PowerShell riêng (Khuyến nghị)**
```powershell
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location 'E:\FraudGuard-AI\services\api-gateway'; go run .\cmd\api\main.go"
```

**Kết quả mong đợi:**
```
2026/01/31 22:45:06  Starting FraudGuard AI API Gateway...
2026/01/31 22:45:06  Database connection established (Max: 25, Min: 5)
2026/01/31 22:45:06  Deepgram client initialized
2026/01/31 22:45:06  WebSocket hub started
2026/01/31 22:45:06  Server listening on 0.0.0.0:8080
2026/01/31 22:45:06  WebSocket endpoint: ws://0.0.0.0:8080/ws?device_id=YOUR_DEVICE_ID
```

**⚠️ QUAN TRỌNG:** 
- Để cửa sổ PowerShell này **MỞ** và **KHÔNG ĐÓNG**
- Server cần chạy liên tục để mobile app có thể kết nối

### 3.4. Kiểm tra Server hoạt động

Mở **PowerShell mới** (cửa sổ thứ 2) và chạy:

```powershell
curl.exe http://localhost:8080/health
```

**Kết quả mong đợi:**
```json
{"database":"connected","service":"FraudGuard AI","status":"healthy"}
```

---

## 🌐 BƯỚC 4: CÀI ĐẶT VÀ CHẠY NGROK

Ngrok giúp expose server local ra internet để test từ điện thoại thật hoặc cho người khác truy cập.

### 4.1. Khởi động Ngrok

Mở **PowerShell mới** (cửa sổ thứ 3):

```powershell
ngrok http 8080
```

**Hoặc nếu ngrok không có trong PATH:**
```powershell
C:\Users\<tên-bạn>\Downloads\ngrok.exe http 8080
```

**Kết quả mong đợi:**
```
ngrok                                                                     
                                                                          
Session Status     online                                                
Account            <your-email>                                          
Version            3.x.x                                                 
Region             Vietnam (vn)                                          
Forwarding         https://xxxx-xxxx.ngrok-free.app -> http://localhost:8080
                                                                          
Connections        ttl     opn     rt1     rt5     p50     p90          
                   0       0       0.00    0.00    0.00    0.00         
```

**⚠️ QUAN TRỌNG:**
- Ghi lại URL **https://xxxx-xxxx.ngrok-free.app** (URL của bạn sẽ khác)
- Để cửa sổ PowerShell này **MỞ** và **KHÔNG ĐÓNG**
- URL này sẽ thay đổi mỗi lần khởi động ngrok (free tier)

### 4.2. Lấy Ngrok URL bằng API

Mở **PowerShell mới** (cửa sổ thứ 4):

```powershell
curl.exe http://localhost:4040/api/tunnels | ConvertFrom-Json | Select-Object -ExpandProperty tunnels | Select-Object -ExpandProperty public_url
```

**Kết quả:** Sẽ in ra URL ngrok của bạn

### 4.3. Kiểm tra Ngrok hoạt động

```powershell
curl.exe https://<your-ngrok-url>.ngrok-free.app/health
```

**Ví dụ:**
```powershell
curl.exe https://98597b36b7d5.ngrok-free.app/health
```

**Kết quả mong đợi:**
```json
{"database":"connected","service":"FraudGuard AI","status":"healthy"}
```

---

## ✅ BƯỚC 5: KIỂM TRA HỆ THỐNG

### 5.1. Kiểm tra tất cả các service đang chạy

```powershell
# Kiểm tra Docker Container
docker ps --filter "name=fraudguard"

# Kiểm tra API Server
curl.exe http://localhost:8080/health

# Kiểm tra Ngrok
curl.exe http://localhost:4040/api/tunnels
```

### 5.2. Test các API Endpoints

#### Test Health Check
```powershell
curl.exe http://localhost:8080/health
```

**Kết quả:**
```json
{"database":"connected","service":"FraudGuard AI","status":"healthy"}
```

#### Test Blacklist API
```powershell
curl.exe http://localhost:8080/api/blacklist
```

**Kết quả:** Trả về danh sách số điện thoại lừa đảo (JSON)

#### Test Check Phone Number
```powershell
curl.exe "http://localhost:8080/api/check?phone=0988111222"
```

**Kết quả:**
```json
{"is_blacklist":true,"phone_number":"0988111222","success":true}
```

#### Test với số điện thoại an toàn
```powershell
curl.exe "http://localhost:8080/api/check?phone=0123456789"
```

**Kết quả:**
```json
{"is_blacklist":false,"phone_number":"0123456789","success":true}
```

### 5.3. Lấy địa chỉ IP LAN (để kết nối từ điện thoại)

```powershell
$lanIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" } | Select-Object -First 1).IPAddress
Write-Host "Địa chỉ IP LAN của bạn: $lanIP"
```

**Kết quả:** `Địa chỉ IP LAN của bạn: 192.168.1.12` (IP của bạn có thể khác)

### 5.4. Test kết nối qua LAN IP

```powershell
curl.exe http://192.168.1.12:8080/health
```

**Nếu thành công:** Hệ thống đã sẵn sàng cho mobile app kết nối!

---

## 📱 BƯỚC 6: CHẠY ỨNG DỤNG MOBILE

### 6.1. Chuẩn bị điện thoại Android

#### A. Bật chế độ Developer (Nhà phát triển)

1. Vào **Settings** (Cài đặt)
2. Chọn **About Phone** (Thông tin điện thoại)
3. Tìm **Build Number** (Số bản dựng)
4. **Nhấn 7 lần** vào Build Number
5. Nhập mật khẩu nếu được yêu cầu
6. Thấy thông báo: *"You are now a developer!"*

#### B. Bật USB Debugging

1. Quay lại **Settings** → **System** → **Developer Options**
2. Bật **USB Debugging** (ON)
3. Chấp nhận cảnh báo

#### C. Kết nối máy tính

1. Cắm cáp USB từ điện thoại vào máy tính
2. Chọn chế độ **File Transfer** hoặc **MTP**
3. Chấp nhận popup *"Allow USB debugging?"* → **Always allow** → **OK**

### 6.2. Cài đặt Visual Studio 2022

1. Tải Visual Studio 2022 Community: https://visualstudio.microsoft.com/
2. Trong installer, chọn workload:
   - ✅ **.NET Multi-platform App UI development**
   - ✅ **Mobile development with .NET**
3. Cài đặt (có thể mất 30-60 phút)

### 6.3. Mở và Build Mobile App

1. Mở **Visual Studio 2022**
2. Chọn **Open a project or solution**
3. Duyệt đến: `E:\FraudGuard-AI\mobile\FraudGuard-AI\FraudGuardAI.csproj`
4. Chờ Visual Studio load project và restore packages

### 6.4. Cấu hình Server URL

Mở file `mobile/FraudGuard-AI/Constants/AppConstants.cs` và cập nhật:

```csharp
public static class AppConstants
{
    // Sử dụng 1 trong 3 URL sau:
    
    // Option 1: Ngrok (khuyến nghị cho testing)
    public const string API_BASE_URL = "https://98597b36b7d5.ngrok-free.app";
    
    // Option 2: LAN IP (nếu điện thoại cùng WiFi)
    // public const string API_BASE_URL = "http://192.168.1.12:8080";
    
    // Option 3: Localhost (chỉ cho emulator)
    // public const string API_BASE_URL = "http://10.0.2.2:8080";
}
```

### 6.5. Deploy ứng dụng lên điện thoại

#### Cách 1: Deploy từ Visual Studio (Khuyến nghị)

1. Trong Visual Studio, chọn target device là điện thoại Android của bạn
2. Nhấn **F5** hoặc nút **▶ Run**
3. Chờ build và deploy (lần đầu có thể mất 5-10 phút)

#### Cách 2: Sử dụng PowerShell script

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
.\deploy_app.ps1
```

### 6.6. Sử dụng App trên điện thoại

1. Mở app **FraudGuard AI** trên điện thoại
2. Vào **Settings** (⚙️)
3. Nhập **Server URL** (ngrok URL hoặc LAN IP)
4. Nhấn **Save** và **Test Connection**
5. Nếu thấy ✅ **Connected** → Thành công!

### 6.7. Test tính năng phát hiện lừa đảo

1. Quay lại **Home** tab
2. Nhập số điện thoại test: **0988111222** (đây là số lừa đảo trong database)
3. Nhấn **Check**
4. Kết quả: **⚠️ CẢNH BÁO: Số này đã bị báo cáo lừa đảo!**

---

## 🔧 XỬ LÝ LỖI THƯỜNG GẶP

### Lỗi 1: Docker không khởi động

**Triệu chứng:**
```
Cannot connect to the Docker daemon
```

**Giải pháp:**
1. Mở **Docker Desktop**
2. Đợi Docker khởi động hoàn toàn (5-10 phút lần đầu)
3. Thử lại lệnh

---

### Lỗi 2: Go không tìm thấy

**Triệu chứng:**
```
go: command not found
```

**Giải pháp:**
1. Kiểm tra Go đã cài đặt: `go version`
2. Nếu chưa có, cài đặt lại Go từ https://go.dev/dl/
3. Khởi động lại PowerShell sau khi cài đặt

---

### Lỗi 3: Port 8080 đã được sử dụng

**Triệu chứng:**
```
bind: address already in use
```

**Giải pháp:**
1. Tìm process đang dùng port 8080:
```powershell
netstat -ano | findstr :8080
```

2. Kill process (thay `<PID>` bằng Process ID từ bước 1):
```powershell
taskkill /PID <PID> /F
```

3. Hoặc đổi port trong code (không khuyến nghị)

---

### Lỗi 4: Ngrok connection failed

**Triệu chứng:**
```
ERR_NGROK_108: The account is limited to 1 online tunnel
```

**Giải pháp:**
1. Thoát tất cả ngrok instances cũ
2. Chạy lại: `ngrok http 8080`
3. Nếu vẫn lỗi, upgrade ngrok hoặc dùng LAN IP thay vì Ngrok

---

### Lỗi 5: Mobile app không kết nối được server

**Triệu chứng:**
```
Connection timeout / Network error
```

**Giải pháp:**

#### A. Nếu dùng Ngrok:
1. Kiểm tra ngrok đang chạy: Mở http://localhost:4040
2. Copy đúng URL từ ngrok (có https://)
3. Nhập lại URL trong app Settings

#### B. Nếu dùng LAN IP:
1. Kiểm tra điện thoại và máy tính **cùng WiFi**
2. Kiểm tra Firewall không chặn port 8080:
```powershell
New-NetFirewallRule -DisplayName "FraudGuard API" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow
```
3. Test lại bằng browser điện thoại: `http://192.168.1.12:8080/health`

---

### Lỗi 6: Database connection failed

**Triệu chứng:**
```
Failed to connect to database
```

**Giải pháp:**
1. Kiểm tra container đang chạy:
```powershell
docker ps --filter "name=fraudguard"
```

2. Nếu container stopped, khởi động lại:
```powershell
docker start fraudguard-db
```

3. Nếu vẫn lỗi, xóa và tạo lại container:
```powershell
cd E:\FraudGuard-AI\services\api-gateway
docker-compose down
docker-compose up -d
```

---

### Lỗi 7: Visual Studio không build được

**Triệu chứng:**
```
NETSDK1005: Assets file project.assets.json not found
```

**Giải pháp:**
1. Restore NuGet packages:
```
Right-click solution → Restore NuGet Packages
```

2. Clean và Rebuild:
```
Build → Clean Solution
Build → Rebuild Solution
```

---

## 📊 TỔNG KẾT

### Checklist hoàn thành:

- [x] ✅ Docker Desktop đã cài đặt và chạy
- [x] ✅ PostgreSQL database container đang hoạt động
- [x] ✅ Go đã cài đặt (version 1.22+)
- [x] ✅ API Server đang chạy trên port 8080
- [x] ✅ Ngrok tunnel đã tạo và có public URL
- [x] ✅ Các API endpoints test thành công
- [x] ✅ Mobile app build và deploy lên điện thoại
- [x] ✅ App kết nối được với server
- [x] ✅ Tính năng phát hiện lừa đảo hoạt động

### Thông tin quan trọng cần lưu lại:

| Thông tin | Giá trị | Ghi chú |
|-----------|---------|---------|
| **API Server Local** | http://localhost:8080 | Chỉ truy cập được từ máy tính |
| **API Server LAN** | http://192.168.1.12:8080 | Truy cập từ điện thoại cùng WiFi |
| **Ngrok Public URL** | https://xxxx.ngrok-free.app | Thay đổi mỗi lần restart ngrok |
| **Database Port** | 5433 | PostgreSQL container |
| **Ngrok Dashboard** | http://localhost:4040 | Xem traffic và requests |

### Các lệnh hữu ích:

```powershell
# Khởi động tất cả services
cd E:\FraudGuard-AI
.\START_SERVER_COMPLETE.ps1

# Dừng API Server
Ctrl + C (trong cửa sổ PowerShell chạy server)

# Dừng Ngrok
Ctrl + C (trong cửa sổ PowerShell chạy ngrok)

# Dừng Database
docker stop fraudguard-db

# Khởi động lại Database
docker start fraudguard-db

# Xem logs của Database
docker logs fraudguard-db -f

# Xem tất cả containers
docker ps -a
```

### Cấu trúc thư mục project:

```
FraudGuard-AI/
├── services/
│   └── api-gateway/          # Backend API Server (Go)
│       ├── cmd/api/main.go   # Entry point
│       ├── internal/         # Business logic
│       ├── pkg/             # Shared packages
│       └── docker-compose.yml # Database config
├── mobile/
│   └── FraudGuard-AI/       # Mobile App (.NET MAUI)
│       ├── MainPage.xaml    # UI chính
│       ├── HistoryPage.xaml # Lịch sử cuộc gọi
│       └── Constants/       # Config (Server URL)
└── README.md
```

---

## 🎓 HƯỚNG DẪN SỬ DỤNG CHO NGƯỜI DÙNG CUỐI

### Kịch bản 1: Kiểm tra số điện thoại trước khi nhận cuộc gọi

1. Mở app **FraudGuard AI**
2. Nhập số điện thoại vào ô **Enter Phone Number**
3. Nhấn nút **🔍 Check**
4. Xem kết quả:
   - ✅ **Số an toàn** → Có thể nhận cuộc gọi
   - ⚠️ **Cảnh báo lừa đảo** → Không nên nhận

### Kịch bản 2: Xem lịch sử các cuộc gọi đã kiểm tra

1. Nhấn tab **📜 History** ở bottom navigation
2. Xem danh sách các số đã check
3. Lọc theo:
   - 🔴 **Fraud Only**: Chỉ hiển thị số lừa đảo
   - 📅 **Date**: Lọc theo ngày

### Kịch bản 3: Cài đặt và cấu hình

1. Nhấn tab **⚙️ Settings**
2. Nhập **Server URL** (ngrok hoặc LAN IP)
3. Nhấn **💾 Save Settings**
4. Nhấn **🔌 Test Connection** để kiểm tra
5. Nếu thấy ✅ → Đã kết nối thành công!

---

## 🚀 TRIỂN KHAI PRODUCTION (NÂNG CAO)

### Cho môi trường thật (không dùng Ngrok):

1. **Thuê VPS/Cloud Server** (AWS, DigitalOcean, Azure...)
2. **Cài đặt Docker trên server**
3. **Deploy code lên server:**
```bash
git clone https://github.com/CypherAli/FraudGuard-AI.git
cd FraudGuard-AI/services/api-gateway
docker-compose up -d
go build -o fraudguard-api ./cmd/api
./fraudguard-api
```

4. **Cấu hình domain và SSL:**
   - Mua domain (ví dụ: fraudguard.com)
   - Cài đặt Nginx reverse proxy
   - Cài SSL certificate (Let's Encrypt)

5. **Cập nhật mobile app:**
   - Đổi `API_BASE_URL` thành domain thật
   - Build release APK và upload lên Google Play Store

---

## 📞 HỖ TRỢ VÀ LIÊN HỆ

- **GitHub Issues:** https://github.com/CypherAli/FraudGuard-AI/issues
- **Email:** support@fraudguard.ai
- **Documentation:** Xem thêm tại `README.md` trong project

---

## 📝 CHANGELOG

### Version 1.0 (31/01/2026)
- ✅ Backend API Server với Go
- ✅ Database PostgreSQL
- ✅ Mobile App .NET MAUI
- ✅ Tính năng check blacklist
- ✅ Lịch sử cuộc gọi
- ✅ WebSocket real-time (đang phát triển)
- ✅ Tích hợp AI (Deepgram + Gemini)

---

## 🎉 CHÚC MỪNG!

Bạn đã hoàn thành việc cài đặt và chạy thành công **FraudGuard AI**! 

Nếu gặp bất kỳ vấn đề nào, hãy xem lại phần [Xử lý lỗi thường gặp](#xử-lý-lỗi-thường-gặp) hoặc liên hệ team support.

**Happy Coding! 🚀**

---

*Tài liệu này được tạo bởi Team ABSOLUTEGW - Swin Hackathon 2026*
