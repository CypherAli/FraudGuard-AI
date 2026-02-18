using Microsoft.Maui.Graphics;

namespace FraudGuardAI.Constants
{
    /// <summary>
    /// Application-wide constants - centralized configuration
    /// </summary>
    public static class AppConstants
    {
        #region Risk Thresholds
        // These thresholds MUST match the backend fraud_detection_config.go values:
        //   LOW=20, MEDIUM=40, HIGH=60, CRITICAL=80
        // Backend sends alert_type: "LOW" | "MEDIUM" | "HIGH" | "CRITICAL"
        // and confidence as 0.0-1.0 (RiskScore/100)

        /// <summary>Critical alert - full-screen popup + Android notification + vibration</summary>
        public const double CRITICAL_RISK_THRESHOLD = 0.80; // 80/100 backend score

        /// <summary>High alert - full-screen popup + Android notification + vibration</summary>
        public const double HIGH_RISK_THRESHOLD = 0.60;     // 60/100 backend score

        /// <summary>Medium alert - banner only (no popup), auto-dismiss after 5s</summary>
        public const double MEDIUM_RISK_THRESHOLD = 0.40;   // 40/100 backend score

        /// <summary>Low alert - banner only, minimal prominence, auto-dismiss after 5s</summary>
        public const double LOW_RISK_THRESHOLD = 0.20;      // 20/100 backend score

        #endregion

        #region Animation Durations (milliseconds)

        public const uint PULSE_DURATION = 2000;
        public const uint DANGER_FLASH_DURATION = 400;
        public const uint VIBRATION_DURATION = 400;
        public const uint VIBRATION_PAUSE = 600;
        public const uint SCALE_IN_DURATION = 150;
        public const uint SCALE_OUT_DURATION = 200;
        public const uint FADE_DURATION = 200;

        #endregion

        #region Audio Configuration

        public const int SAMPLE_RATE = 16000;  // 16kHz for Deepgram
        public const int CHANNELS = 1;          // Mono
        public const int BUFFER_SIZE = 4096;

        #endregion

        #region Colors (matching App.xaml)

        public static readonly Color SafeColor = Color.FromArgb("#34D399");
        public static readonly Color DangerColor = Color.FromArgb("#F87171");
        public static readonly Color InactiveColor = Color.FromArgb("#5C6B7A");
        public static readonly Color BackgroundDark = Color.FromArgb("#0D1B2A");
        public static readonly Color DangerBackground = Color.FromArgb("#7F1D1D");
        public static readonly Color WarningBackground = Color.FromArgb("#78350F");
        public static readonly Color GlowGreen = Color.FromArgb("#34D39960");
        public static readonly Color GlowRed = Color.FromArgb("#F8717160");
        public static readonly Color TextSecondary = Color.FromArgb("#8B9CAF");

        #endregion

        #region Server Configuration

        /// <summary>
        /// Production server URL (Render.com)
        /// </summary>
        public const string PRODUCTION_SERVER_URL = "https://fraudguard-ai-jljl.onrender.com";

        /// <summary>
        /// Default local server URL (for development)
        /// </summary>
        public const string LOCAL_SERVER_URL = "http://192.168.1.234:8080";

        /// <summary>
        /// USB/Emulator server URL
        /// </summary>
        public const string USB_SERVER_URL = "http://10.0.2.2:8080";

        #endregion

        #region Display Settings

        public const int ALERT_AUTO_DISMISS_DELAY = 5000; // 5 seconds
        public const int DANGER_FLASH_COUNT = 3;
        public const string TIMESTAMP_FORMAT = "HH:mm:ss";

        #endregion
    }
}
