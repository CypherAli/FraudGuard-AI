using System.Net.Http.Headers;
using System.Net.Http.Json;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Fetches call-analysis history from the backend REST API.
    /// All requests include the bearer token stored in SecureStorage.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        // Shared client — avoids socket exhaustion from repeated instantiation
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly IAppSettings _settings;
        private readonly SecureStorageService _secureStorage;

        public HistoryService(IAppSettings settings, SecureStorageService secureStorage)
        {
            _settings = settings;
            _secureStorage = secureStorage;
        }

        // Builds an authenticated GET request
        private async Task<HttpRequestMessage> AuthGet(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var token = await _secureStorage.GetAuthTokenAsync();
            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return req;
        }

        /// <inheritdoc/>
        public async Task<List<CallLog>> GetHistoryAsync(
            string? deviceId = null,
            int limit = 20,
            bool fraudOnly = false)
        {
            var query = new List<string> { $"limit={limit}" };

            if (!string.IsNullOrEmpty(deviceId))
                query.Add($"device_id={Uri.EscapeDataString(deviceId)}");

            if (fraudOnly)
                query.Add("fraud_only=true");

            var url = $"{_settings.GetApiBaseUrl()}/api/history?{string.Join("&", query)}";
            System.Diagnostics.Debug.WriteLine($"[HistoryService] GET {url}");

            var response = await _http.SendAsync(await AuthGet(url));
            response.EnsureSuccessStatusCode();

            // Fix 42: ReadFromJsonAsync throws JsonException when the response body is
            // not valid JSON (e.g. reverse-proxy 502 HTML error page delivered with 200).
            try
            {
                var result = await response.Content.ReadFromJsonAsync<HistoryResponse>();
                return result?.Success == true && result.Data != null
                    ? new List<CallLog>(result.Data)
                    : new List<CallLog>();
            }
            catch (System.Text.Json.JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] JSON parse error: {ex.Message}");
                return new List<CallLog>();
            }
        }

        /// <inheritdoc/>
        public async Task<CallLog?> GetCallDetailAsync(int callId)
        {
            var url = $"{_settings.GetApiBaseUrl()}/api/call/{callId}";
            System.Diagnostics.Debug.WriteLine($"[HistoryService] GET {url}");

            var response = await _http.SendAsync(await AuthGet(url));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CallDetailResponse>();
            return result?.Data;
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // /health is public — no auth needed
                var url = $"{_settings.GetApiBaseUrl()}/health";
                var response = await _http.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
