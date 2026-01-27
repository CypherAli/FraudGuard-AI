using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace FraudGuardAI.Helpers
{
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
        /// Show dialog when permission is denied
        /// </summary>
        private static async Task<bool> ShowPermissionDeniedDialog()
        {
            if (Application.Current?.MainPage == null)
                return false;

            return await Application.Current.MainPage.DisplayAlert(
                "⚠️ Permission Required",
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
                "🔴 Cannot Connect:\n" +
                "  • Check WiFi connection\n" +
                "  • Verify server IP in Settings\n" +
                "  • Ensure server is running\n\n" +
                "🔴 No Audio:\n" +
                "  • Check microphone permission\n" +
                "  • Test with another app\n" +
                "  • Restart FraudGuard\n\n" +
                "🔴 False Alerts:\n" +
                "  • Adjust sensitivity in Settings\n" +
                "  • Report false positives\n\n" +
                "Need more help? Contact support.",
                "Got It"
            );
        }
    }
}
