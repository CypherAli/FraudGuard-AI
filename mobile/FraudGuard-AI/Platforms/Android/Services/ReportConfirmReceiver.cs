using Android.App;
using Android.Content;
using FraudGuardAI.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// BroadcastReceiver xử lý action buttons từ "Pending Report" notification.
    /// [Xác nhận chặn] → POST /api/report → thêm vào blacklist cộng đồng.
    /// [Bỏ qua]        → dismiss notification, không làm gì.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] {
        AlertNotificationHelper.ACTION_CONFIRM_REPORT,
        AlertNotificationHelper.ACTION_IGNORE_REPORT
    })]
    public class ReportConfirmReceiver : BroadcastReceiver
    {
        public override async void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            var action = intent.Action;
            var phone  = intent.GetStringExtra(AlertNotificationHelper.EXTRA_PHONE)  ?? "";
            var reason = intent.GetStringExtra(AlertNotificationHelper.EXTRA_REASON) ?? "";

            // Xóa notification ngay lập tức
            AlertNotificationHelper.ClearPendingReport(context);

            if (action == AlertNotificationHelper.ACTION_IGNORE_REPORT)
            {
                System.Diagnostics.Debug.WriteLine($"[ReportConfirm] User ignored pending report for {phone}");
                return;
            }

            if (action != AlertNotificationHelper.ACTION_CONFIRM_REPORT || string.IsNullOrEmpty(phone))
                return;

            // User xác nhận → POST /api/report
            await SubmitConfirmedReportAsync(context, phone, reason);
        }

        private static async Task SubmitConfirmedReportAsync(Context context, string phone, string reason)
        {
            try
            {
                var settings = IPlatformApplication.Current?.Services.GetService<IAppSettings>();
                var baseUrl  = settings?.GetApiBaseUrl() ?? "https://fraudguard-ai-jljl.onrender.com";

                var payload = new
                {
                    phone_number = phone,
                    reason       = $"[User Confirmed] {reason}",
                    device_id    = settings?.GetDeviceId() ?? "android_device"
                };

                using var http    = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8, "application/json");

                var response = await http.PostAsync($"{baseUrl}/api/report", content);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReportConfirm] ✅ Reported {phone} to blacklist");

                    // Cập nhật local blacklist cache — guarded: MAUI DI may not be fully
                    // initialized when BroadcastReceiver fires on cold-start notification tap.
                    try { BlacklistCacheService.Instance.AddToLocalCache(phone); }
                    catch (Exception cacheEx)
                    { System.Diagnostics.Debug.WriteLine($"[ReportConfirm] Cache update skipped: {cacheEx.Message}"); }

                    try { _ = BlacklistCacheService.Instance.SyncFromServerAsync(); }
                    catch { /* non-critical — will sync on next app launch */ }

                    // Hiển thị toast thông báo thành công
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Toast không có sẵn trong MAUI — dùng thông báo tạm qua
                        // Application.Current nếu app đang foreground
                        if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                        {
                            _ = Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(
                                "✅ Đã chặn",
                                $"Số {phone} đã được báo cáo vào blacklist cộng đồng.\nCảm ơn bạn đã giúp bảo vệ cộng đồng!",
                                "OK");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ReportConfirm] ❌ Report failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReportConfirm] Exception: {ex.Message}");
            }
        }
    }
}
