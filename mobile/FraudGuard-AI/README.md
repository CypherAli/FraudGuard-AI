# FraudGuard AI - Mobile App

Ứng dụng di động bảo vệ khỏi lừa đảo qua điện thoại sử dụng AI phân tích realtime.

## 🏗️ Kiến trúc

```
FraudGuard-AI/
├── Constants/           # App-wide constants
│   └── AppConstants.cs
├── Models/             # Data models
│   └── CallLog.cs
├── Services/           # Business logic
│   ├── AudioStreamingServiceLowLevel.cs
│   └── HistoryService.cs
├── Resources/          # Images, fonts, etc.
├── Platforms/          # Platform-specific code
├── MainPage.xaml       # Trang chủ - Bảo vệ realtime
├── HistoryPage.xaml    # Lịch sử cuộc gọi
├── SettingsPage.xaml   # Cấu hình kết nối
└── App.xaml            # Shared resources & styles
```

## 🎨 Features

### ✅ MainPage - Real-time Protection
- 🛡️ Shield animation với trạng thái active/inactive
- 🔴 Alert banner khi phát hiện rủi ro
- 📊 Hiển thị risk score realtime
- 🔊 Audio streaming qua WebSocket
- 📳 Vibration khi nguy hiểm cao

### 📋 HistoryPage
- Danh sách cuộc gọi đã phân tích
- Risk level badges (Safe/Warning/Danger)
- Pull-to-refresh
- Empty state

### ⚙️ SettingsPage
- Cấu hình server IP
- Test connection
- Device ID management
- Hướng dẫn setup

## 🎨 Design System

### Colors (App.xaml)
- **BackgroundDark**: `#0D1B2A` - Background chính
- **CardBackground**: `#1B2838` - Card surfaces
- **SafeColor**: `#34D399` - Trạng thái an toàn
- **DangerColor**: `#F87171` - Cảnh báo nguy hiểm
- **WarningColor**: `#FBBF24` - Cảnh báo trung bình
- **PrimaryBlue**: `#60A5FA` - Accent color

### Shared Styles
- `CardBorder` - Styling cho cards
- `InputBorder` - Styling cho inputs

## 🚀 Setup

### Requirements
- .NET 8.0+
- .NET MAUI Workload
- Android SDK 21+

### Build & Run
```powershell
# Deploy to emulator
.\deploy_app.ps1

# Hoặc manual
dotnet build
dotnet run
```

### Configuration
1. Mở **Settings** trong app
2. Nhập IP server (ví dụ: `192.168.1.100`)
3. Nhấn **Save** và **Test** để kiểm tra

## 📡 API Integration

### WebSocket
```
ws://<SERVER_IP>:8080/ws
```

Gửi audio chunks PCM 16-bit, 16kHz, Mono

### REST API
```
GET /api/history?device_id=<DEVICE_ID>&limit=50
```

## 🔧 Code Structure

### Constants
Tất cả magic numbers và colors được centralize trong `AppConstants.cs`:
- Thresholds
- Animation durations
- Audio configs
- Colors

### Services
- **AudioStreamingServiceLowLevel**: WebSocket + AudioRecord
- **HistoryService**: Fetch call logs

### XAML Pattern
- Shared resources trong `App.xaml`
- Style inheritance
- Data binding với `ObservableCollection`

## 📝 Development Notes

### Animation Timings
- Pulse: 2000ms
- Scale in/out: 150-200ms
- Danger flash: 400ms

### Risk Thresholds
- **High Risk**: ≥ 80%
- **Medium Risk**: 50-79%
- **Low Risk**: < 50%

### Audio Config
- Sample Rate: 16000 Hz
- Channels: Mono (1)
- Format: PCM 16-bit
- Buffer: 4096 bytes

## 🐛 Troubleshooting

### "Cannot connect to server"
- Kiểm tra IP đúng chưa
- Cả 2 devices trên cùng WiFi
- Server đang chạy (`go run main.go`)

### Audio không stream
- Cấp quyền Microphone trong Settings
- Kiểm tra WebSocket connection status

## 📚 Documentation

- [Mobile Deploy Guide](../../MOBILE_DEPLOY_GUIDE.md)
- [Hướng dẫn tiếng Việt](../../HUONG_DAN_CHAY_DIEN_THOAI.md)
- [Device Authorization](../../DEVICE_AUTHORIZATION_REQUIRED.md)

## 🔐 Permissions Required

```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.VIBRATE" />
```

## 🎯 Next Steps

- [ ] iOS support
- [ ] Background recording
- [ ] Local ML inference
- [ ] Call logs export
- [ ] Dark/Light theme toggle
