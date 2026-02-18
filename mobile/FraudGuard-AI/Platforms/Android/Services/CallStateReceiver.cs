using Android.App;
using Android.Content;
using Android.Telephony;
using System.Diagnostics;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// BroadcastReceiver để tự động phát hiện cuộc gọi đến
    /// Khi có cuộc gọi đến → tự động bật protection (recording + streaming)
    /// Khi cuộc gọi kết thúc → tự động tắt protection
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { TelephonyManager.ActionPhoneStateChanged })]
    public class CallStateReceiver : BroadcastReceiver
    {
        private static bool _isCallActive = false;
        private static bool _wasProtectionAutoStarted = false;

        /// <summary>
        /// Event để thông báo cho MainPage khi trạng thái cuộc gọi thay đổi
        /// </summary>
        public static event EventHandler<CallStateChangedEventArgs> CallStateChanged;

        /// <summary>
        /// Kiểm tra xem có cuộc gọi đang diễn ra không
        /// </summary>
        public static bool IsCallActive => _isCallActive;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action != TelephonyManager.ActionPhoneStateChanged)
                return;

            try
            {
                string stateStr = intent.GetStringExtra(TelephonyManager.ExtraState);
                string phoneNumber = intent.GetStringExtra(TelephonyManager.ExtraIncomingNumber) ?? "Unknown";

                Debug.WriteLine($"[CallReceiver] Phone state changed: {stateStr}, Number: {phoneNumber}");

                if (stateStr == TelephonyManager.ExtraStateRinging)
                {
                    // Cuộc gọi đến đang rung
                    Debug.WriteLine($"[CallReceiver] 📞 INCOMING CALL from: {phoneNumber}");
                    OnCallStateChanged(CallState.Ringing, phoneNumber);
                }
                else if (stateStr == TelephonyManager.ExtraStateOffhook)
                {
                    // Cuộc gọi đã được nhấc máy
                    Debug.WriteLine($"[CallReceiver] 📱 CALL ANSWERED: {phoneNumber}");
                    _isCallActive = true;
                    OnCallStateChanged(CallState.Active, phoneNumber);

                    // Tự động bật protection nếu chưa bật
                    AutoStartProtection();
                }
                else if (stateStr == TelephonyManager.ExtraStateIdle)
                {
                    // Cuộc gọi đã kết thúc
                    Debug.WriteLine($"[CallReceiver] 📴 CALL ENDED");

                    if (_isCallActive)
                    {
                        _isCallActive = false;
                        OnCallStateChanged(CallState.Ended, phoneNumber);

                        // Tự động tắt nếu đã tự động bật
                        if (_wasProtectionAutoStarted)
                        {
                            AutoStopProtection();
                            _wasProtectionAutoStarted = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động bật protection khi nhấc máy
        /// </summary>
        private void AutoStartProtection()
        {
            try
            {
                var audioService = App.GetAudioService();
                if (audioService != null && !audioService.IsStreaming)
                {
                    Debug.WriteLine("[CallReceiver] 🛡️ Auto-starting protection for incoming call...");
                    _wasProtectionAutoStarted = true;

                    // Chạy trên main thread vì liên quan đến UI service
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            bool connected = await audioService.StartStreamingAsync();
                            if (connected)
                            {
                                Debug.WriteLine("[CallReceiver] ✅ Protection auto-started successfully");
                                ServiceHelper.StartProtectionService();
                            }
                            else
                            {
                                Debug.WriteLine("[CallReceiver] ❌ Failed to auto-start protection");
                                _wasProtectionAutoStarted = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CallReceiver] Auto-start error: {ex.Message}");
                            _wasProtectionAutoStarted = false;
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("[CallReceiver] Protection already active, skipping auto-start");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] AutoStartProtection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động tắt protection khi kết thúc cuộc gọi
        /// </summary>
        private void AutoStopProtection()
        {
            try
            {
                var audioService = App.GetAudioService();
                if (audioService != null && audioService.IsStreaming)
                {
                    Debug.WriteLine("[CallReceiver] 🛑 Auto-stopping protection (call ended)...");

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await audioService.StopStreamingAsync();
                            ServiceHelper.StopProtectionService();
                            Debug.WriteLine("[CallReceiver] ✅ Protection auto-stopped successfully");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CallReceiver] Auto-stop error: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] AutoStopProtection error: {ex.Message}");
            }
        }

        private static void OnCallStateChanged(CallState state, string phoneNumber)
        {
            CallStateChanged?.Invoke(null, new CallStateChangedEventArgs(state, phoneNumber));
        }
    }

    /// <summary>
    /// Trạng thái cuộc gọi
    /// </summary>
    public enum CallState
    {
        Ringing,
        Active,
        Ended
    }

    /// <summary>
    /// EventArgs cho sự kiện thay đổi trạng thái cuộc gọi
    /// </summary>
    public class CallStateChangedEventArgs : EventArgs
    {
        public CallState State { get; }
        public string PhoneNumber { get; }

        public CallStateChangedEventArgs(CallState state, string phoneNumber)
        {
            State = state;
            PhoneNumber = phoneNumber;
        }
    }
}
