using Android.App;
using Android.Content;
using Android.Telecom;
using Android.Telephony;
using FraudGuardAI.Services;
using System.Diagnostics;
using System.Threading;

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
        private static volatile bool _isCallActive = false;
        private static volatile bool _wasProtectionAutoStarted = false;

        // Debounce IDLE events: speakerphone toggle / audio re-route cũng fire IDLE,
        // cần chờ 1.5s để xác nhận cuộc gọi thực sự kết thúc (không phải chỉ đổi audio route)
        private static CancellationTokenSource? _idleDebounceToken;
        private static readonly object _idleLock = new object();

        /// <summary>
        /// Event để thông báo cho MainPage khi trạng thái cuộc gọi thay đổi
        /// </summary>
        public static event EventHandler<CallStateChangedEventArgs>? CallStateChanged;

        /// <summary>
        /// Kiểm tra xem có cuộc gọi đang diễn ra không
        /// </summary>
        public static bool IsCallActive => _isCallActive;

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != TelephonyManager.ActionPhoneStateChanged)
                return;

            try
            {
                string? stateStr = intent.GetStringExtra(TelephonyManager.ExtraState);
                string phoneNumber = intent.GetStringExtra(TelephonyManager.ExtraIncomingNumber) ?? "Unknown";

                Debug.WriteLine($"[CallReceiver] Phone state changed: {stateStr}, Number: {phoneNumber}");

                if (stateStr == TelephonyManager.ExtraStateRinging)
                {
                    // Cuộc gọi đến đang rung
                    Debug.WriteLine($"[CallReceiver] 📞 INCOMING CALL from: {phoneNumber}");

                    // Auto-reject if number is blacklisted
                    bool autoReject = Preferences.Get("AutoRejectBlacklisted", false);
                    if (autoReject && phoneNumber != "Unknown")
                    {
                        try
                        {
                            if (BlacklistCacheService.Instance.IsBlacklisted(phoneNumber))
                            {
                                Debug.WriteLine($"[CallReceiver] 🚫 BLACKLISTED — auto-rejecting: {phoneNumber}");
                                if (context != null)
                                {
                                    RejectCall(context);
                                    ShowRejectionNotification(context, phoneNumber);
                                }
                                return; // Don't process further
                            }
                        }
                        catch (Exception bex)
                        {
                            Debug.WriteLine($"[CallReceiver] Blacklist check error: {bex.Message}");
                        }
                    }

                    OnCallStateChanged(CallState.Ringing, phoneNumber);
                }
                else if (stateStr == TelephonyManager.ExtraStateOffhook)
                {
                    // Cuộc gọi đã được nhấc máy (hoặc đang gọi đi)
                    Debug.WriteLine($"[CallReceiver] 📱 CALL ANSWERED/OFFHOOK: {phoneNumber}");

                    // Hủy debounce IDLE nếu đang chờ — cuộc gọi vẫn còn active
                    lock (_idleLock)
                    {
                        _idleDebounceToken?.Cancel();
                        _idleDebounceToken = null;
                    }

                    _isCallActive = true;
                    OnCallStateChanged(CallState.Active, phoneNumber);

                    // Tự động bật protection nếu chưa bật
                    AutoStartProtection();
                }
                else if (stateStr == TelephonyManager.ExtraStateIdle)
                {
                    // IDLE có thể do: (1) cuộc gọi thực sự kết thúc, hoặc (2) speakerphone toggle / audio re-route
                    // Dùng debounce 1.5s: nếu sau 1.5s không có OFFHOOK mới → cuộc gọi thực sự ended
                    if (_isCallActive)
                    {
                        Debug.WriteLine($"[CallReceiver] 📴 IDLE received — debouncing 1.5s to confirm call ended...");

                        CancellationTokenSource thisCts;
                        lock (_idleLock)
                        {
                            _idleDebounceToken?.Cancel();
                            thisCts = new CancellationTokenSource();
                            _idleDebounceToken = thisCts;
                        }

                        var capturedPhone = phoneNumber;
                        _ = Task.Delay(1500, thisCts.Token).ContinueWith(t =>
                        {
                            if (t.IsCanceled) return; // OFFHOOK came in → bỏ qua

                            Debug.WriteLine($"[CallReceiver] 📴 CALL CONFIRMED ENDED after debounce");
                            _isCallActive = false;
                            OnCallStateChanged(CallState.Ended, capturedPhone);

                            // Tự động tắt protection nếu đã tự động bật
                            if (_wasProtectionAutoStarted)
                            {
                                AutoStopProtection();
                                _wasProtectionAutoStarted = false;
                            }
                        }, TaskScheduler.Default);
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

        /// <summary>
        /// Reject an incoming call using TelecomManager (Android 9+)
        /// </summary>
        private void RejectCall(Context context)
        {
            try
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.P)
                {
                    var telecomManager = context.GetSystemService(Context.TelecomService) as TelecomManager;
                    if (telecomManager != null)
                    {
                        bool ended = telecomManager.EndCall();
                        Debug.WriteLine($"[CallReceiver] EndCall result: {ended}");
                    }
                }
                else
                {
                    Debug.WriteLine("[CallReceiver] EndCall requires Android 9+ (API 28)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] RejectCall error: {ex.Message}");
            }
        }

        /// <summary>
        /// Show notification that a blacklisted call was rejected
        /// </summary>
        private void ShowRejectionNotification(Context context, string phoneNumber)
        {
            try
            {
                AlertNotificationHelper.ShowFraudAlert(
                    context,
                    "BLOCKED",
                    100.0,
                    $"Auto-rejected blacklisted number: {phoneNumber}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] Notification error: {ex.Message}");
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
