using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FraudGuardAI.Helpers
{
    /// <summary>
    /// Manages server connection with retry logic and connection testing
    /// </summary>
    public class ConnectionManager
    {
        private const int MAX_RETRIES = 3;
        private const int TIMEOUT_SECONDS = 10;
        private readonly HttpClient _httpClient;

        public ConnectionManager()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
            };
        }

        public class ConnectionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public Exception Error { get; set; }

            public static ConnectionResult Successful(string message = "Connected successfully")
            {
                return new ConnectionResult { Success = true, Message = message };
            }

            public static ConnectionResult Failed(string message, Exception error = null)
            {
                return new ConnectionResult { Success = false, Message = message, Error = error };
            }
        }

        /// <summary>
        /// Test HTTP health endpoint
        /// </summary>
        public async Task<ConnectionResult> TestHTTPConnection(string serverIP, int port = 8080)
        {
            try
            {
                var url = $"http://{serverIP}:{port}/health";
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SECONDS));
                var response = await _httpClient.GetAsync(url, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return ConnectionResult.Successful($"Server is online ({response.StatusCode})");
                }
                else
                {
                    return ConnectionResult.Failed($"Server returned {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                return ConnectionResult.Failed($"Connection timeout after {TIMEOUT_SECONDS}s");
            }
            catch (Exception ex)
            {
                return ConnectionResult.Failed("Cannot reach server", ex);
            }
        }

        /// <summary>
        /// Test WebSocket connection
        /// </summary>
        public async Task<ConnectionResult> TestWebSocketConnection(string serverIP, int port = 8080, string deviceId = "test_device")
        {
            ClientWebSocket ws = null;
            try
            {
                ws = new ClientWebSocket();
                var url = $"ws://{serverIP}:{port}/ws?device_id={deviceId}";
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SECONDS));
                await ws.ConnectAsync(new Uri(url), cts.Token);

                if (ws.State == WebSocketState.Open)
                {
                    // Send test message
                    var testMsg = Encoding.UTF8.GetBytes("{\"type\":\"test\"}");
                    await ws.SendAsync(
                        new ArraySegment<byte>(testMsg),
                        WebSocketMessageType.Text,
                        true,
                        cts.Token
                    );

                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                    return ConnectionResult.Successful("WebSocket connection verified");
                }
                else
                {
                    return ConnectionResult.Failed($"WebSocket state: {ws.State}");
                }
            }
            catch (TaskCanceledException)
            {
                return ConnectionResult.Failed("WebSocket timeout");
            }
            catch (Exception ex)
            {
                return ConnectionResult.Failed("WebSocket connection failed", ex);
            }
            finally
            {
                ws?.Dispose();
            }
        }

        /// <summary>
        /// Comprehensive connection test
        /// </summary>
        public async Task<ConnectionResult> TestConnection(string serverIP, int port = 8080)
        {
            // Test 1: HTTP Health check
            var httpResult = await TestHTTPConnection(serverIP, port);
            if (!httpResult.Success)
            {
                return httpResult;
            }

            // Test 2: WebSocket connection
            var wsResult = await TestWebSocketConnection(serverIP, port);
            if (!wsResult.Success)
            {
                return ConnectionResult.Failed(
                    "HTTP works but WebSocket failed. Check server configuration.",
                    wsResult.Error
                );
            }

            return ConnectionResult.Successful("✓ All connection tests passed!");
        }

        /// <summary>
        /// Connect with automatic retry and exponential backoff
        /// </summary>
        public async Task<ConnectionResult> ConnectWithRetry(
            Func<Task<bool>> connectAction,
            Action<int, int> onRetry = null)
        {
            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    var success = await connectAction();
                    if (success)
                    {
                        return ConnectionResult.Successful();
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == MAX_RETRIES)
                    {
                        return ConnectionResult.Failed($"Failed after {MAX_RETRIES} attempts", ex);
                    }

                    // Notify retry attempt
                    onRetry?.Invoke(attempt, MAX_RETRIES);

                    // Exponential backoff: 1s, 2s, 4s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }

            return ConnectionResult.Failed("Max retries exceeded");
        }

        /// <summary>
        /// Check internet connectivity
        /// </summary>
        public bool IsInternetAvailable()
        {
            var connectivity = Microsoft.Maui.Networking.Connectivity.NetworkAccess;
            return connectivity == Microsoft.Maui.Networking.NetworkAccess.Internet;
        }

        /// <summary>
        /// Get connection quality info
        /// </summary>
        public string GetConnectionInfo()
        {
            var profiles = Microsoft.Maui.Networking.Connectivity.ConnectionProfiles;
            var access = Microsoft.Maui.Networking.Connectivity.NetworkAccess;

            return $"Network: {access}, Profiles: {string.Join(", ", profiles)}";
        }
    }
}
