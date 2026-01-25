# 🚀 Quick Setup Guide: Call History Feature

## Backend Setup (Go)

### 1. Install Dependencies
```bash
cd e:\FraudGuard-AI\services\api-gateway
go mod tidy
```

### 2. Start Server
```bash
go run cmd/api/main.go
```

**Expected Output:**
```
✅ SQLite database initialized successfully (fraud_guard.db)
✅ Server listening on :8080
📡 WebSocket endpoint: ws://:8080/ws?device_id=YOUR_DEVICE_ID
```

### 3. Test API
```bash
curl "http://localhost:8080/api/history?device_id=test_device&limit=10"
```

---

## Mobile Setup (.NET MAUI)

### 1. Add Navigation Button to MainPage

Update `MainPage.xaml` - add button before the toggle button:

```xml
<!-- History Button -->
<Button x:Name="HistoryButton"
        Grid.Row="2"
        Text="📋 XEM LỊCH SỬ"
        FontSize="18"
        FontAttributes="Bold"
        HeightRequest="50"
        CornerRadius="25"
        BackgroundColor="#4CAF50"
        TextColor="White"
        Clicked="OnHistoryButtonClicked"
        Margin="0,0,0,10"/>
```

Update `MainPage.xaml.cs` - add click handler:

```csharp
private async void OnHistoryButtonClicked(object sender, EventArgs e)
{
    await Navigation.PushAsync(new HistoryPage());
}
```

### 2. Configure IP Address

**For Emulator** (default): No changes needed - uses `10.0.2.2:8080`

**For Real Device**: 
1. Find your computer's IP: `ipconfig` → Look for IPv4 Address (e.g., `192.168.1.100`)
2. Update `Services/HistoryService.cs` line 21:
   ```csharp
   private const string EMULATOR_BASE_URL = "http://192.168.1.100:8080";
   ```

### 3. Build and Run
```bash
cd e:\FraudGuard-AI\mobile\FraudGuard-AI
dotnet build -f net8.0-android
dotnet build -t:Run -f net8.0-android
```

---

## Testing Flow

1. **Start Backend**: `go run cmd/api/main.go`
2. **Open Mobile App**
3. **Tap "BẬT BẢO VỆ"** → Shield turns green
4. **Speak fraud keywords**: "chuyển tiền", "mã OTP", "ngân hàng"
5. **Tap "TẮT BẢO VỆ"** → Session ends, log saved to database
6. **Tap "📋 XEM LỊCH SỬ"** → See call log with red card showing high risk score
7. **Pull down to refresh** → Reload history

---

## Expected Results

### Backend Logs:
```
🔍 [test_device] Analyzing text: chuyển tiền ngay
🔴 [test_device] Critical keyword detected: 'chuyển tiền' (+50 points)
🚨 [test_device] CRITICAL ALERT: Score=50
🛑 [test_device] Session ended - Duration: 30s, RiskScore: 50, IsFraud: false
✅ [test_device] Call log saved successfully (ID: 1)
```

### Mobile App:
- History page shows card with:
  - 🚨 "NGUY HIỂM" badge (if score ≥ 60)
  - Red background
  - Risk score displayed
  - Evidence: "Patterns: CRITICAL: chuyển tiền (+50)"

---

## Troubleshooting

**Backend: "fraud_guard.db" not created**
- Check write permissions in project directory
- Verify `repository.InitSQLite()` is called in main.go

**Mobile: "Không thể kết nối tới server"**
- Emulator: Ensure backend running on `localhost:8080`
- Real device: Update IP in `HistoryService.cs` and ensure same WiFi network

**No call logs appearing**
- Verify `device_id` matches (default: "test_device")
- Check backend logs for "Session ended" message
- Test API directly: `curl http://localhost:8080/api/history`

---

## Files Modified

**Backend:**
- ✅ `go.mod` - Added GORM dependencies
- ✅ `cmd/api/main.go` - Added SQLite init and history route
- ✅ `internal/models/call_log.go` - NEW
- ✅ `internal/repository/database.go` - NEW
- ✅ `internal/handlers/history_handler.go` - NEW
- ✅ `internal/services/fraud_detector.go` - Added EndSession()
- ✅ `internal/hub/client.go` - Call EndSession() on disconnect

**Mobile:**
- ✅ `Models/CallLog.cs` - NEW
- ✅ `Services/HistoryService.cs` - NEW
- ✅ `HistoryPage.xaml` - NEW
- ✅ `HistoryPage.xaml.cs` - NEW
- ⏳ `MainPage.xaml` - Add history button (manual step)
- ⏳ `MainPage.xaml.cs` - Add navigation handler (manual step)

---

## Success! 🎉

Your FraudGuard AI now has complete call history tracking with:
- ✅ Automatic logging when calls end
- ✅ SQLite persistence (portable, no Docker needed)
- ✅ REST API for history retrieval
- ✅ Beautiful mobile UI with color-coded fraud indicators
- ✅ Pull-to-refresh functionality

**Ready for demo!**
