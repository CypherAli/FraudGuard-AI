using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Plugin.AudioRecorder;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Service để stream audio realtime lên WebSocket Server
    /// Cấu hình: 16000Hz, Mono, PCM 16-bit (khớp với backend Deepgram)
    /// </summary>
    public class AudioStreamingService : IDisposable
    {
        #region Fields & Properties

        private ClientWebSocket _webSocket;
        private AudioRecorderService _audioRecorder;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isStreaming;
        private bool _isConnected;

        // Cấu hình Audio (BẮT BUỘC KHỚP BACKEND)
        private const int SAMPLE_RATE = 16000;  // 16kHz cho Deepgram
        private const int CHANNELS = 1;          // Mono
        private const int BITS_PER_SAMPLE = 16;  // PCM 16-bit
        private const int BUFFER_SIZE = 4096;    // Chunk size để gửi

        // WebSocket URL (sử dụng 10.0.2.2 cho Android Emulator)
        private string _webSocketUrl;

        /// <summary>
        /// Event được kích hoạt khi nhận được cảnh báo từ Server
        /// </summary>
        public event EventHandler<AlertEventArgs> AlertReceived;

        /// <summary>
        /// Event được kích hoạt khi có lỗi xảy ra
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Event được kích hoạt khi trạng thái kết nối thay đổi
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        #endregion

        #region Constructor

        public AudioStreamingService(string webSocketUrl = "ws://10.0.2.2:8080/ws")
        {
            _webSocketUrl = webSocketUrl;
            _audioRecorder = new AudioRecorderService
            {
                StopRecordingAfterTimeout = false,
                StopRecordingOnSilence = false,
                AudioSilenceTimeout = TimeSpan.FromSeconds(2),
                TotalAudioTimeout = TimeSpan.MaxValue,
                
                // Cấu hình Audio Settings
                PreferredSampleRate = SAMPLE_RATE
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kết nối tới WebSocket Server
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    Console.WriteLine("Already connected to WebSocket");
                    return true;
                }

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                Console.WriteLine($"Connecting to WebSocket: {_webSocketUrl}");
                
                await _webSocket.ConnectAsync(
                    new Uri(_webSocketUrl), 
                    _cancellationTokenSource.Token
                );

                _isConnected = true;
                OnConnectionStatusChanged(true, "Connected successfully");

                // Bắt đầu lắng nghe message từ Server
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                Console.WriteLine("WebSocket connected successfully");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectionStatusChanged(false, $"Connection failed: {ex.Message}");
                OnError($"Failed to connect: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Bắt đầu streaming audio lên Server
        /// </summary>
        public async Task<bool> StartStreamingAsync()
        {
            try
            {
                // Kiểm tra kết nối
                if (!_isConnected)
                {
                    var connected = await ConnectAsync();
                    if (!connected)
                    {
                        OnError("Cannot start streaming: Not connected to server", null);
                        return false;
                    }
                }

                // Kiểm tra quyền Microphone
                var hasPermission = await CheckAndRequestMicrophonePermission();
                if (!hasPermission)
                {
                    OnError("Microphone permission denied", null);
                    return false;
                }

                if (_isStreaming)
                {
                    Console.WriteLine("Already streaming");
                    return true;
                }

                _isStreaming = true;
                Console.WriteLine("Starting audio streaming...");

                // Bắt đầu ghi âm - unwrap Task<Task<string>>
                var audioFilePath = await await _audioRecorder.StartRecording();
                
                if (string.IsNullOrEmpty(audioFilePath))
                {
                    _isStreaming = false;
                    OnError("Failed to start audio recording", null);
                    return false;
                }

                // Bắt đầu stream audio trong background task
                _ = Task.Run(() => StreamAudioDataAsync(_cancellationTokenSource.Token));

                Console.WriteLine("Audio streaming started successfully");
                return true;
            }
            catch (Exception ex)
            {
                _isStreaming = false;
                OnError($"Failed to start streaming: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Dừng streaming và đóng kết nối
        /// </summary>
        public async Task StopStreamingAsync()
        {
            try
            {
                Console.WriteLine("Stopping audio streaming...");

                _isStreaming = false;

                // Dừng ghi âm
                if (_audioRecorder != null)
                {
                    await _audioRecorder.StopRecording();
                }

                // Hủy các task đang chạy
                _cancellationTokenSource?.Cancel();

                // Đóng WebSocket connection
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client closing connection",
                        CancellationToken.None
                    );
                }

                _isConnected = false;
                OnConnectionStatusChanged(false, "Disconnected");

                Console.WriteLine("Audio streaming stopped successfully");
            }
            catch (Exception ex)
            {
                OnError($"Error stopping streaming: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Thay đổi WebSocket URL (ví dụ: chuyển từ Emulator sang LAN IP)
        /// </summary>
        public void SetWebSocketUrl(string url)
        {
            if (_isConnected || _isStreaming)
            {
                throw new InvalidOperationException("Cannot change URL while connected or streaming");
            }
            _webSocketUrl = url;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kiểm tra và xin quyền Microphone
        /// </summary>
        private async Task<bool> CheckAndRequestMicrophonePermission()
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

        /// <summary>
        /// Stream audio data lên WebSocket Server
        /// Đọc audio stream và gửi theo chunks
        /// </summary>
        private async Task StreamAudioDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Audio streaming loop started");

                // Lấy audio stream từ recorder
                var audioStream = _audioRecorder.GetAudioFileStream();
                
                if (audioStream == null)
                {
                    OnError("Failed to get audio stream", null);
                    return;
                }

                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;

                while (_isStreaming && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Đọc audio data
                        bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead > 0)
                        {
                            // Gửi audio chunk lên WebSocket dưới dạng Binary
                            if (_webSocket?.State == WebSocketState.Open)
                            {
                                var segment = new ArraySegment<byte>(buffer, 0, bytesRead);
                                await _webSocket.SendAsync(
                                    segment,
                                    WebSocketMessageType.Binary,
                                    endOfMessage: true,
                                    cancellationToken
                                );

                                Console.WriteLine($"Sent {bytesRead} bytes to server");
                            }
                            else
                            {
                                // WebSocket bị đóng, thử reconnect
                                Console.WriteLine("WebSocket disconnected, attempting to reconnect...");
                                await ReconnectAsync();
                            }
                        }
                        else
                        {
                            // Không có data, đợi một chút
                            await Task.Delay(10, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Task bị cancel, thoát loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnError($"Error streaming audio: {ex.Message}", ex);
                        await Task.Delay(100, cancellationToken); // Đợi trước khi retry
                    }
                }

                Console.WriteLine("Audio streaming loop ended");
            }
            catch (Exception ex)
            {
                OnError($"Fatal error in streaming loop: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lắng nghe messages từ WebSocket Server
        /// Xử lý JSON alerts từ backend
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (_webSocket?.State != WebSocketState.Open)
                    {
                        break;
                    }

                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server closed connection");
                        _isConnected = false;
                        OnConnectionStatusChanged(false, "Server closed connection");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received message: {message}");

                        // Parse JSON và kiểm tra xem có phải alert không
                        ProcessServerMessage(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Receive loop cancelled");
            }
            catch (Exception ex)
            {
                OnError($"Error receiving messages: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xử lý message từ Server (JSON alerts)
        /// </summary>
        private void ProcessServerMessage(string message)
        {
            try
            {
                // Parse JSON
                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;

                // Kiểm tra xem có phải alert không
                if (root.TryGetProperty("type", out var typeElement) && 
                    typeElement.GetString() == "alert")
                {
                    // Extract alert data
                    var alertType = root.GetProperty("alert_type").GetString();
                    var confidence = root.GetProperty("confidence").GetDouble();
                    var transcript = root.TryGetProperty("transcript", out var transcriptElement) 
                        ? transcriptElement.GetString() 
                        : "";
                    var keywords = root.TryGetProperty("keywords", out var keywordsElement)
                        ? JsonSerializer.Deserialize<string[]>(keywordsElement.GetRawText())
                        : Array.Empty<string>();

                    // Kích hoạt event
                    OnAlertReceived(new AlertData
                    {
                        AlertType = alertType,
                        Confidence = confidence,
                        Transcript = transcript,
                        Keywords = keywords,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                OnError($"Error processing server message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Thử reconnect tới WebSocket Server
        /// </summary>
        private async Task ReconnectAsync()
        {
            try
            {
                Console.WriteLine("Attempting to reconnect...");
                
                // Đóng connection cũ
                if (_webSocket != null)
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }

                _isConnected = false;

                // Đợi 1 giây trước khi reconnect
                await Task.Delay(1000);

                // Thử connect lại
                await ConnectAsync();
            }
            catch (Exception ex)
            {
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
            Console.WriteLine($"ERROR: {message}");
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
            _webSocket?.Dispose();
            // _audioRecorder doesn't have Dispose method
        }

        #endregion
    }

    #region Event Args Classes

    /// <summary>
    /// Alert data từ Server
    /// </summary>
    public class AlertData
    {
        public string AlertType { get; set; }
        public double Confidence { get; set; }
        public string Transcript { get; set; }
        public string[] Keywords { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AlertEventArgs : EventArgs
    {
        public AlertData Alert { get; }

        public AlertEventArgs(AlertData alert)
        {
            Alert = alert;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }

        public ErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public ConnectionStatusEventArgs(bool isConnected, string message)
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    #endregion
}
