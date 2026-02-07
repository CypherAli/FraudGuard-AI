# 🐛 HƯỚNG DẪN DEBUG KHI APP CRASH

## Vấn đề: App crash ngay khi mở trên điện thoại

### ✅ Các fix đã thực hiện:

1. **MainApplication.cs**: Thêm Firebase initialization với try-catch
2. **MauiProgram.cs**: Thêm error handling toàn diện  
3. **App.xaml.cs**: Thêm error page thay vì crash
4. **Logging**: Thêm debug logging ở mọi bước khởi tạo

---

## 📋 CHECKLIST DEBUG

### 1. Kiểm tra Firebase Configuration

```powershell
# Kiểm tra file google-services.json có tồn tại
Test-Path "e:\FraudGuard-AI\mobile\FraudGuard-AI\Platforms\Android\google-services.json"
```

✅ **Phải trả về: True**

Nếu False:
- Download từ Firebase Console
- Copy vào thư mục Platforms\Android\

### 2. Kiểm tra SHA-1 Fingerprint

```powershell
# Lấy SHA-1 của debug keystore
& "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe" -list -v `
  -keystore "$env:USERPROFILE\.android\debug.keystore" `
  -alias androiddebugkey `
  -storepass android `
  -keypass android | Select-String "SHA1"
```

✅ **SHA-1 này phải được đăng ký trong Firebase Console:**
- Firebase Console → Project Settings → Your apps
- Android app → Add fingerprint
- Paste SHA-1 → Save

### 3. Xem LogCat Realtime

```powershell
# Kết nối điện thoại qua USB → Enable USB Debugging
adb devices

# Xem log của app
adb logcat | Select-String "FraudGuard|MainActivity|MainApplication|MauiProgram"
```

**Cài đặt mỗi lỗi:**

#### Lỗi 1: `Firebase initialization failed`
```
[MainApplication] Firebase init error: ...
```
**Giải pháp:**
- Kiểm tra google-services.json đúng project
- Kiểm tra SHA-1 đã đăng ký
- Re-download google-services.json từ Firebase

#### Lỗi 2: `Java.Lang.ClassNotFoundException`
```
Java.Lang.ClassNotFoundException: Didn't find class ...
```
**Giải pháp:**
```powershell
# Clean và rebuild
dotnet clean
dotnet build -f net8.0-android -c Debug
```

#### Lỗi 3: `Native library not found`
```
java.lang.UnsatisfiedLinkError: dlopen failed: library "..." not found
```
**Giải pháp:**
- Kiểm tra .csproj: `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>`
- Rebuild: `dotnet clean && dotnet build`

#### Lỗi 4: `AndroidX Lifecycle duplicate`
```
Duplicate class androidx.lifecycle.ViewModelKt found in modules
```
**Giải pháp:**
- Đã fix trong .csproj với `<AndroidPackagingOptions>pickFirst</AndroidPackagingOptions>`
- Nếu vẫn lỗi, update packages:
```powershell
dotnet restore --force
```

### 4. Test với Emulator trước

```powershell
# Tạo emulator
avdmanager create avd -n test -k "system-images;android-33;google_apis;x86_64"

# Start emulator
emulator -avd test

# Install APK
adb install -r "bin\Debug\net8.0-android\com.fraudguard.ai-Signed.apk"

# Xem log khi chạy
adb logcat -c  # Clear log
# Mở app trên emulator
adb logcat | Select-String "FraudGuard"
```

### 5. Kiểm tra Permissions

App cần các quyền sau trong AndroidManifest.xml (✅ đã có):
- `RECORD_AUDIO`
- `INTERNET`
- `ACCESS_NETWORK_STATE`
- `FOREGROUND_SERVICE`
- `FOREGROUND_SERVICE_MICROPHONE`
- `POST_NOTIFICATIONS`

### 6. Build Release với Debug Info

```powershell
# Build Release nhưng giữ debug symbols
dotnet build -f net8.0-android -c Release /p:AndroidCreatePackagePerAbi=false /p:AndroidLinkMode=None
```

---

## 🔍 CÁC ĐIỂM KIỂM TRA QUAN TRỌNG

### MainApplication.cs
```csharp
public override void OnCreate()
{
    base.OnCreate();
    try {
        CrossFirebase.Initialize(this);  // ← Phải thành công
    } catch (Exception ex) {
        Debug.WriteLine($"Firebase init error: {ex}");  // ← Xem log này
    }
}
```

### MauiProgram.cs
```csharp
public static MauiApp CreateMauiApp()
{
    try {
        var builder = MauiApp.CreateBuilder();
        // ... setup
        return builder.Build();  // ← Phải build thành công
    } catch (Exception ex) {
        Debug.WriteLine($"CreateMauiApp error: {ex}");  // ← Xem log này
        throw;
    }
}
```

### App.xaml.cs
```csharp
public App()
{
    try {
        InitializeComponent();
        // Nếu crash ở đây, sẽ hiển thị error page
    } catch (Exception ex) {
        MainPage = CreateErrorPage("Lỗi", ex.Message);  // ← Xem error page
    }
}
```

---

## 🚀 QUICK FIX STEPS

### Option 1: Build & Test Immediately

```powershell
cd e:\FraudGuard-AI\mobile\FraudGuard-AI

# Clean build
dotnet clean
dotnet restore
dotnet build -f net8.0-android -c Debug

# Copy APK ra
$apk = Get-ChildItem "bin\Debug\net8.0-android\*.apk" | Select-Object -First 1
Copy-Item $apk "FraudGuard-AI-FIXED.apk"

# Kết nối điện thoại và install
adb install -r FraudGuard-AI-FIXED.apk

# Xem log realtime
adb logcat -c
adb logcat | Select-String "FraudGuard|MainActivity|Exception|Error"
```

### Option 2: Use Build Script

```powershell
cd e:\FraudGuard-AI\mobile\FraudGuard-AI
.\BUILD_APK_FIX_CRASH.ps1
```

---

## 📱 TEST TRÊN ĐIỆN THOẠI

1. **Uninstall phiên bản cũ:**
   ```powershell
   adb uninstall com.fraudguard.ai
   ```

2. **Install phiên bản mới:**
   ```powershell
   adb install -r FraudGuard-AI-FIXED.apk
   ```

3. **Start app và xem log:**
   ```powershell
   adb shell am start -n com.fraudguard.ai/crc64...MainActivity
   adb logcat | Select-String "FraudGuard"
   ```

4. **Nếu crash, lấy full log:**
   ```powershell
   adb logcat -d > crash_log.txt
   ```

---

## 💡 EXPECTED OUTPUT (Thành công)

Khi app chạy thành công, bạn sẽ thấy log như sau:

```
[MainApplication] Initializing Firebase...
[MainApplication] Firebase initialized successfully
[MauiProgram] Starting CreateMauiApp...
[MauiProgram] Registering services...
[MauiProgram] Building app...
[MauiProgram] App built successfully
[App] Initializing App...
[App] Audio service initialized
[App] App initialized successfully
[App] OnStart called
[App] Checking authentication...
[App] User is not authenticated, navigating to LoginPage
```

Nếu thấy log này → App chạy OK! ✅

---

## ❌ COMMON ERRORS & SOLUTIONS

| Error | Solution |
|-------|----------|
| Firebase init failed | Check google-services.json + SHA-1 |
| ClassNotFoundException | dotnet clean && rebuild |
| Native library not found | Set EmbedAssembliesIntoApk=true |
| AndroidX duplicate | Set AndroidPackagingOptions=pickFirst |
| Permission denied | Grant permissions in Settings |
| Connection refused | Check server URL in Settings |

---

## 📞 STILL CRASHING?

Gửi cho tôi:
1. Full crash log: `adb logcat -d > crash_log.txt`
2. Build output log
3. Điện thoại model & Android version
4. Steps to reproduce

Tôi sẽ điều tra ngay! 🔍
