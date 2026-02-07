using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
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
        private bool _isStreaming;
        private bool _isConnected;

        // Cấu hình Audio (khớp với backend Deepgram)
        private const int SAMPLE_RATE = 16000;
        private const ChannelIn CHANNEL_CONFIG = ChannelIn.Mono;
        private const Android.Media.Encoding AUDIO_FORMAT = Android.Media.Encoding.Pcm16bit;
        private const int BUFFER_SIZE = 8192; // Increased from 4096 to reduce fragmentation
        private const int BYTES_PER_SAMPLE = 2; // 16-bit = 2 bytes

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
                OnConnectionStatusChanged(true, "Connected");

                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
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
                System.Diagnostics.Debug.WriteLine($"[AudioService] Starting audio stream - Buffer: {BUFFER_SIZE}, Rate: {SAMPLE_RATE}Hz");
                
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
                            
                            // Ensure data is not silence (all zeros)
                            bool isSilence = true;
                            for (int i = 0; i < Math.Min(bytesRead, 100); i++)
                            {
                                if (buffer[i] != 0)
                                {
                                    isSilence = false;
                                    break;
                                }
                            }
                            
                            // Gửi lên WebSocket
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
            var buffer = new byte[8192];

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
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessServerMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError($"Receive error: {ex.Message}", ex);
            }
        }

        private void ProcessServerMessage(string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] ===== PROCESSING SERVER MESSAGE =====");
                System.Diagnostics.Debug.WriteLine($"[AudioService] Raw message ({message.Length} chars): {message}");
                
                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;
                
                System.Diagnostics.Debug.WriteLine($"[AudioService] JSON parsed successfully");

                // Log all properties in the JSON
                System.Diagnostics.Debug.WriteLine($"[AudioService] JSON properties:");
                foreach (var property in root.EnumerateObject())
                {
                    System.Diagnostics.Debug.WriteLine($"  - {property.Name}: {property.Value}");
                }

                if (root.TryGetProperty("type", out var typeElement) &&
                    typeElement.GetString() == "alert")
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] ✅ Alert message detected!");
                    
                    var alertData = new AlertData
                    {
                        AlertType = root.GetProperty("alert_type").GetString(),
                        Confidence = root.GetProperty("confidence").GetDouble(),
                        Transcript = root.TryGetProperty("transcript", out var t) ? t.GetString() : "",
                        Keywords = root.TryGetProperty("keywords", out var k)
                            ? JsonSerializer.Deserialize<string[]>(k.GetRawText())
                            : Array.Empty<string>(),
                        Timestamp = DateTime.Now
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"[AudioService] Alert parsed:");
                    System.Diagnostics.Debug.WriteLine($"  - AlertType: {alertData.AlertType}");
                    System.Diagnostics.Debug.WriteLine($"  - Confidence: {alertData.Confidence}");
                    System.Diagnostics.Debug.WriteLine($"  - Transcript: {alertData.Transcript}");
                    System.Diagnostics.Debug.WriteLine($"  - Keywords: {string.Join(", ", alertData.Keywords)}");
                    
                    System.Diagnostics.Debug.WriteLine($"[AudioService] 🚨 Triggering OnAlertReceived event...");
                    OnAlertReceived(alertData);
                    System.Diagnostics.Debug.WriteLine($"[AudioService] ✅ OnAlertReceived event triggered");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] ⚠️ Message is not an alert (type: {(root.TryGetProperty("type", out var t) ? t.GetString() : "missing")})");
                }
                
                System.Diagnostics.Debug.WriteLine($"[AudioService] ===== MESSAGE PROCESSING COMPLETE =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] ❌ Message processing error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AudioService] Stack trace: {ex.StackTrace}");
                OnError($"Message processing error: {ex.Message}", ex);
            }
        }

        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private static readonly Random _random = new Random();
        
        private async Task ReconnectAsync()
        {
            try
            {
                _reconnectAttempts++;
                
                if (_reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] ❌ Max reconnect attempts ({MAX_RECONNECT_ATTEMPTS}) reached");
                    OnError("Failed to reconnect after multiple attempts", null);
                    _reconnectAttempts = 0;
                    return;
                }
                
                // Exponential backoff: 1s, 2s, 4s, 8s, 16s
                int baseDelay = (int)Math.Pow(2, _reconnectAttempts - 1) * 1000;
                
                // Add jitter (0-1000ms) to prevent thundering herd
                int jitter = _random.Next(0, 1000);
                int totalDelay = baseDelay + jitter;
                
                System.Diagnostics.Debug.WriteLine($"[AudioService] 🔄 Reconnect attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}");
                System.Diagnostics.Debug.WriteLine($"[AudioService] Waiting {totalDelay}ms (base: {baseDelay}ms + jitter: {jitter}ms)");
                
                _webSocket?.Dispose();
                _webSocket = null;
                _isConnected = false;
                
                await Task.Delay(totalDelay);
                
                bool success = await ConnectAsync();
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] ✅ Reconnection successful");
                    _reconnectAttempts = 0; // Reset on success
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] ❌ Reconnect failed: {ex.Message}");
                OnError($"Reconnect failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Event Handlers

        private void OnAlertReceived(AlertData alertData)
        {
            AlertReceived?.Invoke(this, new AlertEventArgs(alertData));
        }

        private void OnError(string message, Exception exception)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(message, exception));
        }

        private void OnConnectionStatusChanged(bool isConnected, string message)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(isConnected, message));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _audioRecord?.Release();
            _audioRecord?.Dispose();
            _webSocket?.Dispose();
        }

        #endregion
    }
}
