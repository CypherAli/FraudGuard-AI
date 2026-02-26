using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace FraudGuardAI.Helpers
{
#if ANDROID
    /// <summary>
    /// Custom MAUI permission wrapper for android.permission.ANSWER_PHONE_CALLS.
    /// Required for TelecomManager.EndCall() (auto-reject blacklisted calls, Android 9+).
    /// </summary>
    public class AnswerPhoneCallPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[] { (Android.Manifest.Permission.AnswerPhoneCalls, true) };
    }
#endif

    /// <summary>
    /// Manages app permissions with user-friendly dialogs
    /// </summary>
    public static class PermissionManager
    {
        /// <summary>
        /// Request microphone permission with rationale
        /// </summary>
        public static async Task<bool> RequestMicrophonePermissionAsync()
        {
            // Check current status
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            if (status == PermissionStatus.Granted)
                return true;

            // If permission was denied before, show rationale
            if (status == PermissionStatus.Denied)
            {
                bool openSettings = await ShowPermissionDeniedDialog();
                if (openSettings)
                {
                    AppInfo.ShowSettingsUI();
                }
                return false;
            }

            // Show rationale before requesting
            bool proceed = await ShowPermissionRationale();
            if (!proceed)
                return false;

            // Request permission
            status = await Permissions.RequestAsync<Permissions.Microphone>();

            if (status != PermissionStatus.Granted)
            {
                await ShowPermissionDeniedDialog();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Explain why we need microphone permission
        /// </summary>
        private static async Task<bool> ShowPermissionRationale()
        {
            if (Application.Current?.MainPage == null)
                return false;

            return await Application.Current.MainPage.DisplayAlert(
                "🎤 Microphone Access",
                "FraudGuard needs microphone access to:\n\n" +
                "• Listen to incoming calls in real-time\n" +
                "• Analyze conversations for fraud patterns\n" +
                "• Alert you immediately when threats detected\n\n" +
                "Your audio is processed privately and never shared.",
                "Allow Access",
                "Not Now"
            );
        }
        
        /// <summary>
        /// Request all required permissions (Microphone + Notification)
        /// </summary>
        public static async Task<bool> RequestAllPermissionsAsync()
        {
            // Request microphone permission
            bool hasMicrophone = await RequestMicrophonePermissionAsync();
            if (!hasMicrophone)
                return false;
            
            // Request phone state permission for auto-detecting incoming calls
            #if ANDROID
            try
            {
                var phoneStatus = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (phoneStatus != PermissionStatus.Granted)
                {
                    phoneStatus = await Permissions.RequestAsync<Permissions.Phone>();
                    System.Diagnostics.Debug.WriteLine($"[PermissionManager] Phone permission: {phoneStatus}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PermissionManager] Phone permission error: {ex.Message}");
            }

            // Notification permission is optional but recommended
            // On Android 13+, we should request it
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }
            catch
            {
                // Notification permission might not be available on older Android versions
            }

            // Request SMS permission for SMS fraud detection
            try
            {
                var smsStatus = await Permissions.CheckStatusAsync<Permissions.Sms>();
                if (smsStatus != PermissionStatus.Granted)
                {
                    smsStatus = await Permissions.RequestAsync<Permissions.Sms>();
                    System.Diagnostics.Debug.WriteLine($"[PermissionManager] SMS permission: {smsStatus}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PermissionManager] SMS permission error: {ex.Message}");
            }

            // ANSWER_PHONE_CALLS — required for TelecomManager.EndCall() (auto-reject blacklist, Android 9+)
            try
            {
                var answerStatus = await Permissions.CheckStatusAsync<AnswerPhoneCallPermission>();
                if (answerStatus != PermissionStatus.Granted)
                {
                    answerStatus = await Permissions.RequestAsync<AnswerPhoneCallPermission>();
                    System.Diagnostics.Debug.WriteLine($"[PermissionManager] AnswerPhoneCall permission: {answerStatus}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PermissionManager] AnswerPhoneCall permission error: {ex.Message}");
            }
            #endif

            return true;
        }

        /// <summary>
        /// Show dialog when permission is denied
        /// </summary>
        private static async Task<bool> ShowPermissionDeniedDialog()
        {
            if (Application.Current?.MainPage == null)
                return false;

            return await Application.Current.MainPage.DisplayAlert(
                " Permission Required",
                "FraudGuard cannot protect you without microphone access.\n\n" +
                "To enable:\n" +
                "1. Tap 'Open Settings' below\n" +
                "2. Find 'Permissions'\n" +
                "3. Enable 'Microphone'\n" +
                "4. Return to FraudGuard",
                "Open Settings",
                "Cancel"
            );
        }

        /// <summary>
        /// Check if all required permissions are granted
        /// </summary>
        public static async Task<bool> CheckAllPermissions()
        {
            var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            return micStatus == PermissionStatus.Granted;
        }

        /// <summary>
        /// Show troubleshooting dialog
        /// </summary>
        public static async Task ShowTroubleshootingGuide()
        {
            if (Application.Current?.MainPage == null)
                return;

            await Application.Current.MainPage.DisplayAlert(
                "Troubleshooting",
                "Common Issues:\n\n" +
                " Cannot Connect:\n" +
                "  • Check WiFi connection\n" +
                "  • Verify server IP in Settings\n" +
                "  • Ensure server is running\n\n" +
                " No Audio:\n" +
                "  • Check microphone permission\n" +
                "  • Test with another app\n" +
                "  • Restart FraudGuard\n\n" +
                " False Alerts:\n" +
                "  • Adjust sensitivity in Settings\n" +
                "  • Report false positives\n\n" +
                "Need more help? Contact support.",
                "Got It"
            );
        }
    }
}
