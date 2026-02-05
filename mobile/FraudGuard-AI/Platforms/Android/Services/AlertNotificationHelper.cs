using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using System.Runtime.Versioning;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Helper để gửi notification cảnh báo khi phát hiện lừa đảo
    /// Notification này sẽ hiển thị ngay cả khi màn hình tắt
    /// </summary>
    [SupportedOSPlatform("android26.0")]
    public static class AlertNotificationHelper
    {
        private const string ALERT_CHANNEL_ID = "FraudAlerts";
        private const string ALERT_CHANNEL_NAME = "Fraud Alerts";
        private const int ALERT_NOTIFICATION_ID = 2001;

        /// <summary>
        /// Khởi tạo notification channel cho cảnh báo
        /// </summary>
        public static void CreateAlertChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ALERT_CHANNEL_ID,
                    ALERT_CHANNEL_NAME,
                    NotificationImportance.High // HIGH để hiển thị popup và phát âm thanh
                )
                {
                    Description = "Critical fraud alerts that require immediate attention",
                    LockscreenVisibility = NotificationVisibility.Public // Hiển thị trên lock screen
                };

                // Bật âm thanh và rung
                channel.EnableVibration(true);
                channel.SetVibrationPattern(new long[] { 0, 400, 200, 400 });
                
                // Sử dụng âm thanh cảnh báo mặc định
                var audioAttributes = new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Sonification)
                    .SetUsage(AudioUsageKind.Notification)
                    .Build();
                channel.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification), audioAttributes);

                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        /// <summary>
        /// Hiển thị notification cảnh báo lừa đảo
        /// </summary>
        public static void ShowFraudAlert(Context context, string alertType, double riskScore, string transcript)
        {
            CreateAlertChannel(context);

            // Intent để mở app khi tap vào notification
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            
            var pendingIntent = PendingIntent.GetActivity(
                context,
                0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            );

            // Tạo notification với priority cao
            var builder = new NotificationCompat.Builder(context, ALERT_CHANNEL_ID)
                .SetContentTitle("⚠️ FRAUD ALERT - NGUY HIỂM CAO!")
                .SetContentText($"{alertType} - Risk: {riskScore:F0}%")
                .SetStyle(new NotificationCompat.BigTextStyle()
                    .BigText($"Loại: {alertType}\n" +
                            $"Mức độ rủi ro: {riskScore:F0}%\n" +
                            $"Nội dung: {(string.IsNullOrEmpty(transcript) ? "Phát hiện dấu hiệu lừa đảo" : transcript)}\n\n" +
                            $"⚠️ Cân nhắc ngắt cuộc gọi ngay!"))
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
                .SetColor(global::Android.Graphics.Color.Red)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryAlarm)
                .SetVisibility(NotificationCompat.VisibilityPublic) // Hiển thị đầy đủ trên lock screen
                .SetAutoCancel(true) // Tự động xóa khi tap
                .SetContentIntent(pendingIntent)
                .SetVibrate(new long[] { 0, 400, 200, 400 }) // Pattern rung
                .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));

            // Nếu Android 8.0+, sử dụng full-screen intent để hiển thị popup ngay cả khi màn hình khóa
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var fullScreenIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    intent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
                );
                builder.SetFullScreenIntent(fullScreenIntent, true);
            }

            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.Notify(ALERT_NOTIFICATION_ID, builder.Build());

            System.Diagnostics.Debug.WriteLine($"[AlertNotification] Fraud alert sent: {alertType} - {riskScore:F0}%");
        }

        /// <summary>
        /// Xóa notification cảnh báo
        /// </summary>
        public static void ClearAlert(Context context)
        {
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.Cancel(ALERT_NOTIFICATION_ID);
        }
    }
}
