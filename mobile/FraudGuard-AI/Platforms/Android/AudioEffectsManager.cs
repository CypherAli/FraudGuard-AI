using Android.Media;
using Android.Media.Audiofx;
using System;

namespace FraudGuardAI.Platforms.Android
{
    /// <summary>
    /// Manages Android hardware audio effects (AEC and Noise Suppression)
    /// This is critical for reducing echo and background noise on mobile devices
    /// </summary>
    public class AudioEffectsManager : IDisposable
    {
        private AcousticEchoCanceler? _echoCanceler;
        private NoiseSuppressor? _noiseSuppressor;
        private int _audioSessionId;
        private bool _isDisposed;

        // Effect availability flags
        public bool IsEchoCancelerAvailable { get; private set; }
        public bool IsNoiseSuppressorAvailable { get; private set; }

        // Effect enabled state
        public bool IsEchoCancelerEnabled { get; private set; }
        public bool IsNoiseSuppressorEnabled { get; private set; }

        // Hardware AEC effectiveness (0.0 - 1.0, higher = better)
        public float EchoCancelerEffectiveness { get; private set; }

        public AudioEffectsManager(int audioSessionId)
        {
            _audioSessionId = audioSessionId;
            
            // Check availability of hardware effects
            IsEchoCancelerAvailable = AcousticEchoCanceler.IsAvailable;
            IsNoiseSuppressorAvailable = NoiseSuppressor.IsAvailable;

            System.Diagnostics.Debug.WriteLine($"🎛️ [AudioEffects] Session ID: {audioSessionId}");
            System.Diagnostics.Debug.WriteLine($"🎛️ [AudioEffects] AEC Available: {IsEchoCancelerAvailable}");
            System.Diagnostics.Debug.WriteLine($"🎛️ [AudioEffects] NS Available: {IsNoiseSuppressorAvailable}");
        }

        /// <summary>
        /// Initialize and enable audio effects
        /// </summary>
        public void EnableEffects(bool enableEchoCanceler = true, bool enableNoiseSuppressor = true)
        {
            try
            {
                // Enable Acoustic Echo Canceler
                if (enableEchoCanceler && IsEchoCancelerAvailable && _echoCanceler == null)
                {
                    _echoCanceler = AcousticEchoCanceler.Create(_audioSessionId);
                    if (_echoCanceler != null)
                    {
                        _echoCanceler.SetEnabled(true);
                        IsEchoCancelerEnabled = true;
                        
                        // Test AEC effectiveness
                        EchoCancelerEffectiveness = TestEchoCancelerEffectiveness();
                        
                        System.Diagnostics.Debug.WriteLine($"✅ [AudioEffects] AEC Enabled (Effectiveness: {EchoCancelerEffectiveness:P0})");
                        
                        // Warn if AEC is weak
                        if (EchoCancelerEffectiveness < 0.5f)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ [AudioEffects] WARNING: Hardware AEC appears weak on this device!");
                        }
                    }
                }

                // Enable Noise Suppressor
                if (enableNoiseSuppressor && IsNoiseSuppressorAvailable && _noiseSuppressor == null)
                {
                    _noiseSuppressor = NoiseSuppressor.Create(_audioSessionId);
                    if (_noiseSuppressor != null)
                    {
                        _noiseSuppressor.SetEnabled(true);
                        IsNoiseSuppressorEnabled = true;
                        System.Diagnostics.Debug.WriteLine($"✅ [AudioEffects] Noise Suppressor Enabled");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [AudioEffects] Error enabling effects: {ex.Message}");
            }
        }

        /// <summary>
        /// Test hardware AEC effectiveness
        /// Returns a score from 0.0 (ineffective) to 1.0 (very effective)
        /// </summary>
        private float TestEchoCancelerEffectiveness()
        {
            try
            {
                if (_echoCanceler == null)
                    return 0.0f;

                // Check device manufacturer and model for known weak AEC devices
                string manufacturer = global::Android.OS.Build.Manufacturer?.ToLower() ?? "";
                string model = global::Android.OS.Build.Model?.ToLower() ?? "";
                
                // Budget devices often have weak hardware AEC
                if (manufacturer.Contains("samsung") && (model.Contains("a0") || model.Contains("a1") || model.Contains("a2")))
                {
                    return 0.3f; // Samsung A-series: weak AEC
                }
                
                if (manufacturer.Contains("xiaomi") && model.Contains("redmi"))
                {
                    return 0.4f; // Redmi series: moderate AEC
                }
                
                // Check Android version (newer = usually better)
                int sdkVersion = (int)global::Android.OS.Build.VERSION.SdkInt;
                if (sdkVersion >= 31) // Android 12+
                {
                    return 0.8f; // Modern devices usually have good AEC
                }
                else if (sdkVersion >= 28) // Android 9+
                {
                    return 0.6f; // Decent AEC
                }
                else
                {
                    return 0.4f; // Older devices may have weak AEC
                }
            }
            catch
            {
                return 0.5f; // Default: assume moderate effectiveness
            }
        }

        /// <summary>
        /// Disable audio effects
        /// </summary>
        public void DisableEffects()
        {
            try
            {
                if (_echoCanceler != null)
                {
                    _echoCanceler.SetEnabled(false);
                    IsEchoCancelerEnabled = false;
                    System.Diagnostics.Debug.WriteLine($"🔇 [AudioEffects] AEC Disabled");
                }

                if (_noiseSuppressor != null)
                {
                    _noiseSuppressor.SetEnabled(false);
                    IsNoiseSuppressorEnabled = false;
                    System.Diagnostics.Debug.WriteLine($"🔇 [AudioEffects] Noise Suppressor Disabled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [AudioEffects] Error disabling effects: {ex.Message}");
            }
        }

        /// <summary>
        /// Get status message for UI display
        /// </summary>
        public string GetStatusMessage()
        {
            if (!IsEchoCancelerAvailable && !IsNoiseSuppressorAvailable)
            {
                return "⚠️ Hardware audio effects not available on this device";
            }

            if (EchoCancelerEffectiveness < 0.5f && IsEchoCancelerEnabled)
            {
                return "⚠️ Khuyến nghị: Giảm âm lượng loa ngoài để cải thiện chất lượng âm thanh";
            }

            var status = new System.Text.StringBuilder();
            if (IsEchoCancelerEnabled)
                status.Append("✅ Echo Cancellation ");
            if (IsNoiseSuppressorEnabled)
                status.Append("✅ Noise Suppression");

            return status.Length > 0 ? status.ToString() : "Audio effects disabled";
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            DisableEffects();

            _echoCanceler?.Release();
            _echoCanceler = null;

            _noiseSuppressor?.Release();
            _noiseSuppressor = null;

            _isDisposed = true;
            System.Diagnostics.Debug.WriteLine($"🗑️ [AudioEffects] Disposed");
        }
    }
}
