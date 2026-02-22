using System.Net.Http.Json;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Fetches call-analysis history from the backend REST API.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        // Shared client — avoids socket exhaustion from repeated instantiation
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly IAppSettings _settings;

        public HistoryService(IAppSettings settings)
        {
            _settings = settings;
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

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HistoryResponse>();
            return result?.Success == true && result.Data != null
                ? new List<CallLog>(result.Data)
                : new List<CallLog>();
        }

        /// <inheritdoc/>
        public async Task<CallLog?> GetCallDetailAsync(int callId)
        {
            var url = $"{_settings.GetApiBaseUrl()}/api/call/{callId}";

            System.Diagnostics.Debug.WriteLine($"[HistoryService] GET {url}");

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CallDetailResponse>();
            return result?.Data;
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
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
