using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Service for fetching call history from the backend API
    /// </summary>
    public class HistoryService
    {
        private readonly HttpClient _httpClient;

        public HistoryService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Get call history for a specific device
        /// </summary>
        /// <param name="deviceId">Device ID to filter by (optional)</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <param name="fraudOnly">If true, return only fraudulent calls</param>
        /// <returns>List of call logs</returns>
        public async Task<List<CallLog>> GetHistoryAsync(
            string deviceId = null, 
            int limit = 20, 
            bool fraudOnly = false)
        {
            try
            {
                // Build query string
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(deviceId))
                {
                    queryParams.Add($"device_id={Uri.EscapeDataString(deviceId)}");
                }
                
                queryParams.Add($"limit={limit}");
                
                if (fraudOnly)
                {
                    queryParams.Add("fraud_only=true");
                }

                var queryString = string.Join("&", queryParams);
                
                // Get base URL dynamically from Settings
                string baseUrl = SettingsPage.GetAPIBaseUrl();
                var url = $"{baseUrl}/api/history?{queryString}";

                System.Diagnostics.Debug.WriteLine($"[HistoryService] Fetching: {url}");

                // Make HTTP request
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Parse JSON response
                var historyResponse = await response.Content.ReadFromJsonAsync<HistoryResponse>();

                if (historyResponse?.Success == true && historyResponse.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[HistoryService] Retrieved {historyResponse.Data.Length} records");
                    return new List<CallLog>(historyResponse.Data);
                }

                System.Diagnostics.Debug.WriteLine("[HistoryService] No data returned");
                return new List<CallLog>();
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] HTTP Error: {ex.Message}");
                throw new Exception($"Cannot connect to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] Error: {ex.Message}");
                throw new Exception($"Error loading history: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get only fraudulent calls
        /// </summary>
        public async Task<List<CallLog>> GetFraudCallsOnlyAsync(string deviceId = null, int limit = 20)
        {
            return await GetHistoryAsync(deviceId, limit, fraudOnly: true);
        }

        /// <summary>
        /// Test connection to the API
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                string baseUrl = SettingsPage.GetAPIBaseUrl();
                var url = $"{baseUrl}/health";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
