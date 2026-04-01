using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Local cache of blacklisted phone numbers for fast lookup during incoming calls.
    /// Syncs with the backend periodically.
    /// </summary>
    public sealed class BlacklistCacheService
    {
        private static readonly Lazy<BlacklistCacheService> _instance =
            new(() => new BlacklistCacheService());
        public static BlacklistCacheService Instance => _instance.Value;

        private const string CACHE_KEY = "blacklist_phone_cache";
        private const string LAST_SYNC_KEY = "blacklist_last_sync";

        private HashSet<string> _cachedNumbers = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();
        private volatile bool _loaded = false;

        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private BlacklistCacheService()
        {
            // Fire-and-forget — callers that need a fully loaded cache should
            // await InitializeAsync() before the first IsBlacklisted() call.
            _ = LoadCacheAsync();
        }

        /// <summary>
        /// Awaitable init: blocks until the local SecureStorage cache is loaded.
        /// Call this from MainPage.OnAppearing() to eliminate the 200ms false-negative window
        /// where IsBlacklisted() returns false before LoadCacheAsync completes.
        /// </summary>
        public Task InitializeAsync() => LoadCacheAsync();

        /// <summary>
        /// Check if a phone number is blacklisted (fast local check)
        /// </summary>
        public bool IsBlacklisted(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber == "Unknown") return false;
            string normalized = NormalizePhone(phoneNumber);
            lock (_lock)
            {
                return _cachedNumbers.Contains(normalized) || _cachedNumbers.Contains(phoneNumber);
            }
        }

        /// <summary>
        /// Sync blacklist from backend server
        /// </summary>
        public async Task SyncFromServerAsync()
        {
            try
            {
                string baseUrl = new AppSettings().GetApiBaseUrl();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/blacklist");

                // Attach session token if available
                var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<BlacklistApiResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in result.Data)
                    {
                        if (!string.IsNullOrEmpty(entry.PhoneNumber))
                        {
                            numbers.Add(entry.PhoneNumber);
                            numbers.Add(NormalizePhone(entry.PhoneNumber));
                        }
                    }

                    lock (_lock)
                    {
                        _cachedNumbers = numbers;
                    }

                    // Persist to local storage
                    var serialized = JsonSerializer.Serialize(numbers.ToList());
                    await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync(CACHE_KEY, serialized);
                    Preferences.Set(LAST_SYNC_KEY, DateTime.UtcNow.ToString("O"));

                    System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Synced {numbers.Count} numbers from server");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a phone number to local cache immediately (for instant blocking after report).
        /// Also persists to storage so it survives app restart.
        /// </summary>
        public void AddToLocalCache(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return;
            string normalized = NormalizePhone(phoneNumber);
            lock (_lock)
            {
                _cachedNumbers.Add(phoneNumber);
                _cachedNumbers.Add(normalized);
            }
            // Persist in background
            _ = PersistCacheAsync();
            System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Added to local cache: {phoneNumber} / {normalized}");
        }

        private async Task PersistCacheAsync()
        {
            try
            {
                List<string> snapshot;
                lock (_lock) { snapshot = _cachedNumbers.ToList(); }
                var serialized = JsonSerializer.Serialize(snapshot);
                await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync(CACHE_KEY, serialized);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Persist error: {ex.Message}");
            }
        }

        public int Count
        {
            get { lock (_lock) { return _cachedNumbers.Count; } }
        }

        public DateTime? LastSyncTime
        {
            get
            {
                var str = Preferences.Get(LAST_SYNC_KEY, "");
                return DateTime.TryParse(str, out var dt) ? dt : null;
            }
        }

        private async Task LoadCacheAsync()
        {
            if (_loaded) return;
            try
            {
                var json = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync(CACHE_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                    {
                        lock (_lock)
                        {
                            _cachedNumbers = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                        }
                        System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Loaded {list.Count} cached numbers");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BlacklistCache] Load error: {ex.Message}");
            }
            _loaded = true;
        }

        private static string NormalizePhone(string phone)
        {
            // Remove spaces, dashes, parentheses
            var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
            // Convert +84 → 0 for Vietnam
            if (digits.StartsWith("+84"))
                digits = "0" + digits.Substring(3);
            return digits;
        }

        // Response models for /api/blacklist
        private class BlacklistApiResponse
        {
            public bool Success { get; set; }
            public List<BlacklistEntry>? Data { get; set; }
        }

        private class BlacklistEntry
        {
            public string? PhoneNumber { get; set; }
            public string? Reason { get; set; }
            public double ConfidenceScore { get; set; }
            public int ReportedCount { get; set; }
        }
    }
}
