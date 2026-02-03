# 🚀 HƯỚNG DẪN PHÂN PHỐI APP CHO NHIỀU NGƯỜI DÙNG

## ⚡ CÁCH NHANH NHẤT: DÙNG DEBUG APK

### Bước 1: Build Debug APK
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
dotnet build -f net8.0-android -c Debug
```

### Bước 2: Lấy file APK
File APK sẽ nằm tại:
```
E:\FraudGuard-AI\mobile\FraudGuard-AI\bin\Debug\net8.0-android\com.fraudguard.ai-Signed.apk
```

### Bước 3: Upload lên Cloud
1. **Google Drive** (Khuyến nghị):
   - Upload file APK lên Google Drive
   - Chuột phải → Get link → Anyone with the link → Copy link
   - Chia sẻ link cho người dùng

2. **Dropbox**:
   - Upload file APK
   - Share → Create link → Copy link

3. **WeTransfer** (tạm thời):
   - https://wetransfer.com/
   - Upload và gửi link

### Bước 4: Hướng dẫn người dùng cài đặt

Gửi cho họ:

```
📱 Cài đặt FraudGuard AI

1. Tải file APK từ link: [LINK CỦA BẠN]

2. Mở Settings trên điện thoại → Security
   Bật "Install unknown apps" hoặc "Unknown sources"

3. Mở file APK vừa tải → Nhấn Install

4. Mở app → Vào Settings tab:
   - Tắt USB Mode
   - Nhập Server URL: [URL SERVER CỦA BẠN]
   - Nhấn Test → Save

5. Vào Protection tab → Start Protection
```

---

## 🔥 PHƯƠNG ÁN PRO: DÙNG FIREBASE APP DISTRIBUTION

### Ưu điểm:
- Quản lý testers
- Auto-update notification
- Tracking downloads
- Professional

### Setup Firebase:

#### Bước 1: Tạo Firebase Project
1. Truy cập: https://console.firebase.google.com/
2. Add project → Nhập tên: `FraudGuard AI`
3. Disable Google Analytics (không cần)
4. Create project

#### Bước 2: Thêm Android App
1. Click biểu tượng Android
2. Android package name: `com.fraudguard.ai`
3. App nickname: `FraudGuard AI`
4. Register app

#### Bước 3: Setup Firebase CLI
```powershell
npm install -g firebase-tools
firebase login
firebase apps:sdkconfig android com.fraudguard.ai
```

#### Bước 4: Upload APK
```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI

# Build APK
dotnet build -f net8.0-android -c Debug

# Upload to Firebase
firebase appdistribution:distribute `
  bin\Debug\net8.0-android\com.fraudguard.ai-Signed.apk `
  --app [YOUR_FIREBASE_APP_ID] `
  --groups "testers" `
  --release-notes "Phiên bản đầu tiên - Fraud Detection System"
```

#### Bước 5: Mời Testers
1. Vào Firebase Console → App Distribution
2. Testers & Groups → Add testers
3. Nhập email người dùng
4. Họ sẽ nhận email với link download

---

## 📦 SERVER SETUP CHO NHIỀU NGƯỜI DÙNG

### Option 1: Deploy Server lên Cloud

**Heroku** (Miễn phí):
```bash
# Cài Heroku CLI
# https://devcenter.heroku.com/articles/heroku-cli

cd E:\FraudGuard-AI\services\api-gateway

# Login
heroku login

# Tạo app
heroku create fraudguard-api

# Deploy
git push heroku main

# Lấy URL
heroku info
# URL sẽ là: https://fraudguard-api.herokuapp.com
```

### Option 2: Dùng Ngrok Pro

```powershell
# Upgrade Ngrok account: https://dashboard.ngrok.com/billing/subscription
# Với paid plan, bạn có:
# - Fixed domain (không đổi URL)
# - Không giới hạn connections
# - IP whitelisting

# Start với custom domain
cd E:\FraudGuard-AI\services\api-gateway
.\start_server.ps1

# Terminal mới:
ngrok http 8080 --domain=your-custom-domain.ngrok-free.app
```

### Option 3: VPS/Cloud Server

**DigitalOcean** ($5/tháng):
1. Tạo Droplet Ubuntu
2. SSH vào server
3. Cài Go: `sudo apt install golang-go`
4. Clone repo: `git clone https://github.com/YOUR_USERNAME/FraudGuard-AI.git`
5. Setup PostgreSQL
6. Run server: `./start_server.sh`
7. Cấu hình firewall mở port 8080
8. Dùng IP public của server

---

## 📊 MONITORING & UPDATES

### Track Usage:
- Firebase Analytics
- Sentry for crash reporting
- Custom backend logging

### Push Updates:
1. Build APK mới với version tăng lên
2. Upload lên Firebase App Distribution
3. Users sẽ nhận notification update

---

## ⚠️ LƯU Ý QUAN TRỌNG

### Bảo Mật:
- [ ] Đổi Deepgram API key thành key riêng
- [ ] Đổi PostgreSQL password
- [ ] Setup HTTPS cho production
- [ ] Rate limiting trên server
- [ ] Input validation

### Legal:
- [ ] Thêm Privacy Policy trong app
- [ ] Thêm Terms of Service
- [ ] Tuân thủ GDPR nếu có users EU
- [ ] Xin permission recording audio rõ ràng

### Performance:
- [ ] Test với nhiều concurrent users
- [ ] Monitor server load
- [ ] Setup database backup
- [ ] CDN cho static assets

---

## 🎯 CHECKLIST TRƯỚC KHI PHÂN PHỐI

- [ ] Test app trên nhiều thiết bị khác nhau
- [ ] Test với network chậm (3G)
- [ ] Verify backend đang chạy ổn định
- [ ] Database có đủ 42 số blacklist
- [ ] Server URL trong app đúng
- [ ] Viết hướng dẫn sử dụng cho user
- [ ] Chuẩn bị support channel (Telegram/Discord)
- [ ] Có plan backup cho server downtime

---

**Version**: 1.0  
**Last Updated**: February 3, 2026
