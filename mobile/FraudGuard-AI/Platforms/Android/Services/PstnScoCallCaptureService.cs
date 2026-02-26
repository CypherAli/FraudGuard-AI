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
    /// "Virtual Bluetooth HFP" — đánh lừa Android audio HAL bằng cách khởi tạo
    /// Bluetooth SCO subsystem trong MODE_IN_CALL, buộc audio route qua SCO path.
    ///
    /// Trên Samsung OneUI (và một số thiết bị khác), audio HAL tạo ra software SCO path
    /// ngay cả khi không có thiết bị Bluetooth thật — AudioRecord với source
    /// VOICE_COMMUNICATION sau đó nhận được audio từ call audio chain (cả 2 chiều).
    ///
    /// Chiến lược (thử theo thứ tự, dừng ở cái đầu tiên thành công):
    ///   S1: AudioSource.VOICE_CALL (= 4)       — cần CAPTURE_AUDIO_OUTPUT, thường fail
    ///   S2: VOICE_COMMUNICATION + MODE_IN_CALL  — Samsung HAL trick, không cần BT
    ///   S3: BT SCO + VOICE_COMMUNICATION        — virtual BT HFP trick
    ///   S4: BT SCO + MIC                        — fallback SCO mode
    ///
    /// Gửi audio qua WebSocket với prefix 0x01 (CHANNEL_VOIP / downlink)
    /// để backend xử lý giống VoIP audio.
    /// </summary>
    public class PstnScoCallCaptureService : IDisposable
    {
        private const string TAG = "FraudGuard.PSTN.SCO";
        private const int SAMPLE_RATE     = 16000;
        private const int BUFFER_SIZE     = 8192;
        private const int BYTES_PER_SAMPLE = 2;

        // Same channel byte as VoIP so backend treats it as caller-side audio
        public const byte CHANNEL_PSTN = 0x01;

        // ── AudioSource values (Android API constants) ───────────────────────
        private const AudioSource SRC_VOICE_CALL  = (AudioSource)4; // both uplink+downlink
        private const AudioSource SRC_VOICE_COMM  = (AudioSource)7; // VOICE_COMMUNICATION

        // ── State ────────────────────────────────────────────────────────────
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

        public bool IsCapturing  => _isCapturing;
        public string? StrategyUsed { get; private set; }

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

                AudioRecord? ar  = null;
                string       strat = "";

                // ── Strategy 1: VOICE_CALL (usually needs system permission) ──
                ar = TryAudioRecord(SRC_VOICE_CALL, useCallMode: false);
                if (ar != null) strat = "VOICE_CALL";

                // ── Strategy 2: VOICE_COMMUNICATION + MODE_IN_CALL (Samsung) ──
                if (ar == null)
                {
                    _audioManager.Mode = Mode.InCall;
                    ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                    if (ar != null) strat = "VOICE_COMM+IN_CALL";
                }

                // ── Strategy 3 & 4: Bluetooth SCO virtual HFP trick ───────────
                if (ar == null)
                {
                    bool scoUp = await StartBluetoothScoAsync(ctx);
                    Log.Info(TAG, $"BT SCO result: {scoUp}");

                    // S3: SCO + VOICE_COMMUNICATION
                    ar = TryAudioRecord(SRC_VOICE_COMM, useCallMode: false);
                    if (ar != null)
                    {
                        strat = scoUp ? "SCO_CONN+VOICE_COMM" : "SCO_STARTED+VOICE_COMM";
                    }
                    else
                    {
                        // S4: SCO + MIC (most likely to succeed — picks up SCO audio path)
                        ar = TryAudioRecord(AudioSource.Mic, useCallMode: false);
                        if (ar != null) strat = scoUp ? "SCO_CONN+MIC" : "SCO_STARTED+MIC";
                    }
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
                    _audioManager.Mode = Mode.InCall;

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
        /// Starts Bluetooth SCO subsystem. On Samsung OneUI, this triggers a software
        /// SCO path in the audio HAL even without a paired Bluetooth device, routing
        /// call audio into AudioRecord's input chain.
        /// </summary>
        private async Task<bool> StartBluetoothScoAsync(Context ctx)
        {
            try
            {
                if (_audioManager == null) return false;

                _audioManager.Mode = Mode.InCall;

                var tcs = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                _scoReceiver = new ScoStateReceiver(tcs);
                ctx.RegisterReceiver(
                    _scoReceiver,
                    new IntentFilter("android.media.ACTION_SCO_AUDIO_STATE_UPDATED"));

                _audioManager.StartBluetoothSco();
                _audioManager.BluetoothScoOn = true;

                Log.Info(TAG, "BT SCO started — waiting for state update...");

                // Wait up to 2.5s for SCO state change
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));
                try
                {
                    return await tcs.Task.WaitAsync(timeout.Token);
                }
                catch (System.OperationCanceledException)
                {
                    // No state broadcast → device may still have SCO active (check the flag)
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
            byte[] pcmBuf  = new byte[BUFFER_SIZE];
            int    errors  = 0;
            long   chunks  = 0;

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

                        // Skip absolute silence
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
                                Log.Info(TAG, $"📡 PSTN SCO: {chunks} chunks");
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

            Log.Info(TAG, $"🔴 PSTN SCO stream ended — {chunks} chunks");
        }

        private static bool IsAbsoluteSilence(byte[] buf, int len)
        {
            double energy = 0;
            int samples = len / BYTES_PER_SAMPLE;
            for (int i = 0; i < len; i += BYTES_PER_SAMPLE)
            {
                short s = (short)(buf[i] | (buf[i + 1] << 8));
                energy += Math.Abs(s);
            }
            return samples > 0 && (energy / samples) < 80;
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
    /// ACTION_SCO_AUDIO_STATE_UPDATED: EXTRA_SCO_AUDIO_STATE = 0 (disc) | 1 (conn) | 3 (error)
    /// </summary>
    // No [BroadcastReceiver] attribute — registered dynamically via RegisterReceiver(), not in manifest
    internal sealed class ScoStateReceiver : BroadcastReceiver
    {
        private const int SCO_AUDIO_STATE_CONNECTED = 1;
        private readonly TaskCompletionSource<bool> _tcs;

        public ScoStateReceiver(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != "android.media.ACTION_SCO_AUDIO_STATE_UPDATED") return;
            int state = intent.GetIntExtra("android.media.extra.SCO_AUDIO_STATE", -1);
            Log.Debug("FraudGuard.SCO", $"SCO state update: {state}");
            _tcs.TrySetResult(state == SCO_AUDIO_STATE_CONNECTED);
        }
    }
}
#endif
