# Deploy FraudGuard AI lên Render

## Vấn đề gặp phải

### 1. ❌ **Render deploy nhưng app timeout**
**Nguyên nhân:**
- `render.yaml` thiếu `rootDir` → Build sai thư mục
- Database connection chưa setup
- Cold start của Render (10-30s đầu tiên)

### 2. ❌ **Mobile APK không tự update**
**Nguyên nhân:**
- Code push lên GitHub CHỈ update source code
- APK Release cần **build lại thủ công**
- Android không tự động download APK mới từ GitHub

---

## ✅ Giải pháp đã fix

### 1. **Fix Render deployment**

#### A. Sửa `render.yaml`:
```yaml
services:
  - type: web
    name: fraudguard-api
    runtime: go
    rootDir: services/api-gateway  # ✅ Fix build path
    buildCommand: go build -o bin/api ./cmd/api
    startCommand: ./bin/api
    envVars:
      - key: PORT
        value: 8080
      - key: SERVER_HOST
        value: 0.0.0.0
      - key: DEEPGRAM_API_KEY
        sync: false  # Set in Render Dashboard
      - key: DATABASE_URL
        fromDatabase:
          name: fraudguard-db
          property: connectionString
      - key: GO_ENV
        value: production
    healthCheckPath: /health
    branch: main  # Auto-deploy on push
```

#### B. Health check trả về 200 OK ngay cả khi DB down:
```go
// Before: Return 503 if DB down → Render thinks service is broken
// After: Return 200 with "degraded" status → Service still accessible
```

#### C. Tăng timeout mobile app lên 30s:
```csharp
httpClient.Timeout = TimeSpan.FromSeconds(30); // For Render cold starts
```

---

## 🚀 Deploy lên Render

### **Bước 1: Đẩy code lên GitHub**
```powershell
cd E:\FraudGuard-AI
git add .
git commit -m "Fix Render deployment path and health check"
git push origin UImobile
```

### **Bước 2: Merge vào main branch** (để Render auto-deploy)
```powershell
# Option 1: Merge locally
git checkout main
git merge UImobile
git push origin main

# Option 2: Create Pull Request trên GitHub và merge
```

### **Bước 3: Configure Render Dashboard**
1. Vào **https://dashboard.render.com**
2. Chọn service **fraudguard-api**
3. Vào **Environment** → Set:
   - `DEEPGRAM_API_KEY`: (your API key)
   - `DATABASE_URL`: (auto-configured from Render PostgreSQL)
4. **Manual Deploy** hoặc đợi auto-deploy sau khi push

### **Bước 4: Verify deployment**
```powershell
# Test health endpoint
curl https://fraudguard-api.onrender.com/health

# Expected response:
{
  "status": "healthy",
  "service": "FraudGuard AI",
  "database": "connected",
  "message": "All systems operational"
}
```

---

## 📱 Build Mobile App Release

### **Quan trọng:** APK KHÔNG tự động update từ GitHub!

### **Build APK mới:**

#### **Windows (PowerShell):**
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Build Release APK
dotnet publish -f net8.0-android -c Release

# APK output location:
# bin\Release\net8.0-android\publish\com.fraudguardai.app-Signed.apk
```

#### **Upload lên GitHub Release:**
1. Vào **GitHub Repository** → **Releases**
2. Click **"Create a new release"**
3. Tag version: `v1.1.0` (tăng version)
4. Upload APK file từ `bin\Release\net8.0-android\publish\`
5. Publish release

#### **User install:**
- Download APK từ GitHub Releases
- Install trên điện thoại (Allow unknown sources)

---

## 🧪 Test Connection từ Mobile App

### **Test với Render URL:**
1. Mở app FraudGuard AI
2. Vào tab **Settings**
3. Nhập Server URL: `https://fraudguard-api.onrender.com`
4. Device ID: `android_device`
5. Bấm **Test** → Đợi 10-30s (cold start)
6. ✅ Expect: "Connection successful"

### **Test với Local Server:**
1. Start local server:
   ```powershell
   cd E:\FraudGuard-AI\services\api-gateway
   go run cmd\api\main.go
   ```
2. Find your local IP: `ipconfig` → IPv4 Address (e.g., `192.168.1.100`)
3. Trong app Settings:
   - Server URL: `http://192.168.1.100:8080`
   - Bấm Test

---

## ⚠️ Common Issues

### **Issue 1: Timeout khi test connection**
**Nguyên nhân:**
- Render cold start (service ngủ sau 15 phút không dùng)
- Database chưa connect

**Giải pháp:**
- Đợi 30s rồi test lại
- Check Render logs: Dashboard → Logs
- Verify DATABASE_URL đã set

### **Issue 2: CORS Error**
**Triệu chứng:** `Access-Control-Allow-Origin` error

**Giải pháp:** Server đã có CORS middleware:
```go
w.Header().Set("Access-Control-Allow-Origin", "*")
```

### **Issue 3: App không update sau khi push code**
**Nguyên nhân:** APK cần build lại

**Giải pháp:**
1. Build new APK (see above)
2. Tăng version trong `FraudGuardAI.csproj`:
   ```xml
   <ApplicationVersion>2</ApplicationVersion>
   <ApplicationDisplayVersion>1.1.0</ApplicationDisplayVersion>
   ```
3. Upload lên GitHub Releases

---

## 📊 Monitor Render Deployment

### **Check deployment status:**
```powershell
# View live logs
# Vào Render Dashboard → fraudguard-api → Logs

# Or check health endpoint
curl https://fraudguard-api.onrender.com/health
```

### **Expected logs:**
```
🚀 Starting FraudGuard AI API Gateway...
📍 Environment: production
🌐 Host: 0.0.0.0
🔌 Port: 8080
✅ Deepgram client initialized
✅ WebSocket hub started
✅ Server listening on 0.0.0.0:8080
```

---

## 🎯 Next Steps

1. **Push code lên GitHub:**
   ```powershell
   git add .
   git commit -m "Fix Render deployment"
   git push origin UImobile
   ```

2. **Merge vào main** (trigger Render auto-deploy)

3. **Wait 2-3 phút** cho Render build xong

4. **Test connection** từ mobile app với Render URL

5. **Build new APK** nếu có thay đổi mobile code

6. **Upload APK** lên GitHub Releases cho users

---

## 📝 Checklist

- [x] Fix `render.yaml` với `rootDir`
- [x] Health check return 200 OK
- [x] Increase mobile timeout to 30s
- [x] Add detailed logging
- [ ] Push code to GitHub
- [ ] Merge to main branch
- [ ] Verify Render deployment
- [ ] Test connection from mobile app
- [ ] Build new release APK
- [ ] Upload to GitHub Releases

✅ **Done!** Server sẽ auto-deploy khi push lên GitHub main branch!
