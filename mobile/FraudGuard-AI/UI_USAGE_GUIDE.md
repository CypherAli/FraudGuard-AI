# 🛡️ FraudGuard AI - Hướng dẫn sử dụng UI

## 📱 Giao diện đã tạo

### MainPage.xaml + MainPage.xaml.cs

Giao diện hiện đại với các tính năng:

✅ **Shield Icon (Biểu tượng khiên)** - Thay đổi màu theo trạng thái  
✅ **Trạng thái rõ ràng** - "Đang được bảo vệ" / "Chưa kích hoạt"  
✅ **Nút bật/tắt lớn** - Dễ sử dụng  
✅ **Chế độ Nguy hiểm** - Màn hình đỏ rực + rung khi phát hiện lừa đảo  
✅ **Animation mượt mà** - Pulse effect, fade transitions  
✅ **Thread-safe** - Tất cả UI updates đều chạy trên Main Thread  

---

## 🎨 Chế độ màu sắc

### 1. **Chế độ Không hoạt động** (Inactive)
- Background: Xanh đậm (#0A1929)
- Shield: Xám, mờ (opacity 0.5)
- Status: "Chưa kích hoạt"

### 2. **Chế độ Bảo vệ** (Protected - Safe)
- Background: Xanh đậm (#0A1929)
- Shield: Xanh lá (#4CAF50), sáng, có hiệu ứng pulse
- Status: "🔒 Đang được bảo vệ"
- Button: Đỏ "TẮT BẢO VỆ"

### 3. **Chế độ Nguy hiểm** (Danger - High Risk)
- Background: **ĐỎ RỰC** (#B71C1C)
- Shield: Đỏ nhạt
- Status: "🚨 PHÁT HIỆN LỪA ĐẢO"
- Alert Banner: Hiện to
- Vibration: Rung 2 lần
- Flash Animation: Nhấp nháy 3 lần

---

## ⚙️ Cấu hình WebSocket

### Trong file `MainPage.xaml.cs`, dòng 17:

```csharp
private const string WEBSOCKET_URL = "ws://10.0.2.2:8080/ws";
```

### Thay đổi theo môi trường:

**Android Emulator:**
```csharp
private const string WEBSOCKET_URL = "ws://10.0.2.2:8080/ws";
```

**Thiết bị thật (Real Device):**
```csharp
// Tìm IP của máy tính (ipconfig)
private const string WEBSOCKET_URL = "ws://192.168.1.100:8080/ws";
```

---

## 🎯 Logic xử lý Alert

### Risk Score Threshold (Ngưỡng nguy hiểm)

```csharp
private const double HIGH_RISK_THRESHOLD = 80.0;
```

### Khi nhận Alert từ Server:

**Nếu `Confidence * 100 >= 80` (Nguy hiểm cao):**
1. ✅ Chuyển toàn bộ màn hình sang **ĐỎ RỰC**
2. ✅ Rung điện thoại (Vibration pattern)
3. ✅ Hiện Alert Banner lớn
4. ✅ Flash animation (nhấp nháy 3 lần)
5. ✅ Popup cảnh báo với chi tiết

**Nếu `Confidence * 100 < 80` (Rủi ro thấp):**
1. ✅ Hiện Alert Banner nhỏ (màu cam)
2. ✅ Tự động ẩn sau 5 giây
3. ❌ Không đổi màu màn hình
4. ❌ Không rung

---

## 🔧 Điều chỉnh Namespace

### Kiểm tra namespace trong các file:

**MainPage.xaml.cs:**
```csharp
namespace FraudGuardAI
{
    public partial class MainPage : ContentPage
    {
        // ...
    }
}
```

**AudioStreamingServiceLowLevel.cs:**
```csharp
namespace FraudGuardAI.Services
{
    public class AudioStreamingServiceLowLevel : IDisposable
    {
        // ...
    }
}
```

**Nếu namespace khác, hãy sửa cho khớp!**

---

## 🚀 Chạy thử Demo

### Bước 1: Build project
```powershell
dotnet build -f net8.0-android
```

### Bước 2: Chạy trên Emulator
```powershell
dotnet build -t:Run -f net8.0-android
```

### Bước 3: Test flow

1. **Mở app** → Thấy shield xám, status "Chưa kích hoạt"
2. **Nhấn "BẬT BẢO VỆ"** → Shield chuyển xanh lá, có hiệu ứng pulse
3. **Nói từ khóa lừa đảo** (ví dụ: "chuyển tiền", "mã OTP")
4. **Backend phát hiện** → Gửi Alert JSON với `confidence > 0.8`
5. **Màn hình chuyển ĐỎ RỰC ngay lập tức** ⚡
6. **Điện thoại rung** 📳
7. **Popup cảnh báo hiện ra** 🚨

---

## 🐛 Troubleshooting

### Lỗi: "Type or namespace 'AudioStreamingServiceLowLevel' could not be found"

**Giải pháp:**
- Kiểm tra namespace trong `AudioStreamingServiceLowLevel.cs`
- Đảm bảo file nằm trong folder `Services/`
- Rebuild project

### Lỗi: "Vibration not working"

**Giải pháp:**
Thêm permission vào `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.VIBRATE" />
```

### Lỗi: "WebSocket connection failed"

**Giải pháp:**
1. Kiểm tra backend có chạy không: `http://10.0.2.2:8080`
2. Thử đổi IP nếu dùng thiết bị thật
3. Xem debug log ở `DebugLabel` (góc dưới màn hình)

### Shield icon không hiện

**Giải pháp:**
- Emoji shield `🛡️` có thể không hiện trên một số emulator
- Thay bằng icon khác hoặc dùng image file

---

## 🎨 Tùy chỉnh giao diện

### Đổi màu chủ đạo

**Trong `MainPage.xaml`, phần Resources:**

```xml
<!-- Safe Mode Colors -->
<Color x:Key="SafePrimaryColor">#1E88E5</Color>  <!-- Xanh dương -->
<Color x:Key="SafeAccentColor">#4CAF50</Color>   <!-- Xanh lá -->

<!-- Danger Mode Colors -->
<Color x:Key="DangerBackgroundColor">#B71C1C</Color>  <!-- Đỏ đậm -->
```

### Thay đổi ngưỡng nguy hiểm

**Trong `MainPage.xaml.cs`:**
```csharp
private const double HIGH_RISK_THRESHOLD = 70.0;  // Giảm xuống 70%
```

### Tắt Debug Label (Production)

**Trong `MainPage.xaml`, dòng cuối:**
```xml
<Label x:Name="DebugLabel"
       IsVisible="False"/>  <!-- Đổi thành False -->
```

---

## 📊 Event Flow Diagram

```
User nhấn "BẬT BẢO VỆ"
    ↓
StartProtectionAsync()
    ↓
AudioService.StartStreamingAsync()
    ↓
Microphone bắt đầu ghi
    ↓
Audio chunks → WebSocket → Backend
    ↓
Backend phân tích (Deepgram + AI)
    ↓
Phát hiện lừa đảo → Gửi Alert JSON
    ↓
OnAlertReceived() event
    ↓
MainThread.BeginInvokeOnMainThread()
    ↓
if (RiskScore >= 80)
    ↓
AnimateToDangerMode() → Đỏ rực
TriggerVibration() → Rung
DangerFlashAnimation() → Nhấp nháy
DisplayAlert() → Popup
```

---

## ✅ Checklist hoàn thành

- [x] MainPage.xaml - Giao diện hiện đại với shield icon
- [x] MainPage.xaml.cs - Logic xử lý alerts và animations
- [x] Tích hợp AudioStreamingServiceLowLevel
- [x] Xử lý High Risk Alert (màn hình đỏ + rung)
- [x] Xử lý Low Risk Alert (banner nhỏ)
- [x] Thread-safe UI updates
- [x] Animations mượt mà
- [x] Debug logging
- [x] Lifecycle management (cleanup on exit)

---

## 🎬 Demo Script

**Để gây ấn tượng:**

1. Mở app, giải thích: "Đây là FraudGuard AI, bảo vệ cuộc gọi khỏi lừa đảo"
2. Nhấn "BẬT BẢO VỆ" → Shield sáng xanh lá
3. Nói: "Anh cần chuyển tiền ngay để nhận quà" (từ khóa lừa đảo)
4. **BOOM!** Màn hình chuyển đỏ rực, rung, popup cảnh báo
5. Giải thích: "AI đã phát hiện nguy cơ lừa đảo với độ tin cậy 95%"

**Hiệu ứng WOW đảm bảo!** 🎉
