using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using Android.Util;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Alternative implementation sử dụng Android.Media.AudioRecord
    /// Cho phép low-level control và streaming trực tiếp (không qua file)
    /// Cấu hình: 16000Hz, Mono, PCM 16-bit
    /// URL động: Đọc từ Preferences (Settings Page)
    /// </summary>
    public class AudioStreamingServiceLowLevel : IDisposable
    {
        #region Fields & Properties

        private ClientWebSocket _webSocket;
        private AudioRecord _audioRecord;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isStreaming;
        private volatile bool _isConnected;
        private readonly SemaphoreSlim _startStopLock = new SemaphoreSlim(1, 1);

        // Cấu hình Audio (khớp với backend Deepgram)
        private const int SAMPLE_RATE = 16000;
        private const ChannelIn CHANNEL_CONFIG = ChannelIn.Mono;
        private const Android.Media.Encoding AUDIO_FORMAT = Android.Media.Encoding.Pcm16bit;
        private const int BUFFER_SIZE = 8192; // Increased from 4096 to reduce fragmentation
        private const int BYTES_PER_SAMPLE = 2; // 16-bit = 2 bytes
        private const string TAG = "FraudGuard"; // Logcat tag for filtering

        public event EventHandler<AlertEventArgs> AlertReceived;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        
        /// <summary>
        /// Check if currently streaming audio
        /// </summary>
        public bool IsStreaming => _isStreaming;
        
        /// <summary>
        /// Check if WebSocket is connected
        /// </summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region Constructor

        public AudioStreamingServiceLowLevel()
        {
            // Initialize fields to prevent null reference
            _webSocket = null!;
            _audioRecord = null!;
            _cancellationTokenSource = null!;
            _isStreaming = false;
            _isConnected = false;
            // URL will be retrieved dynamically from Settings when connecting
        }

        #endregion

        #region Public Methods

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    return true;
                }

                // Get WebSocket URL dynamically from Settings
                string webSocketUrl = SettingsPage.GetWebSocketUrl();
                string deviceId = SettingsPage.GetDeviceID();
                
                // Add device_id query parameter
                string fullUrl = $"{webSocketUrl}?device_id={Uri.EscapeDataString(deviceId)}";
                
                System.Diagnostics.Debug.WriteLine($"[AudioService] Connecting to: {fullUrl}");

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                await _webSocket.ConnectAsync(
                    new Uri(fullUrl),
                    _cancellationTokenSource.Token
                );

                _isConnected = true;
                Log.Info(TAG, $"✅ WebSocket CONNECTED to: {fullUrl}");
                OnConnectionStatusChanged(true, "Connected");

                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Log.Error(TAG, $"❌ WebSocket connection FAILED: {ex.Message}");
                OnConnectionStatusChanged(false, $"Failed: {ex.Message}");
                OnError($"Connection failed: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> StartStreamingAsync()
        {
            try
            {
                if (!_isConnected)
                {
                    var connected = await ConnectAsync();
                    if (!connected) return false;
                }

                var hasPermission = await CheckMicrophonePermission();
                if (!hasPermission)
                {
                    OnError("Microphone permission denied", null);
                    return false;
                }

                if (_isStreaming) return true;

                // Tính buffer size tối thiểu
                int minBufferSize = AudioRecord.GetMinBufferSize(
                    SAMPLE_RATE,
                    CHANNEL_CONFIG,
                    AUDIO_FORMAT
                );

                // AudioRecord.GetMinBufferSize returns negative values on error
                if (minBufferSize < 0)
                {
                    OnError($"Failed to get minimum buffer size: {minBufferSize}", null);
                    return false;
                }

                // Sử dụng buffer lớn hơn để tránh buffer overflow
                int bufferSize = Math.Max(minBufferSize * 2, BUFFER_SIZE);

                // Khởi tạo AudioRecord
                _audioRecord = new AudioRecord(
                    AudioSource.Mic,
                    SAMPLE_RATE,
                    CHANNEL_CONFIG,
                    AUDIO_FORMAT,
                    bufferSize
                );

                if (_audioRecord.State != State.Initialized)
                {
                    OnError("AudioRecord initialization failed", null);
                    return false;
                }

                // Bắt đầu recording
                _audioRecord.StartRecording();
                _isStreaming = true;
                Log.Info(TAG, $"🎤 Audio recording STARTED - BufferSize={bufferSize}, MinBuffer={minBufferSize}");

                // Bắt đầu streaming loop
                _ = Task.Run(() => StreamAudioDataAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                _isStreaming = false;
                OnError($"Failed to start streaming: {ex.Message}", ex);
                return false;
            }
        }

        public async Task StopStreamingAsync()
        {
            System.Diagnostics.Debug.WriteLine("[AudioService] StopStreamingAsync called");

            if (!await _startStopLock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                System.Diagnostics.Debug.WriteLine("[AudioService] StopStreamingAsync: lock timeout, already stopping");
                return;
            }

            try
            {
                // 1. Set flags FIRST to stop loops immediately
                _isStreaming = false;
                _isConnected = false;
                
                // 2. Cancel all running tasks to unblock any awaits
                _cancellationTokenSource?.Cancel();
                
                // 3. Notify UI immediately (don't wait for cleanup)
                OnConnectionStatusChanged(false, "Stopping...");
                
                // 4. Run cleanup in background with timeout to not block UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Stop AudioRecord with timeout
                        var audioCleanupTask = Task.Run(() =>
                        {
                            try
                            {
                                if (_audioRecord != null)
                                {
                                    if (_audioRecord.RecordingState == RecordState.Recording)
                                    {
                                        _audioRecord.Stop();
                                    }
                                    _audioRecord.Release();
                                    _audioRecord.Dispose();
                                    _audioRecord = null;
                                    System.Diagnostics.Debug.WriteLine("[AudioService] AudioRecord stopped and disposed");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AudioService] AudioRecord cleanup error: {ex.Message}");
                            }
                        });
                        
                        // Wait max 2 seconds for audio cleanup
                        await Task.WhenAny(audioCleanupTask, Task.Delay(2000));
                        
                        // Close WebSocket with timeout
                        if (_webSocket != null)
                        {
                            try
                            {
                                if (_webSocket.State == WebSocketState.Open)
                                {
                                    using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                                    await _webSocket.CloseAsync(
                                        WebSocketCloseStatus.NormalClosure,
                                        "Client closing",
                                        closeTimeout.Token
                                    );
                                    System.Diagnostics.Debug.WriteLine("[AudioService] WebSocket closed gracefully");
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                System.Diagnostics.Debug.WriteLine("[AudioService] WebSocket close timed out, forcing abort");
                                _webSocket?.Abort();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AudioService] WebSocket close error: {ex.Message}");
                                _webSocket?.Abort();
                            }
                            finally
                            {
                                _webSocket?.Dispose();
                                _webSocket = null;
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[AudioService] Cleanup complete");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioService] Background cleanup error: {ex.Message}");
                    }
                });
                
                // 5. Final status update
                OnConnectionStatusChanged(false, "Disconnected");
                System.Diagnostics.Debug.WriteLine("[AudioService] StopStreamingAsync completed (cleanup running in background)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] StopStreamingAsync error: {ex.Message}");
                OnError($"Error stopping: {ex.Message}", ex);
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        #endregion

        #region Private Methods

        private async Task<bool> CheckMicrophonePermission()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                }
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                OnError($"Permission check failed: {ex.Message}", ex);
                return false;
            }
        }

        private async Task StreamAudioDataAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            int consecutiveErrors = 0;
            const int MAX_CONSECUTIVE_ERRORS = 10;

            try
            {
                Log.Info(TAG, $"🔄 Audio streaming loop started - Buffer: {BUFFER_SIZE}, Rate: {SAMPLE_RATE}Hz");
                int chunksSent = 0;
                long totalBytesSent = 0;
                
                while (_isStreaming && !cancellationToken.IsCancellationRequested)
                {
                    if (_audioRecord?.RecordingState != RecordState.Recording)
                    {
                        System.Diagnostics.Debug.WriteLine("[AudioService] AudioRecord not recording, stopping...");
                        break;
                    }

                    try
                    {
                        // Đọc audio data trực tiếp từ microphone
                        int bytesRead = await _audioRecord.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead > 0)
                        {
                            // Validate audio data
                            if (bytesRead % BYTES_PER_SAMPLE != 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AudioService] Warning: Unaligned audio data {bytesRead} bytes");
                                // Align to sample boundary
                                bytesRead = (bytesRead / BYTES_PER_SAMPLE) * BYTES_PER_SAMPLE;
                            }
                            
                            // Check audio energy level - skip silence to save bandwidth & API calls
                            bool isSilence = true;
                            double energy = 0;
                            int sampleCount = bytesRead / BYTES_PER_SAMPLE;
                            for (int i = 0; i < bytesRead; i += BYTES_PER_SAMPLE)
                            {
                                short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                                energy += Math.Abs(sample);
                            }
                            if (sampleCount > 0)
                            {
                                energy /= sampleCount;
                            }
                            // Threshold ~150 out of 32768 - filters out background noise/silence
                            isSilence = energy < 150;

                            if (isSilence)
                            {
                                // Skip sending silence - saves Deepgram API calls and bandwidth
                                continue;
                            }

                            // Gửi lên WebSocket (only non-silent audio)
                            if (_webSocket?.State == WebSocketState.Open)
                            {
                                var segment = new ArraySegment<byte>(buffer, 0, bytesRead);
                                await _webSocket.SendAsync(
                                    segment,
                                    WebSocketMessageType.Binary,
                                    endOfMessage: true,
                                    cancellationToken
                                );
                                consecutiveErrors = 0; // Reset error counter on success
                                chunksSent++;
                                totalBytesSent += bytesRead;
                                // Log every 50 chunks (~12 seconds at 8KB/250ms)
                                if (chunksSent % 50 == 0)
                                {
                                    Log.Info(TAG, $"📡 Streaming: {chunksSent} chunks sent ({totalBytesSent / 1024}KB total)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[AudioService] WebSocket not open, attempting reconnect...");
                                await ReconnectAsync();
                            }
                        }
                        else if (bytesRead < 0)
                        {
                            // Error occurred
                            consecutiveErrors++;
                            OnError($"AudioRecord read error: {bytesRead} (attempt {consecutiveErrors}/{MAX_CONSECUTIVE_ERRORS})", null);
                            
                            if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                            {
                                OnError("Too many consecutive read errors, stopping stream", null);
                                break;
                            }
                            
                            await Task.Delay(100, cancellationToken); // Brief delay before retry
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Normal cancellation
                    }
                    catch (Exception readEx)
                    {
                        consecutiveErrors++;
                        OnError($"Read cycle error: {readEx.Message} (attempt {consecutiveErrors}/{MAX_CONSECUTIVE_ERRORS})", readEx);
                        
                        if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                        {
                            break;
                        }
                        
                        await Task.Delay(100, cancellationToken);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[AudioService] Audio stream ended");
            }
            catch (Exception ex)
            {
                OnError($"Streaming error: {ex.Message}", ex);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            // Buffer large enough for the receive chunk; messages larger than this
            // are assembled across multiple ReceiveAsync calls via messageBuffer.
            var buffer = new byte[8192];
            // Accumulator for multi-frame messages (result.EndOfMessage == false)
            var messageBuffer = new System.IO.MemoryStream(8192);

            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (_webSocket?.State != WebSocketState.Open) break;

                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _isConnected = false;
                        OnConnectionStatusChanged(false, "Server closed");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Accumulate this frame's data
                        messageBuffer.Write(buffer, 0, result.Count);

                        if (result.EndOfMessage)
                        {
                            // Full message assembled — process it
                            var message = System.Text.Encoding.UTF8.GetString(
                                messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
                            messageBuffer.SetLength(0); // Reset for next message
                            ProcessServerMessage(message);
                        }
                        // else: more frames coming — keep accumulating
                    }
                }
            }
            catch (Exception ex)
            {
                OnError($"Receive error: {ex.Message}", ex);
            }
            finally
            {
                messageBuffer.Dispose();
            }
        }

        private void ProcessServerMessage(string message)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) ||
                    typeElement.GetString() != "alert")
                    return; // Not an alert message, ignore

                // Validate required fields before accessing
                if (!root.TryGetProperty("alert_type", out var alertTypeEl) ||
                    alertTypeEl.ValueKind == JsonValueKind.Null)
                {
                    Log.Error(TAG, $"[AudioService] Alert missing alert_type field: {message}");
                    OnError("Invalid alert: missing alert_type", null);
                    return;
                }

                if (!root.TryGetProperty("confidence", out var confidenceEl) ||
                    confidenceEl.ValueKind == JsonValueKind.Null)
                {
                    Log.Error(TAG, "[AudioService] Alert missing confidence field");
                    OnError("Invalid alert: missing confidence", null);
                    return;
                }

                // Parse backend Unix timestamp -> local DateTime
                DateTime alertTimestamp = DateTime.Now;
                if (root.TryGetProperty("timestamp", out var tsEl) &&
                    tsEl.ValueKind == JsonValueKind.Number)
                {
                    long unixSec = tsEl.GetInt64();
                    if (unixSec > 0)
                    {
                        try { alertTimestamp = DateTimeOffset.FromUnixTimeSeconds(unixSec).LocalDateTime; }
                        catch { /* Overflow/invalid - keep DateTime.Now */ }
                    }
                }

                // Parse keywords safely
                string[] keywords = Array.Empty<string>();
                if (root.TryGetProperty("keywords", out var kEl) &&
                    kEl.ValueKind == JsonValueKind.Array)
                {
                    try { keywords = JsonSerializer.Deserialize<string[]>(kEl.GetRawText()) ?? Array.Empty<string>(); }
                    catch (Exception kEx) { Log.Warn(TAG, $"[AudioService] Could not parse keywords: {kEx.Message}"); }
                }

                var alertData = new AlertData
                {
                    AlertType    = alertTypeEl.GetString() ?? "UNKNOWN",
                    Confidence   = confidenceEl.GetDouble(),
                    Transcript   = root.TryGetProperty("transcript", out var tr) && tr.ValueKind != JsonValueKind.Null ? tr.GetString() ?? "" : "",
                    Keywords     = keywords,
                    DeepfakeScore = root.TryGetProperty("deepfake_score", out var df) && df.ValueKind == JsonValueKind.Number ? df.GetInt32() : 0,
                    Message      = root.TryGetProperty("message", out var mg) && mg.ValueKind != JsonValueKind.Null ? mg.GetString() ?? "" : "",
                    Timestamp    = alertTimestamp
                };

                Log.Warn(TAG, $"ALERT: {alertData.AlertType} confidence={alertData.Confidence:F2} at {alertData.Timestamp:HH:mm:ss}");
                OnAlertReceived(alertData);
            }
            catch (System.Text.Json.JsonException ex)
            {
                Log.Error(TAG, $"[AudioService] Failed to parse alert JSON: {ex.Message}");
                OnError($"Alert JSON parse error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                OnError($"Message processing error: {ex.Message}", ex);
            }
        }

        private async Task ReconnectAsync()
        {
            try
            {
                if (!_isConnected)
                {
                    Log.Info(TAG, "[AudioService] Attempting reconnect...");
                    await ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"[AudioService] Reconnect failed: {ex.Message}");
            }
        }

        #endregion

        #region Event Helpers

        protected virtual void OnAlertReceived(AlertData alert)
        {
            AlertReceived?.Invoke(this, new AlertEventArgs(alert));
        }

        protected virtual void OnError(string message, Exception? ex)
        {
            Log.Error(TAG, $"[AudioService] Error: {message}");
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(message, ex));
        }

        protected virtual void OnConnectionStatusChanged(bool isConnected, string status)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(isConnected, status));
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                try { _audioRecord?.Release(); } catch { }
                try { _audioRecord?.Dispose(); } catch { }
                _audioRecord = null;

                try { _webSocket?.Dispose(); } catch { }
                _webSocket = null;

                _startStopLock.Dispose();
            }
            _disposed = true;
        }

        #endregion
    }

    // ─── Event Arg Types ────────────────────────────────────────────────────

    public class AlertEventArgs : EventArgs
    {
        public AlertData Alert { get; }
        public AlertEventArgs(AlertData alert) => Alert = alert;
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Status { get; }
        public ConnectionStatusEventArgs(bool isConnected, string status)
        {
            IsConnected = isConnected;
            Status = status;
        }
    }
}
