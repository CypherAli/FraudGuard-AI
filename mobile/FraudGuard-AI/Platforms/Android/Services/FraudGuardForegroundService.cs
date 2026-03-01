using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Foreground Service để đảm bảo app chạy ngầm và phát cảnh báo ngay cả khi màn hình tắt.
    /// TypeMicrophone: dùng cho AudioRecord (MIC + PSTN-SCO đều capture qua AudioRecord).
    /// NOTE: TypeMediaProjection KHÔNG được dùng ở đây vì yêu cầu active MediaProjection token
    ///       (user consent qua MediaProjectionManager) — app dùng Bluetooth SCO, không dùng MediaProjection.
    /// </summary>
    [Service(ForegroundServiceType =
        global::Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class FraudGuardForegroundService : Service
    {
        private const int SERVICE_NOTIFICATION_ID = 1001;
        private const string CHANNEL_ID = "FraudGuardProtection";
        private const string CHANNEL_NAME = "Fraud Protection Active";

        private PowerManager.WakeLock? _wakeLock;

        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannel();
            AcquireWakeLock();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            // Dùng localized strings nếu LocalizationResourceManager đã được khởi tạo.
            // Fallback sang English nếu chưa sẵn sàng (e.g. service restart sau khi app bị kill).
            string notifTitle   = "🛡️ Protection Active";
            string notifContent = "Monitoring calls for fraud detection";
            try
            {
                var rm = FraudGuardAI.Localization.LocalizationResourceManager.Instance;
                notifTitle   = rm["Service_NotificationTitle"]   ?? notifTitle;
                notifContent = rm["Service_NotificationContent"] ?? notifContent;
            }
            catch { /* LocalizationResourceManager chưa init — dùng fallback */ }

            var notification = CreateNotification(notifTitle, notifContent);

            // Android 10+ (API 29+): StartForeground PHẢI chỉ định foreground service type.
            // TypeMicrophone đủ cho mọi trường hợp: MIC capture + PSTN-SCO đều dùng AudioRecord.
            // TypeMediaProjection KHÔNG được dùng — yêu cầu active token từ MediaProjectionManager,
            // không phù hợp với Bluetooth SCO routing (đây không phải screen capture).
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) // API 29 = Android 10
            {
                StartForeground(SERVICE_NOTIFICATION_ID, notification,
                    global::Android.Content.PM.ForegroundService.TypeMicrophone);
            }
            else
            {
                StartForeground(SERVICE_NOTIFICATION_ID, notification);
            }

            System.Diagnostics.Debug.WriteLine("[ForegroundService] Service started - App will continue running in background");

            // Trả về START_STICKY để service tự động restart nếu bị kill
            return StartCommandResult.Sticky;
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null; // Không cần bind vì đây là started service
        }

        public override void OnDestroy()
        {
            ReleaseWakeLock();
            StopForeground(true);
            base.OnDestroy();
            System.Diagnostics.Debug.WriteLine("[ForegroundService] Service stopped");
        }

        /// <summary>
        /// Tạo notification channel cho Android 8.0+
        /// </summary>
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    CHANNEL_NAME,
                    NotificationImportance.Low // Low để không làm phiền user
                )
                {
                    Description = "Shows when FraudGuard is actively protecting you"
                };

                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        /// <summary>
        /// Tạo notification cho foreground service
        /// </summary>
        private Notification CreateNotification(string title, string content)
        {
            // Intent để mở app khi tap vào notification
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                intent,
                PendingIntentFlags.Immutable
            );

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText(content)
                .SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleAlarm) // Sử dụng icon hệ thống
                .SetOngoing(true) // Không thể swipe away
                .SetContentIntent(pendingIntent)
                .SetPriority(NotificationCompat.PriorityLow);

            return builder.Build();
        }

        /// <summary>
        /// Cập nhật notification (ví dụ khi phát hiện mối đe dọa)
        /// </summary>
        public void UpdateNotification(string title, string content)
        {
            var notification = CreateNotification(title, content);
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.Notify(SERVICE_NOTIFICATION_ID, notification);
        }

        /// <summary>
        /// Acquire wake lock để giữ CPU hoạt động khi màn hình tắt
        /// </summary>
        private void AcquireWakeLock()
        {
            try
            {
                var powerManager = GetSystemService(PowerService) as PowerManager;
                if (powerManager != null)
                {
                    _wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.Partial, // Chỉ giữ CPU, không giữ màn hình
                        "FraudGuard::AudioProcessing"
                    );
                    // Acquire indefinite WakeLock — the ForegroundService lifecycle (OnDestroy →
                    // ReleaseWakeLock) ensures it is always released when protection stops.
                    // A fixed timeout (e.g. 4 h) would silently drop the WakeLock mid-call,
                    // causing the CPU to sleep and halting audio recording.
                    _wakeLock?.Acquire();
                    System.Diagnostics.Debug.WriteLine("[ForegroundService] Wake lock acquired (indefinite, released on service stop)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForegroundService] Error acquiring wake lock: {ex.Message}");
            }
        }

        /// <summary>
        /// Release wake lock khi service dừng
        /// </summary>
        private void ReleaseWakeLock()
        {
            try
            {
                if (_wakeLock != null && _wakeLock.IsHeld)
                {
                    _wakeLock.Release();
                    System.Diagnostics.Debug.WriteLine("[ForegroundService] Wake lock released");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForegroundService] Error releasing wake lock: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method để start service từ code
        /// </summary>
        public static void Start(Context context)
        {
            var intent = new Intent(context, typeof(FraudGuardForegroundService));
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        /// <summary>
        /// Helper method để stop service từ code
        /// </summary>
        public static void Stop(Context context)
        {
            var intent = new Intent(context, typeof(FraudGuardForegroundService));
            context.StopService(intent);
        }
    }
}
