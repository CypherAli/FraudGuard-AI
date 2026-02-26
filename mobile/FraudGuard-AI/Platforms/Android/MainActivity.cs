using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media.Projection;
using Android.OS;

namespace FraudGuardAI
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges =
            ConfigChanges.ScreenSize | ConfigChanges.Orientation |
            ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // ── MediaProjection (VoIP PlaybackCapture) ───────────────────────────
        private const int REQUEST_MEDIA_PROJECTION = 1001;

        private static MediaProjectionManager?                   _projectionManager;
        private static TaskCompletionSource<MediaProjection?>?   _projectionTcs;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _projectionManager = (MediaProjectionManager?)
                GetSystemService(MediaProjectionService);

            System.Diagnostics.Debug.WriteLine("[MainActivity] OnCreate — MediaProjectionManager ready");
        }

        // ── MediaProjection API ───────────────────────────────────────────────

        /// <summary>
        /// Hiển thị dialog hệ thống "FraudGuard muốn bắt đầu ghi màn hình".
        /// UX tip: Đổi tên thành "Call-Shield Mode" trong onboarding để user
        /// hiểu đây là tính năng bảo mật, không phải quay màn hình.
        ///
        /// Trả về MediaProjection nếu user đồng ý, null nếu từ chối.
        /// </summary>
        public static async Task<Android.Media.Projection.MediaProjection?> RequestMediaProjectionAsync()
        {
            if (_projectionManager == null)
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] MediaProjectionManager is null");
                return null;
            }

            // Nếu đang có request pending, hủy để tránh leak
            _projectionTcs?.TrySetResult(null);
            _projectionTcs = new TaskCompletionSource<Android.Media.Projection.MediaProjection?>();

            var intent = _projectionManager.CreateScreenCaptureIntent();
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.StartActivityForResult(
                intent, REQUEST_MEDIA_PROJECTION);

            // Đợi callback từ OnActivityResult
            var result = await _projectionTcs.Task
                .WaitAsync(TimeSpan.FromSeconds(60)); // timeout nếu user không phản hồi

            return result;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode == REQUEST_MEDIA_PROJECTION)
            {
                if (resultCode == Result.Ok && data != null && _projectionManager != null)
                {
                    // CRITICAL: Wrapped in try-catch.
                    // Android 14 (API 34) throws SecurityException/IllegalStateException if:
                    //   - No foreground service with TypeMediaProjection is running.
                    //   - Service was not started BEFORE createScreenCaptureIntent().
                    // Without this catch the unhandled exception kills the process immediately.
                    try
                    {
                        var projection = _projectionManager.GetMediaProjection((int)resultCode, data);
                        System.Diagnostics.Debug.WriteLine("[MainActivity] ✅ MediaProjection granted");
                        _projectionTcs?.TrySetResult(projection);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainActivity] ❌ GetMediaProjection threw: {ex.GetType().Name}: {ex.Message}");
                        // Resolve the pending task so TryActivateCallShieldAsync() can handle it gracefully.
                        _projectionTcs?.TrySetResult(null);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] ❌ MediaProjection denied by user");
                    _projectionTcs?.TrySetResult(null);
                }
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }
    }
}
