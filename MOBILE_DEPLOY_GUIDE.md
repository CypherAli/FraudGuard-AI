# ===========================================
# HƯỚNG DẪN CHẠY APP TRÊN ĐIỆN THOẠI THẬT
# ===========================================

## 📱 BƯỚC 1: CÀI ĐẶT APP LÊN ĐIỆN THOẠI

### Option A: Build và Deploy từ Visual Studio (Khuyến nghị)

1. **Mở project trong Visual Studio:**
   ```powershell
   cd E:\FraudGuard-AI\mobile\FraudGuard-AI
   start FraudGuardAI.csproj
   ```

2. **Kết nối điện thoại:**
   - Bật **USB Debugging** trên Android (Settings → Developer Options)
   - Cắm dây USB vào máy tính
   - Chọn "Transfer Files" khi điện thoại hỏi

3. **Deploy trong Visual Studio:**
   - Chọn target: **Android** (không phải Android Emulator)
   - Chọn device của bạn trong dropdown (ví dụ: "Samsung Galaxy")
   - Nhấn **F5** hoặc **Debug → Start Debugging**
   - Chờ build xong (~2-5 phút lần đầu)

### Option B: Build APK và cài thủ công

```powershell
cd E:\FraudGuard-AI\mobile\FraudGuard-AI
dotnet publish -f net8.0-android -c Release
```

APK sẽ nằm trong:
`bin\Release\net8.0-android\publish\`

Gửi file .apk qua email/Telegram rồi cài trên điện thoại.

---

## 🌐 BƯỚC 2: CẤU HÌNH KẾT NỐI SERVER

### Thông tin Ngrok Tunnel hiện tại:

```
📡 Public URL: https://86d51f22e8fb.ngrok-free.app
🔌 WebSocket:  wss://86d51f22e8fb.ngrok-free.app/ws
```

### Cách nhập trong App:

1. **Mở app** trên điện thoại
2. **Chuyển sang tab "Settings"** (biểu tượng ⚙️)
3. **Nhập thông tin:**
   - **Server URL**: `86d51f22e8fb.ngrok-free.app`
   - **KHÔNG CẦN** https:// hay ws://
   - App sẽ tự động thêm scheme phù hợp

4. **Nhấn "Save" hoặc "Connect"**

---

## 🧪 BƯỚC 3: TEST CHỨC NĂNG

### Test 1: Kiểm tra kết nối
1. Mở tab **Protection** (biểu tượng lá chắn 🛡️)
2. Shield phải chuyển từ Gray → **Blue** (đang kết nối)
3. Nếu thấy lỗi → kiểm tra lại URL

### Test 2: Kiểm tra số Blacklist
1. Gọi điện thoại thử (hoặc dùng simulator)
2. Nhập số: **0911333444** (Số lừa đảo giả danh công an)
3. Shield phải chuyển **RED** ngay lập tức
4. Điện thoại rung (vibrate)
5. Hiển thị thông báo: "CRITICAL - Giả danh Đại úy công an..."

### Test 3: Kiểm tra từ khóa (Keyword Detection)
1. Nói thử các từ khóa nguy hiểm:
   - "Bộ Công an"
   - "chuyển tiền"
   - "tài khoản ngân hàng"
   - "OTP"
   - "căn cước công dân"

2. Shield phải đổi màu dần:
   - **Yellow** (cảnh báo nhẹ)
   - **Orange** (nguy hiểm)
   - **Red** (rất nguy hiểm)

### Test 4: Xem lịch sử
1. Chuyển sang tab **History**
2. Phải thấy danh sách cuộc gọi vừa test
3. Mỗi cuộc gọi hiển thị:
   - Số điện thoại
   - Risk Score (0-100)
   - Thời gian

---

## ❗ TROUBLESHOOTING

### Lỗi "Cannot connect to server"
```powershell
# Kiểm tra server đang chạy
netstat -ano | findstr :8080

# Kiểm tra Ngrok tunnel
Invoke-RestMethod http://localhost:4040/api/tunnels
```

### Lỗi "WebSocket connection failed"
- Kiểm tra firewall có block không
- Thử dùng **4G** thay vì Wifi công ty
- Restart app

### App không record audio
- Vào Settings → Apps → FraudGuard AI → Permissions
- Bật **Microphone** permission

### Ngrok URL bị đổi
Mỗi lần restart Ngrok, URL sẽ thay đổi (free plan).
Cách fix:
```powershell
# Lấy URL mới
Invoke-RestMethod http://localhost:4040/api/tunnels | 
  Select-Object -ExpandProperty tunnels | 
  Select-Object -First 1 -ExpandProperty public_url
```

---

## 🎥 DEMO SCRIPT (Khi giới thiệu)

### Kịch bản Demo 1: Cuộc gọi lừa đảo giả danh công an
```
📞 Số gọi đến: 0911333444
🗣️ Nội dung: "Chào anh, đây là Đại úy Nguyễn Văn A từ Bộ Công an..."

✅ Kết quả mong đợi:
- Shield đỏ ngay lập tức
- Rung điện thoại 3 lần
- Hiển thị: "CRITICAL - Giả danh cơ quan công quyền"
```

### Kịch bản Demo 2: Phát hiện từ khóa nguy hiểm
```
🗣️ Nói trong cuộc gọi:
"Anh cần chuyển tiền vào tài khoản này để kích hoạt thẻ, 
nhớ đọc mã OTP cho tôi nhé"

✅ Kết quả mong đợi:
- Risk Score tăng dần: 30 → 65 → 85
- Shield đổi màu: Blue → Yellow → Orange → Red
```

### Kịch bản Demo 3: Cuộc gọi bình thường
```
📞 Số gọi: 0987654321 (không có trong blacklist)
🗣️ Nội dung: "Hẹn gặp bạn chiều nay ở quán cafe nhé"

✅ Kết quả mong đợi:
- Shield giữ màu Blue (an toàn)
- Risk Score < 30
- Không rung
```

---

## 📊 THỐNG KÊ DỮ LIỆU BLACKLIST HIỆN TẠI

```
Tổng số records: 39
├─ CRITICAL (≥90%): 20 số
├─ HIGH (80-89%):    9 số
├─ MEDIUM (70-79%):  5 số
└─ LOW (<70%):       5 số
```

**Top 5 số nguy hiểm nhất:**
1. `0988111222` - Giả danh VKSND (98%)
2. `0933444555` - App chứa mã độc (97%)
3. `0912999888` - App giả Bộ Công an (96%)
4. `0868123123` - Deepfake video call (96%)
5. `0911333444` - Giả danh công an (95%)

---

**Chúc bạn demo thành công! 🎉**

_Nếu gặp vấn đề, hãy kiểm tra logs trong Visual Studio Output window._
