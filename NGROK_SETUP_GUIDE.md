# 🌐 NGROK SETUP GUIDE - FraudGuard AI

## 📋 Tổng Quan

Hướng dẫn chi tiết để **expose Backend Go** (localhost:8080) ra Internet public để Mobile App có thể kết nối qua 4G.

---

## 🎯 Mục Tiêu

- Backend đang chạy trên `localhost:8080` (**chỉ truy cập được trong mạng nội bộ**)
- Mobile App cần kết nối qua **4G/5G** (Internet public)
- Dùng **Ngrok** để tạo tunnel: `localhost:8080` ➜ `https://xyz.ngrok-free.app`

---

## ⚡ QUICK START (Tự Động)

### Cách 1: Chạy Script Tự Động

```powershell
# Mở PowerShell và chạy:
cd E:\FraudGuard-AI\services\api-gateway
.\setup_ngrok.ps1
```

Script sẽ tự động:
1. ✅ Kiểm tra Ngrok đã cài chưa
2. ✅ Hướng dẫn lấy auth token (nếu cần)
3. ✅ Kiểm tra Backend đang chạy
4. ✅ Start tunnel và lấy URL public
5. ✅ Hiển thị config cho Mobile App

---

## 🔧 MANUAL SETUP (Từng Bước)

### 📥 Bước 1: Cài Đặt Ngrok

#### Option 1: Chocolatey (Recommended)

```powershell
# Cài Chocolatey (nếu chưa có):
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Cài Ngrok:
choco install ngrok -y

# Refresh PATH:
refreshenv
```

#### Option 2: Manual Download

1. Truy cập: [https://ngrok.com/download](https://ngrok.com/download)
2. Download file `.zip` cho Windows
3. Giải nén vào: `C:\Program Files\ngrok\`
4. Thêm vào PATH:
   ```powershell
   $env:Path += ";C:\Program Files\ngrok"
   ```

### 🔑 Bước 2: Đăng Ký & Lấy Auth Token

1. **Đăng ký FREE** tại: [https://dashboard.ngrok.com/signup](https://dashboard.ngrok.com/signup)
   - Dùng GitHub/Google hoặc Email
   - **100% miễn phí** cho basic usage

2. **Lấy Auth Token**: [https://dashboard.ngrok.com/get-started/your-authtoken](https://dashboard.ngrok.com/get-started/your-authtoken)
   - Copy token (dạng: `2abc...xyz`)

3. **Cấu hình Token**:
   ```powershell
   ngrok config add-authtoken YOUR_TOKEN_HERE
   ```
   
   Example:
   ```powershell
   ngrok config add-authtoken 2abcdefg1234567_hijklmnopqrstuvwxyz
   ```

### 🚀 Bước 3: Start Backend

```powershell
# Terminal 1 (Backend):
cd E:\FraudGuard-AI\services\api-gateway
go run cmd/api/main.go
```

Đợi thấy message:
```
✅ Server listening on 0.0.0.0:8080
✅ WebSocket endpoint: ws://0.0.0.0:8080/ws?device_id=YOUR_DEVICE_ID
```

### 🌍 Bước 4: Start Ngrok Tunnel

```powershell
# Terminal 2 (Ngrok):
ngrok http 8080
```

Hoặc với custom domain (paid plan):
```powershell
ngrok http 8080 --domain=fraudguard.ngrok.app
```

### 📱 Bước 5: Lấy Public URL

Sau khi chạy, bạn sẽ thấy:

```
ngrok

Session Status                online
Account                       YourName (Plan: Free)
Version                       3.5.0
Region                        United States (us)
Latency                       25ms
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc-123-xyz.ngrok-free.app -> http://localhost:8080

Connections                   ttl     opn     rt1     rt5     p50     p90
                              0       0       0.00    0.00    0.00    0.00
```

**Copy địa chỉ HTTPS**: `https://abc-123-xyz.ngrok-free.app`

---

## 📲 CẤU HÌNH MOBILE APP

### Cách 1: Settings Tab (Recommended)

1. **Mở Mobile App** trên điện thoại (kết nối 4G, tắt WiFi)
2. **Tab "⚙️ Settings"**
3. **Mục "Server Address"**:
   ```
   Server IP: abc-123-xyz.ngrok-free.app
   Port: (để trống hoặc 443)
   ```
   
   ⚠️ **QUAN TRỌNG**: 
   - **KHÔNG** nhập `https://` phía trước
   - **KHÔNG** nhập `/ws` phía sau
   - Chỉ nhập domain: `abc-123-xyz.ngrok-free.app`

4. **Tap "💾 Save Settings"**

### Cách 2: Hardcode (Temporary Testing)

Nếu muốn test nhanh, sửa file [SettingsPage.xaml.cs](e:\FraudGuard-AI\mobile\FraudGuard-AI\SettingsPage.xaml.cs):

```csharp
// Line ~60
private void LoadSettings()
{
    // Default to Ngrok URL
    ServerIpEntry.Text = Preferences.Get("server_ip", "abc-123-xyz.ngrok-free.app");
    PortEntry.Text = Preferences.Get("server_port", "443");
    
    // ... rest of code
}
```

Sau đó rebuild app:
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
dotnet build -f net8.0-android
```

---

## 🔐 BACKEND CORS CONFIGURATION

### ✅ Đã Cấu Hình Sẵn

Backend đã có CORS cho phép mọi origin:

📄 **File**: [cmd/api/main.go](e:\FraudGuard-AI\services\api-gateway\cmd\api\main.go#L73-L83)

```go
// CORS middleware (allow all origins for development)
r.Use(func(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        w.Header().Set("Access-Control-Allow-Origin", "*")
        w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
        if r.Method == "OPTIONS" {
            w.WriteHeader(http.StatusOK)
            return
        }
        next.ServeHTTP(w, r)
    })
})
```

### ⚠️ Production: Lock Down CORS

Khi deploy production, nên giới hạn origins:

```go
// CORS middleware (production - specific origins)
r.Use(func(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        allowedOrigins := []string{
            "https://abc-123-xyz.ngrok-free.app",
            "https://fraudguard-production.com",
        }
        
        origin := r.Header.Get("Origin")
        for _, allowed := range allowedOrigins {
            if origin == allowed {
                w.Header().Set("Access-Control-Allow-Origin", origin)
                break
            }
        }
        
        w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
        
        if r.Method == "OPTIONS" {
            w.WriteHeader(http.StatusOK)
            return
        }
        next.ServeHTTP(w, r)
    })
})
```

---

## 🧪 TESTING

### 1️⃣ Test Backend (Local)

```powershell
# Test HTTP:
curl http://localhost:8080/health

# Test WebSocket:
# Dùng tool: https://www.websocket.org/echo.html
# URL: ws://localhost:8080/ws?device_id=test123
```

### 2️⃣ Test Ngrok Tunnel

```powershell
# Test HTTP:
curl https://abc-123-xyz.ngrok-free.app/health

# Expected response:
# {"status":"ok","timestamp":"2026-01-26T..."}
```

### 3️⃣ Test Mobile App

1. **Tắt WiFi**, bật 4G/5G
2. Mở **FraudGuard AI** app
3. Tab **Settings**: Nhập `abc-123-xyz.ngrok-free.app`
4. Tab **Protection**: Tap "🎤 Start Listening"
5. Quan sát:
   - ✅ Status: "🟢 Connected"
   - ✅ Shield màu xanh
   - ✅ Console log: "WebSocket connected"

### 4️⃣ Ngrok Dashboard

Mở browser: [http://localhost:4040](http://localhost:4040)

Dashboard hiển thị:
- 📊 Real-time traffic
- 🔍 Request/Response details
- 📈 Connection statistics

---

## ⚠️ NGROK LIMITATIONS (FREE PLAN)

| Feature | Free Plan | Paid Plan |
|---------|-----------|-----------|
| **Public URL** | Random (thay đổi mỗi lần) | Custom domain |
| **Session Time** | 2 hours | Unlimited |
| **Connections/min** | 40 | Unlimited |
| **Bandwidth** | Limited | Unlimited |
| **Custom Domain** | ❌ | ✅ |
| **Reserved TCP** | ❌ | ✅ |
| **IP Whitelist** | ❌ | ✅ |

### Workarounds:

1. **URL thay đổi**: Update Mobile App Settings mỗi lần restart ngrok
2. **Session timeout**: Restart tunnel sau 2 giờ
3. **Connection limit**: Đủ cho testing, production cần paid plan

---

## 🚀 PRODUCTION ALTERNATIVES

Khi deploy production, thay Ngrok bằng:

### 1. Cloud Deployment (Recommended)

#### Option A: Heroku
```bash
# Free tier available
heroku create fraudguard-api
git push heroku main
# URL: https://fraudguard-api.herokuapp.com
```

#### Option B: Railway.app
```bash
# Free tier: $5 credit/month
railway init
railway up
# URL: https://fraudguard-production.railway.app
```

#### Option C: Fly.io
```bash
# Free tier: 3 VMs
fly launch
fly deploy
# URL: https://fraudguard-ai.fly.dev
```

### 2. VPS Hosting

#### DigitalOcean Droplet ($4/month)
```bash
# SSH to server:
ssh root@your-server-ip

# Install Go, clone repo, run app
# Setup Nginx reverse proxy
# SSL certificate via Let's Encrypt (free)
```

#### Oracle Cloud (Free Forever)
- 2 VMs free forever
- ARM-based instances
- Good for testing/small projects

---

## 🔍 TROUBLESHOOTING

### ❌ "Failed to connect to server"

**Nguyên nhân**:
1. Ngrok chưa chạy
2. Backend chưa chạy
3. URL sai format

**Giải pháp**:
```powershell
# Check backend:
curl http://localhost:8080/health

# Check ngrok:
curl https://your-url.ngrok-free.app/health

# Check mobile app settings:
# Đúng: abc-123.ngrok-free.app
# SAI: https://abc-123.ngrok-free.app/ws
```

### ❌ "Ngrok session expired"

**Nguyên nhân**: Free plan giới hạn 2 giờ

**Giải pháp**:
```powershell
# Restart ngrok:
Ctrl+C  # Stop current tunnel
ngrok http 8080  # Start new tunnel
# Update URL in Mobile App Settings
```

### ❌ "CORS error" trong browser

**Nguyên nhân**: Browser blocking cross-origin requests

**Giải pháp**: 
- Backend đã config CORS `*` (allow all)
- Mobile app không bị CORS (native app)
- Nếu test web dashboard, cần run từ same origin hoặc disable CORS trong browser

---

## 📚 Tài Liệu Tham Khảo

- Ngrok Docs: [https://ngrok.com/docs](https://ngrok.com/docs)
- Ngrok Dashboard: [https://dashboard.ngrok.com](https://dashboard.ngrok.com)
- Chi-Router (CORS): [https://go-chi.io](https://go-chi.io)
- WebSocket over Ngrok: [https://ngrok.com/docs/using-ngrok-with/websockets](https://ngrok.com/docs/using-ngrok-with/websockets)

---

## ✅ CHECKLIST HOÀN THÀNH

- [ ] Ngrok đã cài đặt: `ngrok version`
- [ ] Auth token đã config: `ngrok config check`
- [ ] Backend đang chạy: `curl localhost:8080/health`
- [ ] Ngrok tunnel đang chạy: Dashboard at http://localhost:4040
- [ ] Public URL đã lấy được: `https://xyz.ngrok-free.app`
- [ ] Mobile App Settings đã update: Server IP = `xyz.ngrok-free.app`
- [ ] Test từ 4G thành công: Connected + Shield xanh
- [ ] Dashboard hiển thị traffic: http://localhost:4040

---

## 🎓 NEXT STEPS

1. ✅ **[BE-07] Tunneling (Ngrok)** - COMPLETED với guide này
2. 📝 **[DATA] Import Blacklist Data** - 50-100 số lừa đảo
3. 🧪 **[QA-01] End-to-End Testing** - Full flow test
4. 🎬 **[DEMO] Prepare Resources** - Slides + Video + Script

---

**Created**: January 26, 2026  
**Author**: FraudGuard AI Team  
**Status**: ✅ Ready for Use
