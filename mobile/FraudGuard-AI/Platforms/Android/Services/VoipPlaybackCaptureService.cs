#if ANDROID
using Android.Media;
using Android.Media.Projection;
using Android.Util;
using System.Net.WebSockets;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Bắt âm thanh downlink (giọng đầu dây bên kia) từ các VoIP app
    /// (Zalo, Messenger, WhatsApp, Telegram...) qua Android AudioPlaybackCapture API.
    ///
    /// Yêu cầu: Android 10+ (API 29) và MediaProjection token từ user.
    ///
    /// Protocol: Mỗi chunk gửi qua WebSocket được prefix bởi 1 byte:
    ///   0x00 = Mic stream (uplink  — giọng USER)
    ///   0x01 = VoIP stream (downlink — giọng KẺ LỪA ĐẢO)
    ///
    /// Backend dùng prefix để label transcript: [USER] vs [SCAMMER]
    /// → Gemini Agent nhận đối thoại 2 chiều, phân tích chính xác hơn
    /// </summary>
    public class VoipPlaybackCaptureService : IDisposable
    {
        private const string TAG = "FraudGuard.VoIP";

        // ── Audio Config ─────────────────────────────────────────────────────
        private const int SAMPLE_RATE  = 16000;
        private const int BUFFER_SIZE  = 8192;
        private const int BYTES_PER_SAMPLE = 2; // PCM 16-bit

        /// <summary>
        /// Channel byte prefix cho protocol multiplexing.
        /// Backend (audio_processor.go) đọc byte đầu tiên để phân loại.
        /// </summary>
        public const byte CHANNEL_MIC  = 0x00;
        public const byte CHANNEL_VOIP = 0x01;

        // ── State ─────────────────────────────────────────────────────────────
        private AudioRecord?              _audioRecord;
        private MediaProjection?          _mediaProjection;
        private CancellationTokenSource?  _cts;
        private volatile bool             _isCapturing;
        private readonly SemaphoreSlim    _lock = new(1, 1);

        // ── Events (mirroring AudioStreamingServiceLowLevel) ─────────────────
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;

        public bool IsCapturing => _isCapturing;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Khởi động capture VoIP audio.
        /// Trả về true nếu AudioRecord khởi tạo thành công.
        /// </summary>
        public async Task<bool> StartAsync(MediaProjection mediaProjection, ClientWebSocket webSocket, CancellationToken externalToken)
        {
            if (!await _lock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                Log.Warn(TAG, "StartAsync: lock timeout");
                return false;
            }

            try
            {
                if (_isCapturing)
                {
                    Log.Info(TAG, "VoIP capture already active");
                    return true;
                }

                // Android 10+ required
                if ((int)global::Android.OS.Build.VERSION.SdkInt < 29)
                {
                    Log.Warn(TAG, "AudioPlaybackCapture requires Android 10+ (API 29). Current: " +
                             global::Android.OS.Build.VERSION.SdkInt);
                    OnStatus("VoIP capture unavailable (requires Android 10+)");
                    return false;
                }

                _mediaProjection = mediaProjection;

                // Build AudioPlaybackCapture config — bắt USAGE_VOICE_COMMUNICATION
                var captureConfig = new AudioPlaybackCaptureConfiguration.Builder(_mediaProjection)
                    .AddMatchingUsage(AudioUsageKind.VoiceCommunication)
                    .AddMatchingUsage(AudioUsageKind.VoiceCommunicationSignalling)
                    .Build();

                int minBuf = AudioRecord.GetMinBufferSize(
                    SAMPLE_RATE,
                    ChannelIn.Mono,
                    global::Android.Media.Encoding.Pcm16bit);

                if (minBuf < 0)
                {
                    OnError($"GetMinBufferSize failed: {minBuf}");
                    return false;
                }

                int bufSize = Math.Max(minBuf * 2, BUFFER_SIZE);

                // AudioPlaybackCapture bắt âm thanh OUTPUT → dùng OUTPUT channel mask.
                // ChannelOut.Mono = CHANNEL_OUT_MONO = 4 ✓  (đúng cho AudioPlaybackCapture)
                // ChannelIn.Mono = CHANNEL_IN_MONO  = 16 ✗  (dành cho AudioRecord thông thường)
                _audioRecord = new AudioRecord.Builder()
                    .SetAudioPlaybackCaptureConfig(captureConfig)
                    .SetAudioFormat(new AudioFormat.Builder()
                        .SetEncoding(global::Android.Media.Encoding.Pcm16bit)
                        .SetSampleRate(SAMPLE_RATE)
                        .SetChannelMask(ChannelOut.Mono)  // CHANNEL_OUT_MONO = 4, correct for playback capture
                        .Build())
                    .SetBufferSizeInBytes(bufSize)
                    .Build();

                if (_audioRecord.State != State.Initialized)
                {
                    // Common cause: regular PSTN call — modem audio not accessible via AudioPlaybackCapture.
                    // VoIP apps (Zalo, Messenger, WhatsApp) route audio through Android audio stack so they work.
                    OnError("PSTN_OR_INIT_FAILED");
                    _audioRecord.Dispose();
                    _audioRecord = null;
                    return false;
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                _audioRecord.StartRecording();
                _isCapturing = true;

                Log.Info(TAG, $"✅ VoIP PlaybackCapture STARTED — buf={bufSize}B, rate={SAMPLE_RATE}Hz");
                OnStatus("VoIP audio capture active — listening to caller");

                // Kick off streaming loop (không await — chạy background)
                _ = Task.Run(() => StreamLoopAsync(webSocket, _cts.Token), _cts.Token);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"StartAsync failed: {ex.Message}");
                OnError(ex.Message);
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task StopAsync()
        {
            // Signal stop immediately regardless of lock acquisition so the streaming
            // loop terminates even if a concurrent StartAsync() holds the lock.
            _isCapturing = false;
            _cts?.Cancel();

            bool lockAcquired = await _lock.WaitAsync(TimeSpan.FromSeconds(3));

            try
            {
                // Release AudioRecord whether or not we got the lock — prevents
                // the resource staying open if lock timed out (e.g. StartAsync slow).
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
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Cleanup error: {ex.Message}");
                    }
                });

                Log.Info(TAG, "VoIP PlaybackCapture STOPPED");
                OnStatus("VoIP capture stopped");
            }
            finally
            {
                if (lockAcquired) _lock.Release();
            }
        }

        // ── Streaming loop ────────────────────────────────────────────────────

        private async Task StreamLoopAsync(ClientWebSocket webSocket, CancellationToken ct)
        {
            // Prepend buffer: [CHANNEL_VOIP (1 byte)] + [PCM data (BUFFER_SIZE bytes)]
            // Tái dùng array để tránh GC pressure trong tight loop
            byte[] sendBuf = new byte[1 + BUFFER_SIZE];
            sendBuf[0] = CHANNEL_VOIP; // constant prefix

            // PCM read buffer (không có prefix)
            byte[] pcmBuf  = new byte[BUFFER_SIZE];
            int    errors  = 0;
            long   chunks  = 0;

            // PSTN detection: if we get only absolute-silence for >8s after init,
            // it means the call is likely PSTN (regular phone call) — notify UI.
            const int PSTN_DETECT_CHUNKS = 200; // ~8s at 8KB/~40ms per chunk
            int silentChunks = 0;
            bool pstnWarningFired = false;

            Log.Info(TAG, "🔄 VoIP streaming loop started");

            try
            {
                while (_isCapturing && !ct.IsCancellationRequested)
                {
                    if (_audioRecord?.RecordingState != RecordState.Recording)
                        break;

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

                        // Align to sample boundary
                        if (bytesRead % BYTES_PER_SAMPLE != 0)
                            bytesRead = (bytesRead / BYTES_PER_SAMPLE) * BYTES_PER_SAMPLE;

                        // Energy check — bỏ qua silence tuyệt đối
                        if (IsAbsoluteSilence(pcmBuf, bytesRead))
                        {
                            silentChunks++;
                            // Sustained silence → likely PSTN call (regular cellular)
                            if (!pstnWarningFired && silentChunks >= PSTN_DETECT_CHUNKS)
                            {
                                pstnWarningFired = true;
                                Log.Warn(TAG, "⚠️ No VoIP audio in 8s — likely a regular PSTN call");
                                OnStatus("PSTN_DETECTED");
                            }
                            continue;
                        }

                        // Got real audio — reset PSTN counter
                        silentChunks = 0;
                        pstnWarningFired = false;

                        // Build send buffer: [0x01][PCM...]
                        Buffer.BlockCopy(pcmBuf, 0, sendBuf, 1, bytesRead);

                        if (webSocket.State == WebSocketState.Open)
                        {
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(sendBuf, 0, 1 + bytesRead),
                                WebSocketMessageType.Binary,
                                endOfMessage: true,
                                ct);

                            chunks++;
                            if (chunks % 50 == 0)
                                Log.Info(TAG, $"📡 VoIP stream: {chunks} chunks sent");
                        }
                    }
                    catch (OperationCanceledException) { break; }
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

            Log.Info(TAG, $"🔴 VoIP streaming loop ended — {chunks} chunks total");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Trả true nếu chunk là silence tuyệt đối (năng lượng < 80).
        /// Ngưỡng VoIP thấp hơn mic vì playback audio thường louder.
        /// </summary>
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

        private void OnStatus(string msg) =>
            StatusChanged?.Invoke(this, msg);

        private void OnError(string msg) =>
            ErrorOccurred?.Invoke(this, msg);

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            try { _audioRecord?.Release(); } catch { }
            try { _audioRecord?.Dispose(); } catch { }
            _lock.Dispose();
        }
    }
}
#endif
