using Microsoft.Maui.Storage;
using FraudGuardAI.Constants;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Reads app configuration from Preferences.
    /// Single source of truth — replaces all SettingsPage.GetXxx() static calls.
    /// </summary>
    public sealed class AppSettings : IAppSettings
    {
        private const string PREF_SERVER_URL   = "ServerURL";
        private const string PREF_USB_MODE     = "UsbMode";
        private const string PREF_DEVICE_ID    = "DeviceID";
        private const string PREF_AUTO_PROTECT = "AutoProtection";
        private const string DEFAULT_DEVICE_ID = "android_device";

        public string GetApiBaseUrl()
        {
            if (Preferences.Get(PREF_USB_MODE, false))
                return AppConstants.USB_SERVER_URL;

            return Preferences.Get(PREF_SERVER_URL, AppConstants.PRODUCTION_SERVER_URL);
        }

        public string GetWebSocketUrl()
        {
            var baseUrl = GetApiBaseUrl();

            if (baseUrl.StartsWith("https://"))
                return baseUrl.Replace("https://", "wss://") + "/ws";

            if (baseUrl.StartsWith("http://"))
                return baseUrl.Replace("http://", "ws://") + "/ws";

            return $"ws://{baseUrl}:8080/ws";
        }

        public string GetDeviceId()
            => Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);

        public bool IsAutoProtectionEnabled()
            => Preferences.Get(PREF_AUTO_PROTECT, true);
    }
}
