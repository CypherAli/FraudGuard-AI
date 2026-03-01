#if ANDROID
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using System.Net.WebSockets;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Bắt âm thanh cuộc gọi PSTN (điện thoại di động thông thường) bằng kỹ thuật
    /// "Virtual Bluetooth HFP" kết hợp setCommunicationDevice (API 31+).
    ///
    /// Chiến lược (thử theo thứ tự, dừng ở cái đầu tiên thành công):
    ///   S0:       VOICE_COMM_NORMAL          — MIUI/ColorOS/OriginOS OEM fast path (zero side-effects)
    ///   S_A31_IC: API31 + earpiece + IN_CALL + VOICE_COMM   — Pixel/Nokia/OnePlus Android 12-14
    ///   S_A31_IN: API31 + earpiece + IN_COMM + VOICE_COMM   — AOSP Android 14 strict (IN_CALL blocked)
    ///   S1:       VOICE_CALL                 — ideal (thường cần CAPTURE_AUDIO_OUTPUT, hiếm thành công)
    ///   S2:       VOICE_DOWNLINK             — explicit caller side (thường cần CAPTURE_AUDIO_OUTPUT)
    ///   S3:       SCO + VOICE_CALL           — virtual-HFP trick
    ///   S4:       SCO + VOICE_DOWNLINK       — virtual-HFP trick
    ///   S5:       SCO + VOICE_COMM + IN_CALL — Samsung OneUI best bet (cả 2 chiều)
    ///   S6:       SCO + MIC                  — Snapdragon SCO fallback
    ///   S_VR:     VOICE_RECOGNITION          — Huawei/HiSilicon HAL (source = 6)
    ///   S7:       VOICE_COMM + IN_CALL       — mic-only fallback (uplink only)
    ///   S8:       UNPROCESSED_ADC            — Qualcomm DSP raw output (API 24+)
    ///   S_B28:    AudioRecord.Builder + setPreferredDevice(earpiece) — API 28+ last resort
    ///
    /// Android 14+ (API 34) improvements vs. trước đây:
    ///   • S_A31_IC/IN: setCommunicationDevice() thay thế startBluetoothSco() deprecated (API 31)
    ///   • SetModeInCallSafe(): MODE_IN_CALL → fallback IN_COMMUNICATION nếu bị SecurityException
    ///   • StartBluetoothScoWithRetryAsync(): 3 lần thử, timeout 3s/lần (tăng từ 2.5s)
    ///   • S_VR: VOICE_RECOGNITION path riêng cho Huawei/HiSilicon
    ///   • S_B28: AudioRecord.Builder + SetPrivacySensitive(false) + SetPreferredDevice
    ///   • RestoreAudio(): ClearCommunicationDevice() để dọn dẹp đúng cách
    ///
    /// Gửi audio qua WebSocket với prefix 0x01 (CHANNEL_PSTN / downlink).
    /// </summary>
    public class PstnScoCallCaptureService : IDisposable
    {
        private const string TAG             = "FraudGuard.PSTN.SCO";
        private const int    SAMPLE_RATE     = 16000;
        private const int    BUFFER_SIZE     = 8192;
        private const int    BYTES_PER_SAMPLE = 2;

        // Same channel byte as VoIP — backend treats as caller-side audio
        public const byte CHANNEL_PSTN = 0x01;

        // ── Device detection ─────────────────────────────────────────────────
        // Detect Samsung để dùng Samsung-optimized strategy order.
        // Samsung OneUI HAL có đặc tính riêng: setCommunicationDevice() bị block,
        // VOICE_CALL/DOWNLINK cần CAPTURE_AUDIO_OUTPUT (không bao giờ granted cho non-system app),
        // nhưng SCO+VOICE_COMM+IN_CALL (S5) hoạt động rất tốt.
        private static readonly bool IsSamsung =
            string.Equals(
                global::Android.OS.Build.Manufacturer,
                "samsung",
                StringComparison.OrdinalIgnoreCase);

        // ── AudioSource values (Android API constants) ────────────────────────
        private const AudioSource SRC_VOICE_CALL     = (AudioSource)4; // uplink+downlink (CAPTURE_AUDIO_OUTPUT)
        private const AudioSource SRC_VOICE_DOWNLINK = (AudioSource)3; // caller-side     (CAPTURE_AUDIO_OUTPUT)
        private const AudioSource SRC_VOICE_COMM     = (AudioSource)7; // VOICE_COMMUNICATION (app permission OK)
        private const AudioSource SRC_VOICE_RECOG    = (AudioSource)6; // VOICE_RECOGNITION   (ít restrictive hơn)
        private const AudioSource SRC_UNPROCESSED    = (AudioSource)9; // raw ADC output      (API 24+, Qualcomm)

        // ── State ─────────────────────────────────────────────────────────────
        private AudioRecord?             _audioRecord;
        private AudioManager?            _audioManager;
        private BroadcastReceiver?       _scoReceiver;
        private CancellationTokenSource? _cts;
        private volatile bool            _isCapturing;
        private Mode                     _savedMode = Mode.Normal;
        private readonly SemaphoreSlim   _lock = new(1, 1);

        // ── Events ────────────────────────────────────────────────────────────
        public event EventHandler<string>?  StatusChanged;
        public event EventHandler<string>?  ErrorOccurred;
        public event Action<byte[], int>?   PcmDataAvailable;

        public bool    IsCapturing   => _isCapturing;
        public string? StrategyUsed  { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        public async Task<bool> StartAsync(ClientWebSocket webSocket, CancellationToken externalToken)
        {
            if (!await _lock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                Log.Warn(TAG, "StartAsync: lock timeout");
                return false;
            }

            try
            {
                if (_isCapturing) return true;

                var ctx = global::Android.App.Application.Context;
                _audioManager = ctx.GetSystemService(Context.AudioService) as AudioManager;
                if (_audioManager == null)
                {
                    OnError("AudioManager unavailable");
                    return false;
                }

                _savedMode = _audioManager.Mode;

                AudioRecord? ar    = null;
                string       strat = "";

                if (IsSamsung)
                {
                    // ════════════════════════════════════════════════════════════════════
                    // SAMSUNG ONEUI FAST PATH
                    // Samsung HAL đặc thù: setCommunicationDevice() bị restrict, VOICE_CALL/
                    // DOWNLINK cần CAPTURE_AUDIO_OUTPUT (không bao giờ granted → skip ngay).
                    // Chiến lược tối ưu: IN_CALL + VOICE_COMM trước (không cần SCO trên một
                    // số model), rồi mới SCO+VOICE_COMM. Timeout SCO 2000ms (Samsung BT stack
                    // nhanh hơn generic Android — thường kết nối trong 1-1.5s).
                    // ════════════════════════════════════════════════════════════════════

                    Log.Info(TAG, "📱 Samsung OneUI detected — using Samsung-optimized strategy");

                    // ── SS0: IN_CALL + VOICE_COMM (không BT SCO) ─────────────────────────
                    // Samsung S23/S24 Ultra (OneUI 6.x, Exynos + Snapdragon), Galaxy A54/A55:
                    // HAL route cả 2 chiều vào VOICE_COMM khi MODE_IN_CALL được set.
                    // Zero SCO overhead — thử trước khi khởi động BT stack.
                    SetModeInCallSafe();
                    ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                    if (ar != null && VerifyAudioHasContent(ar))
                    {
                        strat = "SAM_INCALL+VOICE_COMM"; // OneUI fast path, no SCO needed
                    }
                    else
                    {
                        SafeRelease(ref ar);

                        // ── SS1-SS2: BT SCO → S5 first (Samsung best bet) ────────────────
                        // Samsung BT stack: timeout 2000ms × tối đa 2 lần thử (vs generic 3000ms×3).
                        // S5 (SCO+VOICE_COMM+IN_CALL) thử TRƯỚC S3/S4 vì Samsung HAL ưu tiên
                        // VOICE_COMM path — không lãng phí thời gian với VOICE_CALL/DOWNLINK.
                        bool scoUp = await StartBluetoothScoWithRetryAsync(ctx, timeoutMs: 2000, maxRetries: 2);
                        Log.Info(TAG, $"Samsung BT SCO: {scoUp}");

                        // SS1: SCO + VOICE_COMM + IN_CALL ← Samsung OneUI champion
                        ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                        if (ar != null) strat = scoUp ? "SAM_SCO+VOICE_COMM+INCALL" : "SAM_SCO_TRY+VOICE_COMM";

                        // SS2: SCO + MIC — Snapdragon Samsung (Galaxy S23/S24 FE, A-series Snapdragon)
                        if (ar == null)
                        {
                            ar = TryAudioRecord(AudioSource.Mic, useCallMode: false);
                            if (ar != null) strat = scoUp ? "SAM_SCO+MIC" : "SAM_SCO_TRY+MIC";
                        }

                        // SS3: SCO + VOICE_DOWNLINK — Samsung Exynos đôi khi expose downlink qua đây
                        if (ar == null)
                        {
                            ar = TryAudioRecord(SRC_VOICE_DOWNLINK, useCallMode: false);
                            if (ar != null) strat = scoUp ? "SAM_SCO+VOICE_DOWNLINK" : "SAM_SCO_TRY+VOICE_DOWNLINK";
                        }
                    }

                    // ── SS_FB: Samsung fallback — VOICE_COMM không SCO (uplink only) ────
                    // Nếu tất cả fail, vẫn bắt được giọng người dùng để phân tích ngữ nghĩa 1 chiều.
                    if (ar == null)
                    {
                        SetModeInCallSafe();
                        ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                        if (ar != null) strat = "SAM_FALLBACK_VOICE_COMM";
                    }
                }
                else
                {
                    // ════════════════════════════════════════════════════════════════════
                    // GENERIC PATH — Xiaomi/OPPO/Vivo/Pixel/Nokia/Huawei/ASUS...
                    // ════════════════════════════════════════════════════════════════════

                    // ── S0: VOICE_COMM_NORMAL — MIUI/ColorOS/OriginOS OEM fast path ────
                    // Một số OEM (Xiaomi MIUI 15, Realme UI, OPPO ColorOS, Vivo OriginOS, Huawei)
                    // expose call audio qua VOICE_COMMUNICATION mà không cần thay đổi audio mode.
                    // Zero side effects — thử trước nhất.
                    ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                    if (ar != null && VerifyAudioHasContent(ar))
                    {
                        strat = "VOICE_COMM_NORMAL";
                    }
                    else
                    {
                        SafeRelease(ref ar);
                    }

                    // ── S_A31_IC: setCommunicationDevice(earpiece) + MODE_IN_CALL (API 31+) ──
                    // Android 12-14+: setCommunicationDevice() là API chính thức thay thế cho
                    // startBluetoothSco() đã deprecated. Force call audio HAL route qua earpiece
                    // communication path → AudioRecord(VOICE_COMM) bắt được cả 2 chiều.
                    // Hiệu quả nhất trên: Pixel 6-8, Nokia, OnePlus, ASUS ZenFone.
                    if (ar == null && OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        ar = TrySetCommunicationDeviceStrategy(Mode.InCall, SRC_VOICE_COMM);
                        if (ar != null) strat = "A31_EARPIECE+IN_CALL";
                    }

                    // ── S_A31_IN: setCommunicationDevice(earpiece) + MODE_IN_COMMUNICATION (API 31+) ─
                    // AOSP Android 14 strict: MODE_IN_CALL có thể bị block với SecurityException
                    // cho non-telephony apps. MODE_IN_COMMUNICATION luôn được phép.
                    if (ar == null && OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        ar = TrySetCommunicationDeviceStrategy(Mode.InCommunication, SRC_VOICE_COMM);
                        if (ar != null) strat = "A31_EARPIECE+IN_COMM";
                    }

                    // ── S1: VOICE_CALL alone ─────────────────────────────────────────────
                    // Thường bị block bởi CAPTURE_AUDIO_OUTPUT permission nhưng thử 1 lần.
                    if (ar == null)
                    {
                        ar = TryAudioRecord(SRC_VOICE_CALL, useCallMode: false);
                        if (ar != null) strat = "VOICE_CALL";
                    }

                    // ── S2: VOICE_DOWNLINK alone ─────────────────────────────────────────
                    // Một số OEM HAL (Exynos/Snapdragon non-Samsung) expose downlink channel.
                    if (ar == null)
                    {
                        ar = TryAudioRecord(SRC_VOICE_DOWNLINK, useCallMode: false);
                        if (ar != null) strat = "VOICE_DOWNLINK";
                    }

                    // ── S3–S6: BT SCO virtual-HFP trick (generic, timeout 3000ms×3) ────
                    if (ar == null)
                    {
                        SetModeInCallSafe();
                        bool scoUp = await StartBluetoothScoWithRetryAsync(ctx);
                        Log.Info(TAG, $"BT SCO result: {scoUp}");

                        // S3: SCO + VOICE_CALL
                        ar = TryAudioRecord(SRC_VOICE_CALL, useCallMode: false);
                        if (ar != null) strat = scoUp ? "SCO+VOICE_CALL" : "SCO_TRY+VOICE_CALL";

                        // S4: SCO + VOICE_DOWNLINK
                        if (ar == null)
                        {
                            ar = TryAudioRecord(SRC_VOICE_DOWNLINK, useCallMode: false);
                            if (ar != null) strat = scoUp ? "SCO+VOICE_DOWNLINK" : "SCO_TRY+VOICE_DOWNLINK";
                        }

                        // S5: SCO + VOICE_COMM + IN_CALL
                        if (ar == null)
                        {
                            ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                            if (ar != null) strat = scoUp ? "SCO+VOICE_COMM+IN_CALL" : "SCO_TRY+VOICE_COMM+IN_CALL";
                        }

                        // S6: SCO + MIC — Snapdragon HAL fallback
                        if (ar == null)
                        {
                            ar = TryAudioRecord(AudioSource.Mic, useCallMode: false);
                            if (ar != null) strat = scoUp ? "SCO+MIC" : "SCO_TRY+MIC";
                        }
                    }
                }

                // ── S_VR: VOICE_RECOGNITION source (API 23+) ─────────────────────────────
                // AudioSource = 6: dùng HAL path khác với VOICE_COMM, ít bị restricted hơn.
                // Hoạt động trên Huawei (HiSilicon Kirin) và một số ASUS/Sony.
                if (ar == null)
                {
                    ar = TryAudioRecord(SRC_VOICE_RECOG, useCallMode: false);
                    if (ar != null) strat = "VOICE_RECOGNITION";
                }

                // ── S7: VOICE_COMM + IN_CALL — mic-only fallback ──────────────────────────
                // Chỉ capture uplink (giọng người dùng). Gửi channel 0x01 để backend vẫn có
                // thể transcribe 1 chiều và phát hiện gian lận qua phân tích ngữ nghĩa.
                if (ar == null)
                {
                    SetModeInCallSafe();
                    ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                    if (ar != null) strat = "FALLBACK_VOICE_COMM+IN_CALL";
                }

                // ── S8: UNPROCESSED ADC (API 24+, Qualcomm DSP) ──────────────────────────
                // AudioSource = 9: raw ADC output trước khi hardware processing.
                // Trên một số Qualcomm Snapdragon devices, DSP expose call audio qua path này.
                if (ar == null && (int)global::Android.OS.Build.VERSION.SdkInt >= 24)
                {
                    ar = TryAudioRecord(SRC_UNPROCESSED, useCallMode: false);
                    if (ar != null) strat = "UNPROCESSED_ADC";
                }

                // ── S_B28: AudioRecord.Builder + setPreferredDevice(earpiece) (API 28+) ───
                // Last resort: explicitly routing capture qua earpiece hardware path.
                // SetPrivacySensitive(false) (API 29+) giảm restriction trên Android 14+.
                if (ar == null && OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    ar = TryAudioRecordBuilder();
                    if (ar != null) strat = "BUILDER_PREFERRED_EARPIECE";
                }

                if (ar == null)
                {
                    RestoreAudio();
                    OnError("PSTN_ALL_STRATEGIES_FAILED");
                    Log.Warn(TAG, "All PSTN capture strategies failed on this device/Android version");
                    return false;
                }

                StrategyUsed = strat;
                _audioRecord = ar;
                _cts         = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                _audioRecord.StartRecording();
                _isCapturing = true;

                Log.Info(TAG, $"✅ PSTN SCO capture STARTED — strategy={strat}");
                OnStatus($"PSTN_SCO_ACTIVE:{strat}");

                _ = Task.Run(() => StreamLoopAsync(webSocket, _cts.Token), _cts.Token);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"StartAsync failed: {ex.Message}");
                OnError(ex.Message);
                RestoreAudio();
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task StopAsync()
        {
            _isCapturing = false;
            _cts?.Cancel();

            bool acquired = await _lock.WaitAsync(TimeSpan.FromSeconds(3));
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        if (_audioRecord != null)
                        {
                            if (_audioRecord.RecordingState == RecordState.Recording)
                                _audioRecord.Stop();
                            _audioRecord.Release();
                            _audioRecord.Dispose();
                            _audioRecord = null;
                        }
                    }
                    catch (Exception ex) { Log.Warn(TAG, $"Cleanup: {ex.Message}"); }
                });

                RestoreAudio();
                Log.Info(TAG, "PSTN SCO capture STOPPED");
                OnStatus("PSTN_SCO_STOPPED");
            }
            finally { if (acquired) _lock.Release(); }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private AudioRecord? TryAudioRecord(AudioSource source, bool useCallMode)
        {
            try
            {
                if (useCallMode && _audioManager != null)
                    SetModeInCallSafe();

                int min = AudioRecord.GetMinBufferSize(
                    SAMPLE_RATE, ChannelIn.Mono, global::Android.Media.Encoding.Pcm16bit);
                if (min < 0) return null;

                int buf = Math.Max(min * 2, BUFFER_SIZE);
                var ar  = new AudioRecord(
                    source, SAMPLE_RATE, ChannelIn.Mono,
                    global::Android.Media.Encoding.Pcm16bit, buf);

                if (ar.State == State.Initialized)
                {
                    Log.Info(TAG, $"  AudioRecord OK: source={source}");
                    return ar;
                }

                ar.Release();
                ar.Dispose();
                Log.Info(TAG, $"  AudioRecord FAIL: source={source}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"  TryAudioRecord({source}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// API 31+ strategy: setCommunicationDevice(earpiece) + setMode + AudioRecord(VOICE_COMM).
        ///
        /// setCommunicationDevice() (API 31) là API chính thức thay thế startBluetoothSco().
        /// Nó báo cho audio HAL biết rằng app đang dùng earpiece cho communication —
        /// HAL sau đó route call audio chain (cả 2 chiều) vào VOICE_COMMUNICATION source.
        ///
        /// Sử dụng AvailableCommunicationDevices (API 31) để chỉ thử devices được hỗ trợ.
        /// </summary>
        private AudioRecord? TrySetCommunicationDeviceStrategy(Mode mode, AudioSource source)
        {
            if (_audioManager == null || !OperatingSystem.IsAndroidVersionAtLeast(31))
                return null;

            try
            {
                AudioDeviceInfo? earpiece = FindEarpieceForCommunication();
                if (earpiece == null)
                {
                    Log.Info(TAG, $"  TrySetCommDev({mode}): earpiece not available");
                    return null;
                }

                // Đặt mode trước để kích hoạt call audio chain trong HAL
                try { _audioManager.Mode = mode; }
                catch (Exception ex) { Log.Warn(TAG, $"  Mode={mode} failed: {ex.Message}"); }

                bool ok = _audioManager.SetCommunicationDevice(earpiece);
                Log.Info(TAG, $"  setCommunicationDevice({earpiece.Type}, mode={mode}): {ok}");

                if (!ok) return null;

                // Chờ HAL settle 150ms trước khi tạo AudioRecord
                System.Threading.Thread.Sleep(150);

                var ar = TryAudioRecord(source, useCallMode: false);
                if (ar == null)
                {
                    // AudioRecord thất bại — undo setCommunicationDevice để không gây side effects
                    try { _audioManager.ClearCommunicationDevice(); } catch { }
                }
                return ar;
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"TrySetCommDevStrategy({mode}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tìm AudioDeviceInfo phù hợp để truyền vào setCommunicationDevice().
        /// Ưu tiên: earpiece → wired headset → bất kỳ device nào available.
        ///
        /// Dùng AvailableCommunicationDevices (API 31) — chỉ trả về devices được phép.
        /// Fallback sang GetDevices(Outputs) nếu API 31 binding chưa có.
        /// </summary>
        private AudioDeviceInfo? FindEarpieceForCommunication()
        {
            if (_audioManager == null) return null;
            try
            {
                // API 31: getAvailableCommunicationDevices() — safe enumeration
                IList<AudioDeviceInfo>? avail = null;
                try { avail = _audioManager.AvailableCommunicationDevices; } catch { }

                if (avail != null && avail.Count > 0)
                {
                    foreach (var d in avail)
                        if (d.Type == AudioDeviceType.BuiltinEarpiece) return d;
                    foreach (var d in avail)
                        if (d.Type == AudioDeviceType.WiredHeadset) return d;
                    // Không tìm thấy earpiece cụ thể — dùng device đầu tiên available
                    return avail[0];
                }

                // Fallback: scan output devices thủ công (API 23+)
                var outputs = _audioManager.GetDevices(GetDevicesTargets.Outputs);
                if (outputs != null)
                {
                    foreach (var d in outputs)
                        if (d.Type == AudioDeviceType.BuiltinEarpiece) return d;
                    foreach (var d in outputs)
                        if (d.Type == AudioDeviceType.WiredHeadset) return d;
                }
            }
            catch (Exception ex) { Log.Warn(TAG, $"FindEarpiece: {ex.Message}"); }
            return null;
        }

        /// <summary>
        /// API 28+ strategy: AudioRecord.Builder với setPreferredDevice(earpiece) và
        /// SetPrivacySensitive(false) (API 29+).
        ///
        /// setPreferredDevice() hint cho HAL route capture qua earpiece hardware path.
        /// SetPrivacySensitive(false) giảm restriction privacy trên Android 14.
        /// </summary>
        private AudioRecord? TryAudioRecordBuilder()
        {
            if (_audioManager == null) return null;
            try
            {
                int min = AudioRecord.GetMinBufferSize(
                    SAMPLE_RATE, ChannelIn.Mono, global::Android.Media.Encoding.Pcm16bit);
                if (min < 0) return null;

                int buf = Math.Max(min * 2, BUFFER_SIZE);
                SetModeInCallSafe();

                // AudioFormat.Builder.SetChannelMask() binding hanya accepts ChannelOut —
                // dùng constructor thông thường để tránh type mismatch, sau đó gọi
                // SetPreferredDevice() trực tiếp trên instance (API 28+).
                var ar = new AudioRecord(
                    SRC_VOICE_COMM, SAMPLE_RATE, ChannelIn.Mono,
                    global::Android.Media.Encoding.Pcm16bit, buf);

                if (ar.State != State.Initialized)
                {
                    ar.Release();
                    ar.Dispose();
                    Log.Info(TAG, "  BUILDER_PREFERRED_EARPIECE: not initialized");
                    return null;
                }

                // API 28+: setPreferredDevice() hint cho AudioRecord route qua earpiece input
                AudioDeviceInfo? preferredDev = null;
                try
                {
                    var inputs = _audioManager.GetDevices(GetDevicesTargets.Inputs);
                    if (inputs != null)
                    {
                        foreach (var d in inputs)
                            if (d.Type == AudioDeviceType.BuiltinEarpiece) { preferredDev = d; break; }
                        if (preferredDev == null)
                            foreach (var d in inputs)
                                if (d.Type == AudioDeviceType.BuiltinMic) { preferredDev = d; break; }
                    }
                }
                catch { }

                if (preferredDev != null)
                {
                    bool set = ar.SetPreferredDevice(preferredDev);
                    Log.Info(TAG, $"  SetPreferredDevice({preferredDev.Type}): {set}");
                }

                Log.Info(TAG, "  BUILDER_PREFERRED_EARPIECE: AudioRecord OK");
                return ar;
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"TryAudioRecordBuilder: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Đặt MODE_IN_CALL an toàn. Trên Android 12+ (API 31+), non-telephony app có thể
        /// nhận SecurityException — fallback sang MODE_IN_COMMUNICATION (luôn được phép).
        /// </summary>
        private void SetModeInCallSafe()
        {
            if (_audioManager == null) return;
            try
            {
                _audioManager.Mode = Mode.InCall;
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"MODE_IN_CALL blocked ({ex.GetType().Name}), using IN_COMMUNICATION");
                try { _audioManager.Mode = Mode.InCommunication; }
                catch (Exception ex2) { Log.Warn(TAG, $"MODE_IN_COMMUNICATION also failed: {ex2.Message}"); }
            }
        }

        /// <summary>
        /// Khởi động BT SCO với retry.
        ///
        /// Samsung: timeoutMs=2000, maxRetries=2 — BT stack Samsung nhanh hơn (~1-1.5s).
        /// Generic: timeoutMs=3000, maxRetries=3 — Android 14 BT stack có thể chậm hơn.
        ///
        /// Trên API 31+: thử setCommunicationDevice(BT_SCO) trước (nhanh và reliable hơn
        /// startBluetoothSco() deprecated). Samsung thường không có BT device thật nhưng
        /// startBluetoothSco() vẫn tạo software SCO path trong audio HAL.
        /// </summary>
        private async Task<bool> StartBluetoothScoWithRetryAsync(
            Context ctx, int timeoutMs = 3000, int maxRetries = 3)
        {
            // API 31+: nếu có BT SCO device đang kết nối, dùng setCommunicationDevice() mới
            // Nhanh hơn và reliable hơn startBluetoothSco() deprecated.
            if (OperatingSystem.IsAndroidVersionAtLeast(31) && _audioManager != null)
            {
                try
                {
                    IList<AudioDeviceInfo>? avail = null;
                    try { avail = _audioManager.AvailableCommunicationDevices; } catch { }
                    if (avail != null)
                    {
                        foreach (var dev in avail)
                        {
                            if (dev.Type == AudioDeviceType.BluetoothSco ||
                                dev.Type == AudioDeviceType.BluetoothA2dp)
                            {
                                bool ok = _audioManager.SetCommunicationDevice(dev);
                                Log.Info(TAG, $"  setCommunicationDevice(BT={dev.Type}): {ok}");
                                if (ok) return true;
                            }
                        }
                    }
                }
                catch (Exception ex) { Log.Warn(TAG, $"  setCommunicationDevice(BT): {ex.Message}"); }
            }

            // Legacy SCO API (deprecated API 31 nhưng vẫn hoạt động trên Samsung/Xiaomi API 34)
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                bool result = await StartBluetoothScoAsync(ctx, timeoutMs: timeoutMs);
                if (result)
                {
                    Log.Info(TAG, $"BT SCO connected on attempt {attempt}/{maxRetries}");
                    return true;
                }
                Log.Warn(TAG, $"BT SCO attempt {attempt}/{maxRetries} failed");
                if (attempt < maxRetries)
                    await Task.Delay(300); // giảm từ 500ms — Samsung reconnect nhanh hơn
            }
            return false;
        }

        /// <summary>
        /// Starts Bluetooth SCO subsystem. Trên Samsung OneUI, kích hoạt software SCO path
        /// trong audio HAL ngay cả khi không có Bluetooth device thật.
        ///
        /// Bug cũ: TCS resolve ngay tại broadcast DISCONNECTED đầu tiên → scoUp luôn false.
        /// Fix: chỉ resolve sau khi đã thấy CONNECTING (state=2) xác nhận SCO thực sự đã thử.
        /// </summary>
        private async Task<bool> StartBluetoothScoAsync(Context ctx, int timeoutMs = 3000)
        {
            try
            {
                if (_audioManager == null) return false;

                _audioManager.Mode = Mode.InCall;

                var tcs = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // Unregister previous receiver nếu còn từ lần thử trước (retry path)
                if (_scoReceiver != null)
                {
                    try { ctx.UnregisterReceiver(_scoReceiver); } catch { }
                    _scoReceiver.Dispose();
                    _scoReceiver = null;
                }

                _scoReceiver = new ScoStateReceiver(tcs);
                ctx.RegisterReceiver(
                    _scoReceiver,
                    new IntentFilter("android.media.ACTION_SCO_AUDIO_STATE_UPDATED"));

                _audioManager.StartBluetoothSco();
                _audioManager.BluetoothScoOn = true;

                Log.Info(TAG, $"BT SCO started — waiting up to {timeoutMs}ms...");

                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                try
                {
                    return await tcs.Task.WaitAsync(timeout.Token);
                }
                catch (System.OperationCanceledException)
                {
                    // No state broadcast → device có thể đã active SCO (check flag)
                    Log.Warn(TAG, "BT SCO state broadcast timed out — checking BluetoothScoOn flag");
                    return _audioManager.BluetoothScoOn;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"StartBluetoothScoAsync: {ex.Message}");
                return false;
            }
        }

        private void RestoreAudio()
        {
            try
            {
                if (_audioManager != null)
                {
                    // API 31+: clear setCommunicationDevice() override trước khi restore mode
                    // Bắt buộc nếu không sẽ gây audio routing issue sau khi cuộc gọi kết thúc
                    if (OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        try { _audioManager.ClearCommunicationDevice(); }
                        catch (Exception ex) { Log.Warn(TAG, $"ClearCommunicationDevice: {ex.Message}"); }
                    }

                    _audioManager.StopBluetoothSco();
                    _audioManager.BluetoothScoOn = false;
                    _audioManager.Mode = _savedMode;
                }
            }
            catch (Exception ex) { Log.Warn(TAG, $"RestoreAudio: {ex.Message}"); }

            try
            {
                if (_scoReceiver != null)
                {
                    global::Android.App.Application.Context
                        .UnregisterReceiver(_scoReceiver);
                    _scoReceiver.Dispose();
                    _scoReceiver = null;
                }
            }
            catch { }
        }

        // ── Streaming loop ────────────────────────────────────────────────────

        private async Task StreamLoopAsync(ClientWebSocket webSocket, CancellationToken ct)
        {
            byte[] sendBuf = new byte[1 + BUFFER_SIZE];
            sendBuf[0] = CHANNEL_PSTN;
            byte[] pcmBuf = new byte[BUFFER_SIZE];
            int    errors = 0;
            long   chunks = 0;

            Log.Info(TAG, "🔄 PSTN SCO stream loop started");

            try
            {
                while (_isCapturing && !ct.IsCancellationRequested)
                {
                    if (_audioRecord?.RecordingState != RecordState.Recording) break;

                    try
                    {
                        int bytesRead = await _audioRecord.ReadAsync(pcmBuf, 0, pcmBuf.Length);

                        if (bytesRead <= 0)
                        {
                            if (bytesRead < 0) errors++;
                            if (errors >= 10) break;
                            continue;
                        }

                        errors = 0;

                        if (bytesRead % BYTES_PER_SAMPLE != 0)
                            bytesRead = (bytesRead / BYTES_PER_SAMPLE) * BYTES_PER_SAMPLE;

                        // Feed waveform drawable
                        PcmDataAvailable?.Invoke(pcmBuf, bytesRead);

                        // Skip absolute silence (giảm bandwidth, không gửi silence frames)
                        if (IsAbsoluteSilence(pcmBuf, bytesRead)) continue;

                        Buffer.BlockCopy(pcmBuf, 0, sendBuf, 1, bytesRead);

                        if (webSocket.State == WebSocketState.Open)
                        {
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(sendBuf, 0, 1 + bytesRead),
                                WebSocketMessageType.Binary,
                                endOfMessage: true,
                                ct);

                            chunks++;
                            if (chunks % 100 == 0)
                                Log.Info(TAG, $"📡 PSTN SCO: {chunks} chunks sent");
                        }
                    }
                    catch (System.OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        errors++;
                        Log.Warn(TAG, $"Read error #{errors}: {ex.Message}");
                        if (errors >= 10) break;
                        await Task.Delay(100, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"StreamLoop fatal: {ex.Message}");
                OnError(ex.Message);
            }

            Log.Info(TAG, $"🔴 PSTN SCO stream ended — {chunks} chunks sent");
        }

        /// <summary>
        /// Kiểm tra AudioRecord có thực sự capture call audio không.
        /// Dùng cho S0 (VOICE_COMM_NORMAL) để loại OEM HAL không route call audio vào VOICE_COMM.
        ///
        /// Đọc tối đa 4 buffers (~1 giây) — dừng sớm ngay khi phát hiện audio thật.
        /// Lý do tăng từ 1→4 buffers: caller vừa nhấc máy thường im lặng 300-700ms đầu
        /// trước khi nói "Xin chào" → verify 256ms (1 buffer) cũ hay bị false-negative.
        /// </summary>
        private static bool VerifyAudioHasContent(AudioRecord ar)
        {
            try
            {
                ar.StartRecording();
                var buf = new byte[BUFFER_SIZE];
                for (int i = 0; i < 4; i++) // tối đa ~1s (4 × 256ms/buffer)
                {
                    int bytesRead = ar.Read(buf, 0, buf.Length);
                    if (bytesRead > 0 && !IsAbsoluteSilence(buf, bytesRead))
                    {
                        ar.Stop();
                        return true; // có audio thật — dừng sớm, không cần đọc thêm
                    }
                }
                ar.Stop();
                return false; // 1s toàn silence → OEM không route call audio qua path này
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAbsoluteSilence(byte[] buf, int len)
        {
            double energy  = 0;
            int    samples = len / BYTES_PER_SAMPLE;
            for (int i = 0; i < len; i += BYTES_PER_SAMPLE)
            {
                short s = (short)(buf[i] | (buf[i + 1] << 8));
                energy += Math.Abs((int)s); // cast to int: tránh OverflowException với short.MinValue
            }
            return samples > 0 && (energy / samples) < 80;
        }

        private static void SafeRelease(ref AudioRecord? ar)
        {
            if (ar == null) return;
            try
            {
                if (ar.RecordingState == RecordState.Recording) ar.Stop();
                ar.Release();
                ar.Dispose();
            }
            catch { }
            ar = null;
        }

        private void OnStatus(string msg) => StatusChanged?.Invoke(this, msg);
        private void OnError(string msg)  => ErrorOccurred?.Invoke(this, msg);

        public void Dispose()
        {
            _scoReceiver?.Dispose();
            _audioRecord?.Dispose();
            _cts?.Dispose();
        }
    }

    /// <summary>
    /// BroadcastReceiver lắng nghe thay đổi trạng thái Bluetooth SCO.
    ///
    /// Android luôn bắn một broadcast DISCONNECTED (0) ngay khi startBluetoothSco()
    /// được gọi để báo "trạng thái hiện tại trước khi thử kết nối".
    /// Sequence đúng: 0 (init) → 2 (connecting) → 1 (connected) | 0 (failed).
    ///
    /// Bug cũ: TCS được resolve ngay tại broadcast 0 đầu tiên → scoUp luôn = false.
    /// Fix: chỉ resolve TCS sau khi đã thấy state=2 (CONNECTING), xác nhận SCO
    /// thực sự đã được thử. Broadcast DISCONNECTED đầu tiên (init) bị bỏ qua.
    /// </summary>
    // No [BroadcastReceiver] attribute — registered dynamically via RegisterReceiver(), not in manifest
    internal sealed class ScoStateReceiver : BroadcastReceiver
    {
        private const int SCO_AUDIO_STATE_CONNECTED  = 1;
        private const int SCO_AUDIO_STATE_CONNECTING = 2;
        private readonly TaskCompletionSource<bool> _tcs;
        private bool _seenConnecting = false;

        public ScoStateReceiver(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != "android.media.ACTION_SCO_AUDIO_STATE_UPDATED") return;
            int state = intent.GetIntExtra("android.media.extra.SCO_AUDIO_STATE", -1);
            Log.Debug("FraudGuard.SCO", $"SCO state update: {state}");

            if (state == SCO_AUDIO_STATE_CONNECTING)
            {
                _seenConnecting = true; // SCO handshake đang diễn ra — chờ kết quả cuối
                return;
            }

            // Chỉ resolve TCS sau khi đã thấy CONNECTING — tức là hardware thực sự đã thử.
            // Broadcast DISCONNECTED đầu tiên (init state) không phải kết quả của request.
            if (_seenConnecting || state == SCO_AUDIO_STATE_CONNECTED)
            {
                _tcs.TrySetResult(state == SCO_AUDIO_STATE_CONNECTED);
            }
            // else: initial DISCONNECTED (init state) — ignore, tiếp tục chờ
        }
    }
}
#endif
