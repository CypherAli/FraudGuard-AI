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

        // Tracks whether PSTN SCO was started by THIS receiver (not by user via Call Shield UI).
        // Used to stop PSTN SCO (but not full protection) when a call ends and the user had
        // already activated protection manually before the call started.
        private static volatile bool _wasPstnScoAutoStarted = false;

        // Pre-warm: WebSocket được connect sẵn trong lúc RINGING để giảm latency sau OFFHOOK.
        // Nếu cuộc gọi bị reject/missed → disconnect WS trong IDLE handler.
        private static volatile bool _wasPreConnected = false;

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

                    // Pre-warm WebSocket trong lúc chuông reo để giảm latency sau OFFHOOK.
                    // Người dùng thường mất 3-8s để nhấc máy — dùng khoảng thời gian này để
                    // kết nối WebSocket trước, tiết kiệm ~200-500ms sau khi nhấc máy.
                    PreWarmConnection();

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

                    // Pre-warm đã được consume — AutoStartProtection sẽ dùng WS đã connect sẵn
                    _wasPreConnected = false;

                    _isCallActive = true;
                    OnCallStateChanged(CallState.Active, phoneNumber);

                    // Tự động bật protection nếu chưa bật
                    AutoStartProtection();
                }
                else if (stateStr == TelephonyManager.ExtraStateIdle)
                {
                    // Missed/rejected: RINGING → IDLE mà không qua OFFHOOK.
                    // Pre-warmed WebSocket chưa được dùng → disconnect để tránh connection leak.
                    if (!_isCallActive && _wasPreConnected)
                    {
                        _wasPreConnected = false;
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                var svc = App.GetAudioService();
                                if (svc != null && svc.IsConnected && !svc.IsStreaming)
                                {
                                    await svc.StopStreamingAsync();
                                    Debug.WriteLine("[CallReceiver] 🔌 Pre-warmed WS disconnected (call missed/rejected)");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[CallReceiver] Pre-warm cleanup error: {ex.Message}");
                            }
                        });
                    }

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

                            if (_wasProtectionAutoStarted)
                            {
                                // Toàn bộ protection được auto-bật → tắt tất cả (mic + PSTN SCO)
                                AutoStopProtection();
                                _wasProtectionAutoStarted = false;
                                _wasPstnScoAutoStarted = false;
                            }
                            else if (_wasPstnScoAutoStarted)
                            {
                                // Protection đã chạy trước (user bật thủ công), chỉ tắt PSTN SCO
                                // để trả lại AudioMode và giải phóng AudioRecord call-path.
                                AutoStopPstnSco();
                                _wasPstnScoAutoStarted = false;
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
        /// Pre-warm WebSocket connection trong lúc chuông reo để giảm latency sau OFFHOOK.
        /// Chỉ kết nối nếu chưa có connection — hoàn toàn không-destructive.
        /// ConnectAsync() idempotent: nếu đã connected thì return ngay.
        /// </summary>
        private void PreWarmConnection()
        {
            try
            {
                var audioService = App.GetAudioService();
                if (audioService == null || audioService.IsConnected) return;

                _wasPreConnected = true;
                Debug.WriteLine("[CallReceiver] 🔌 Pre-warming WebSocket during ring...");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        bool ok = await audioService.ConnectAsync();
                        Debug.WriteLine($"[CallReceiver] Pre-warm: {(ok ? "✅ WS connected" : "❌ failed")}");
                        if (!ok) _wasPreConnected = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CallReceiver] Pre-warm error: {ex.Message}");
                        _wasPreConnected = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] PreWarmConnection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động bật protection khi nhấc máy (OFFHOOK).
        ///
        /// Hai trường hợp:
        ///   A) Protection chưa chạy → bật mic streaming + PSTN SCO.
        ///   B) Protection đang chạy (user bật thủ công) → chỉ start PSTN SCO
        ///      cho cuộc gọi đang diễn ra (tránh chiếm AudioMode 24/7 ngoài call).
        ///
        /// PSTN SCO luôn dừng khi IDLE (call ended) thông qua _wasPstnScoAutoStarted.
        /// </summary>
        private void AutoStartProtection()
        {
            try
            {
                var audioService = App.GetAudioService();
                if (audioService == null) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (!audioService.IsStreaming)
                        {
                            // ── Case A: Protection chưa chạy → bật từ đầu ────────────
                            Debug.WriteLine("[CallReceiver] 🛡️ Auto-starting protection for call...");
                            _wasProtectionAutoStarted = true;

                            bool connected = await audioService.StartStreamingAsync();
                            if (connected)
                            {
                                ServiceHelper.StartProtectionService();
                                Debug.WriteLine("[CallReceiver] ✅ Mic streaming started");

                                // Bắt PSTN call audio ngay khi cuộc gọi active
                                _wasPstnScoAutoStarted = true;
                                bool scoOk = await audioService.StartPstnScoAsync();
                                Debug.WriteLine($"[CallReceiver] PSTN SCO: {(scoOk ? "✅ started" : "❌ failed")}");
                            }
                            else
                            {
                                Debug.WriteLine("[CallReceiver] ❌ Failed to auto-start protection");
                                _wasProtectionAutoStarted = false;
                            }
                        }
                        else
                        {
                            // ── Case B: Protection đang chạy → chỉ start PSTN SCO ────
                            // Tránh chiếm AudioMode khi không có cuộc gọi (fix "24/7 issue").
                            Debug.WriteLine("[CallReceiver] Protection already active — starting PSTN SCO for call");
                            _wasPstnScoAutoStarted = true;
                            bool scoOk = await audioService.StartPstnScoAsync();
                            Debug.WriteLine($"[CallReceiver] PSTN SCO: {(scoOk ? "✅ started" : "❌ failed")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CallReceiver] Auto-start error: {ex.Message}");
                        _wasProtectionAutoStarted = false;
                        _wasPstnScoAutoStarted = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] AutoStartProtection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động tắt toàn bộ protection khi kết thúc cuộc gọi
        /// (chỉ dùng khi protection được tự động bật bởi CallStateReceiver).
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
                            await audioService.StopStreamingAsync(); // internally calls StopPstnScoAsync()
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
        /// Chỉ tắt PSTN SCO capture, giữ nguyên mic streaming.
        /// Dùng khi protection được user bật thủ công trước khi có cuộc gọi:
        /// cuộc gọi kết thúc → trả AudioMode về bình thường, giải phóng call-path
        /// AudioRecord, nhưng mic streaming vẫn tiếp tục.
        /// </summary>
        private void AutoStopPstnSco()
        {
            try
            {
                var audioService = App.GetAudioService();
                if (audioService == null) return;

                Debug.WriteLine("[CallReceiver] 🛑 Auto-stopping PSTN SCO (call ended, mic streaming kept)...");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await audioService.StopPstnScoAsync();
                        Debug.WriteLine("[CallReceiver] ✅ PSTN SCO auto-stopped, mic streaming continues");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CallReceiver] AutoStopPstnSco error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallReceiver] AutoStopPstnSco error: {ex.Message}");
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
