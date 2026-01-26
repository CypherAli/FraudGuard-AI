# 📱 HƯỚNG DẪN CHẠY FRAUDGUARD AI TRÊN ĐIỆN THOẠI

**Cập nhật:** 26/01/2026  
**Thời gian:** ~10-15 phút  
**Yêu cầu:** Điện thoại Android, máy tính Windows có Visual Studio

---

## 🎯 TỔNG QUAN

FraudGuard AI là ứng dụng phát hiện cuộc gọi lừa đảo theo thời gian thực. Hướng dẫn này sẽ giúp bạn:
1. Cài đặt app lên điện thoại
2. Kết nối với server
3. Test tính năng phát hiện lừa đảo

---

## ✅ BƯỚC 1: CHUẨN BỊ ĐIỆN THOẠI

### 1.1. Bật chế độ Developer (Nhà phát triển)

**Trên điện thoại Android:**

1. Vào **Settings** (Cài đặt)
2. Kéo xuống chọn **About Phone** (Thông tin điện thoại)
3. Tìm dòng **Build Number** (Số bản dựng)
4. **Nhấn 7 lần** vào Build Number
5. Nhập mật khẩu/vân tay nếu được yêu cầu
6. Sẽ thấy thông báo: *"You are now a developer!"*

### 1.2. Bật USB Debugging

1. Quay lại **Settings** → **System** → **Developer Options**
   - Nếu không thấy Developer Options, tìm trong **Additional Settings**
2. Bật **USB Debugging** (ON)
3. Chấp nhận cảnh báo bảo mật

### 1.3. Kết nối máy tính

1. **Cắm cáp USB** từ điện thoại vào máy tính
2. Trên điện thoại, chọn chế độ **File Transfer** (Truyền file) hoặc **MTP**
3. Nếu thấy popup *"Allow USB debugging?"* → Chọn **Always allow** và **OK**

---

## 💻 BƯỚC 2: KHỞI ĐỘNG SERVER

### 2.1. Kiểm tra PostgreSQL Database

Mở **PowerShell** và chạy:

```powershell
# Kiểm tra container database
docker ps --filter "name=fraudguard"
```

**Kết quả mong đợi:** Thấy container `fraudguard-db` đang chạy (Up)

Nếu không chạy:
```powershell
docker start fraudguard-db
```

### 2.2. Khởi động API Server

```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\start_server.ps1
```

**Chờ** cho đến khi thấy dòng:
```
✓ Server listening on 0.0.0.0:8080
✓ WebSocket hub started
```

⚠️ **QUAN TRỌNG:** Để cửa sổ PowerShell này mở, **KHÔNG ĐÓNG**.

### 2.3. Khởi động Ngrok Tunnel

Mở **PowerShell mới** (cửa sổ thứ 2):

```powershell
cd E:\FraudGuard-AI\services\api-gateway
ngrok http 8080
```

Hoặc nếu ngrok chưa có trong PATH:
```powershell
C:\Users\trinh\Downloads\ngrok.exe http 8080
```

**Chờ** đến khi thấy:
```
Forwarding    https://xxxx-xxxx.ngrok-free.app -> http://localhost:8080
```

### 2.4. Lấy URL Ngrok

Trong PowerShell thứ 3, chạy:

```powershell
cd E:\FraudGuard-AI\services\api-gateway
.\get_ngrok_url.ps1
```

**Ghi chú lại URL**, ví dụ: `86d51f22e8fb.ngrok-free.app`

---

## 📲 BƯỚC 3: CÀI ĐẶT APP LÊN ĐIỆN THOẠI

### 3.1. Chạy script tự động

Mở **PowerShell thứ 4**:

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
.\clean_install.ps1
```

**Quá trình này sẽ:**
- ✓ Kiểm tra điện thoại đã kết nối chưa
- ✓ Xóa app cũ (nếu có)
- ✓ Build app mới (mất ~2-5 phút lần đầu)
- ✓ Tự động cài lên điện thoại

**Chờ đợi** cho đến khi thấy:
```
=== DEPLOY COMPLETE ===
```

### 3.2. Kiểm tra app đã cài

Trên điện thoại, mở **App Drawer** (danh sách ứng dụng), tìm:
- 🛡️ **FraudGuard AI**

---

## ⚙️ BƯỚC 4: CẤU HÌNH APP

### 4.1. Mở app và cấp quyền

1. **Mở app** FraudGuard AI
2. App sẽ yêu cầu quyền **Microphone** (Ghi âm)
   - Chọn **Allow** (Cho phép)
3. Nếu hỏi thêm quyền **Phone**, **Storage** → Chọn **Allow**

### 4.2. Nhập Server URL

1. Nhấn vào tab **Settings** (biểu tượng ⚙️ ở góc phải)
2. Trong ô **Server IP/URL**, nhập URL Ngrok (đã lấy ở bước 2.4):
   ```
   86d51f22e8fb.ngrok-free.app
   ```
   ⚠️ **KHÔNG nhập** `https://` hay `ws://`, chỉ nhập domain

3. Nhấn **Save** hoặc **Connect**

### 4.3. Kiểm tra kết nối

Chuyển sang tab **Protection** (🛡️):
- Biểu tượng lá chắn phải chuyển từ **Xám** → **Xanh dương**
- Trạng thái hiển thị: *"Connected"* hoặc *"Đã kết nối"*

**Nếu Shield vẫn màu xám:**
1. Kiểm tra lại URL đã đúng chưa
2. Kiểm tra Ngrok tunnel có đang chạy không
3. Thử nhấn nút **Reconnect** trong Settings

---

## 🧪 BƯỚC 5: TEST TÍNH NĂNG

### Test 1: Phát hiện số Blacklist

1. Mở ứng dụng **Phone** (Điện thoại) của Android
2. Nhập số: **0911333444**
3. **Nhấn gọi** (hoặc giả lập cuộc gọi đến)

**Kết quả mong đợi:**
- ⚠️ Shield chuyển **ĐỎ** ngay lập tức
- 📳 Điện thoại **rung 3 lần**
- 📢 Hiển thị cảnh báo:
  ```
  🚨 CRITICAL THREAT
  Giả danh Đại úy công an: Báo liên quan đường dây rửa tiền xuyên quốc gia
  Risk Score: 95%
  ```

### Test 2: Phát hiện từ khóa nguy hiểm

1. Gọi số bình thường (bạn bè/gia đình)
2. Trong cuộc gọi, **nói thử** các từ khóa:
   - "Bộ Công an"
   - "chuyển tiền"
   - "mã OTP"
   - "tài khoản ngân hàng"
   - "căn cước công dân"

**Kết quả mong đợi:**
- Shield đổi màu theo mức độ nguy hiểm:
  - 🟡 **Vàng** (cảnh báo nhẹ): 1-2 từ khóa
  - 🟠 **Cam** (nguy hiểm): 3-4 từ khóa
  - 🔴 **Đỏ** (rất nguy hiểm): 5+ từ khóa
- Risk Score tăng dần: 20% → 50% → 85%

### Test 3: Xem lịch sử

1. Chuyển sang tab **History** (📊)
2. Xem danh sách các cuộc gọi vừa test
3. Mỗi cuộc gọi hiển thị:
   - 📞 Số điện thoại
   - 🎯 Risk Score (%)
   - ⏰ Thời gian
   - 🔴/🟢 Trạng thái (Nguy hiểm/An toàn)

---

## 🎯 DANH SÁCH SỐ ĐIỆN THOẠI TEST

Trong database có **39 số lừa đảo** để test. Top 5 số nguy hiểm nhất:

| Số điện thoại | Mô tả | Risk Score |
|--------------|-------|------------|
| `0988111222` | Giả danh VKSND Tối cao | 98% |
| `0933444555` | App chứa mã độc | 97% |
| `0912999888` | App giả Bộ Công an | 96% |
| `0868123123` | Deepfake video call | 96% |
| `0911333444` | Giả danh Đại úy công an | 95% |

**Thử gọi các số này** để xem app phản ứng như thế nào!

---

## ❓ XỬ LÝ SỰ CỐ

### Sự cố 1: "Cannot connect to server"

**Nguyên nhân:**
- Server chưa chạy
- Ngrok tunnel chưa hoạt động
- Firewall chặn kết nối

**Cách khắc phục:**
```powershell
# 1. Kiểm tra server
netstat -ano | findstr :8080

# 2. Kiểm tra Ngrok
Invoke-RestMethod http://localhost:4040/api/tunnels

# 3. Restart server
cd E:\FraudGuard-AI\services\api-gateway
.\start_server.ps1
```

### Sự cố 2: Shield không đổi màu khi gọi số blacklist

**Nguyên nhân:**
- App chưa kết nối server
- Database chưa có dữ liệu

**Cách khắc phục:**
```powershell
# Kiểm tra database có dữ liệu
docker exec -i fraudguard-db psql -U fraudguard -d fraudguard_db -c "SELECT COUNT(*) FROM blacklist;"

# Nếu trả về 0, import lại dữ liệu:
cd E:\FraudGuard-AI\services\api-gateway
Get-Content seed_data.sql | docker exec -i fraudguard-db psql -U fraudguard -d fraudguard_db
```

### Sự cố 3: "WebSocket connection failed"

**Nguyên nhân:**
- URL Ngrok sai
- Dùng WiFi công ty có chặn WebSocket
- Ngrok session hết hạn

**Cách khắc phục:**
1. Thử dùng **mạng 4G** thay vì WiFi
2. Lấy lại URL Ngrok mới:
   ```powershell
   .\get_ngrok_url.ps1
   ```
3. Nhập lại URL trong Settings tab

### Sự cố 4: App không record audio

**Nguyên nhân:**
- Chưa cấp quyền Microphone

**Cách khắc phục:**
1. Vào **Settings** → **Apps** → **FraudGuard AI**
2. Chọn **Permissions**
3. Bật **Microphone** (ON)
4. Restart app

### Sự cố 5: Ngrok URL thay đổi sau khi restart

**Nguyên nhân:**
- Ngrok free plan tạo URL ngẫu nhiên mỗi lần chạy

**Cách khắc phục:**
1. Lấy URL mới:
   ```powershell
   .\get_ngrok_url.ps1
   ```
2. Vào app Settings → Nhập URL mới → Save
3. **HOẶC** nâng cấp Ngrok lên paid plan để có URL cố định

---

## 📊 KIẾN TRÚC HỆ THỐNG

```
┌─────────────────┐
│  📱 Mobile App  │ (Điện thoại Android)
│  FraudGuard AI  │
└────────┬────────┘
         │ WebSocket (wss://)
         ▼
┌─────────────────┐
│   🌐 Ngrok      │ (Public Tunnel)
│  xxxx.ngrok.app │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  🖥️ API Server  │ (localhost:8080)
│   Go Backend    │
└────────┬────────┘
         │
         ├──► 🗄️ PostgreSQL (Blacklist DB)
         ├──► 🎤 Deepgram API (Speech-to-Text)
         └──► 🤖 Gemini AI (Fraud Detection)
```

---

## 📚 CÁC FILE QUAN TRỌNG

| File | Mô tả | Đường dẫn |
|------|-------|-----------|
| `start_server.ps1` | Khởi động API server | `services/api-gateway/` |
| `get_ngrok_url.ps1` | Lấy URL Ngrok | `services/api-gateway/` |
| `clean_install.ps1` | Deploy app lên điện thoại | `mobile/FraudGuard-AI/` |
| `seed_data.sql` | Dữ liệu 39 số lừa đảo | `services/api-gateway/` |
| `.env` | Cấu hình server | `services/api-gateway/` |

---

## 🎓 GHI CHÚ BỔ SUNG

### Deepgram API Key

App sử dụng Deepgram để chuyển giọng nói thành text. API key đã được cấu hình trong file `.env`:

```
DEEPGRAM_API_KEY=41b6d70eb5a731165dde1eee393277fc9563a128
```

⚠️ **Lưu ý:** Đây là API key test, có giới hạn 200 phút/tháng miễn phí.

### Cấu trúc Database

**Table: blacklist**
- `phone_number` - Số điện thoại
- `reason` - Lý do (mô tả thủ đoạn lừa đảo)
- `confidence_score` - Điểm nguy hiểm (0.0 - 1.0)
- `reported_count` - Số lần bị báo cáo
- `status` - Trạng thái (active/inactive)

---

## 🚀 NÂNG CAO

### Sử dụng Domain tùy chỉnh (thay vì Ngrok)

Nếu muốn dùng domain riêng (ví dụ: `fraudguard.yourdomain.com`):

1. Thuê VPS (DigitalOcean, AWS EC2, Vultr...)
2. Deploy server lên VPS
3. Cấu hình Nginx reverse proxy
4. Cài SSL certificate (Let's Encrypt)
5. Cập nhật DNS record

### Triển khai Production

```bash
# Build Release APK
cd mobile/FraudGuard-AI
dotnet publish -f net8.0-android -c Release

# APK output:
# bin/Release/net8.0-android/publish/com.fraudguard.ai-Signed.apk
```

### Tối ưu hiệu năng

- Bật **Battery Optimization exclusion** cho app
- Dùng **Foreground Service** để app chạy background
- Cache kết quả AI để giảm API calls

---

## 📞 HỖ TRỢ

Nếu gặp vấn đề, kiểm tra logs:

**Server logs:**
```powershell
# Trong cửa sổ PowerShell đang chạy server
# Xem output real-time
```

**Mobile logs:**
- Trong Visual Studio: **View** → **Output** → Chọn **Debug**
- Hoặc dùng `adb logcat` để xem Android logs

---

**Chúc bạn test thành công! 🎉**

*Nếu có câu hỏi, liên hệ: trinhviethoang@example.com*
