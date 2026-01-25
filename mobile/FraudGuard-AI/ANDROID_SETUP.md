# Android Setup cho Audio Streaming

## 1. Cài đặt NuGet Packages

Thêm các packages sau vào project `.csproj`:

```xml
<ItemGroup>
  <!-- WebSocket - đã có sẵn trong .NET -->
  
  <!-- Audio Recording -->
  <PackageReference Include="Plugin.AudioRecorder" Version="1.1.0" />
  
  <!-- Permissions -->
  <PackageReference Include="Microsoft.Maui.Essentials" Version="8.0.0" />
</ItemGroup>
```

Hoặc cài qua Package Manager Console:

```powershell
Install-Package Plugin.AudioRecorder -Version 1.1.0
```

## 2. Cấu hình AndroidManifest.xml

File: `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application 
        android:allowBackup="true" 
        android:icon="@mipmap/appicon" 
        android:roundIcon="@mipmap/appicon_round" 
        android:supportsRtl="true">
    </application>
    
    <!-- Quyền ghi âm (BẮT BUỘC) -->
    <uses-permission android:name="android.permission.RECORD_AUDIO" />
    
    <!-- Quyền Internet (BẮT BUỘC cho WebSocket) -->
    <uses-permission android:name="android.permission.INTERNET" />
    
    <!-- Quyền truy cập mạng -->
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    
    <!-- Quyền ghi file (cho audio recording) -->
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" 
                     android:maxSdkVersion="32" />
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" 
                     android:maxSdkVersion="32" />
    
    <!-- Android 13+ storage permissions -->
    <uses-permission android:name="android.permission.READ_MEDIA_AUDIO" />
    
    <!-- Foreground Service (nếu cần chạy background) -->
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE_MICROPHONE" />
    
    <!-- Wake Lock (giữ app không sleep khi recording) -->
    <uses-permission android:name="android.permission.WAKE_LOCK" />
    
    <!-- Vibration (cho cảnh báo nguy hiểm) -->
    <uses-permission android:name="android.permission.VIBRATE" />
    
    <!-- Khai báo audio recording feature -->
    <uses-feature 
        android:name="android.hardware.microphone" 
        android:required="true" />
</manifest>
```

## 3. Cấu hình MainApplication.cs (nếu cần)

File: `Platforms/Android/MainApplication.cs`

```csharp
using Android.App;
using Android.Runtime;

namespace FraudGuardAI
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
```

## 4. Sử dụng AudioStreamingService trong UI

### Ví dụ: MainPage.xaml.cs

```csharp
using FraudGuardAI.Services;

namespace FraudGuardAI;

public partial class MainPage : ContentPage
{
    private AudioStreamingService _audioService;

    public MainPage()
    {
        InitializeComponent();
        
        // Khởi tạo service
        // Sử dụng 10.0.2.2 cho Android Emulator
        // Hoặc IP LAN thực tế cho thiết bị vật lý
        _audioService = new AudioStreamingService("ws://10.0.2.2:8080/ws");
        
        // Đăng ký events
        _audioService.AlertReceived += OnAlertReceived;
        _audioService.ErrorOccurred += OnErrorOccurred;
        _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    private async void OnStartButtonClicked(object sender, EventArgs e)
    {
        var success = await _audioService.StartStreamingAsync();
        if (success)
        {
            StatusLabel.Text = "Đang ghi âm và streaming...";
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
    }

    private async void OnStopButtonClicked(object sender, EventArgs e)
    {
        await _audioService.StopStreamingAsync();
        StatusLabel.Text = "Đã dừng";
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }

    private void OnAlertReceived(object sender, AlertEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var alert = e.Alert;
            DisplayAlert(
                $"⚠️ CẢNH BÁO: {alert.AlertType}",
                $"Độ tin cậy: {alert.Confidence:P}\n" +
                $"Transcript: {alert.Transcript}\n" +
                $"Keywords: {string.Join(", ", alert.Keywords)}",
                "OK"
            );
        });
    }

    private void OnErrorOccurred(object sender, Services.ErrorEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DisplayAlert("Lỗi", e.Message, "OK");
        });
    }

    private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionStatusLabel.Text = e.IsConnected 
                ? "✅ Đã kết nối" 
                : "❌ Ngắt kết nối";
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _audioService?.Dispose();
    }
}
```

## 5. Kiểm tra IP cho WebSocket

### Android Emulator:
- Sử dụng: `ws://10.0.2.2:8080/ws`
- `10.0.2.2` là IP đặc biệt trỏ tới `localhost` của máy host

### Thiết bị vật lý (Real Device):
- Tìm IP LAN của máy chạy backend:
  ```powershell
  ipconfig
  # Tìm IPv4 Address (ví dụ: 192.168.1.100)
  ```
- Sử dụng: `ws://192.168.1.100:8080/ws`
- Đảm bảo cả máy tính và điện thoại cùng mạng WiFi

## 6. Debug và Troubleshooting

### Xem logs trong Visual Studio:
- Output Window → Debug
- Hoặc Android Device Log (Logcat)

### Kiểm tra quyền:
```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
Console.WriteLine($"Microphone permission: {status}");
```

### Test WebSocket connection:
```csharp
var connected = await _audioService.ConnectAsync();
Console.WriteLine($"Connected: {connected}");
```

## 7. Build và Deploy

### Debug Build:
```powershell
dotnet build -f net8.0-android
```

### Deploy lên Emulator:
```powershell
dotnet build -t:Run -f net8.0-android
```

### Deploy lên thiết bị vật lý:
1. Bật Developer Mode trên Android
2. Bật USB Debugging
3. Kết nối USB
4. Run từ Visual Studio

## 8. Performance Tips

- **Buffer Size**: 4096 bytes là tối ưu cho realtime streaming
- **Sample Rate**: 16000 Hz là yêu cầu của Deepgram
- **Network**: Sử dụng WiFi thay vì 4G/5G để giảm latency
- **Battery**: Cân nhắc sử dụng Foreground Service cho recording lâu dài
