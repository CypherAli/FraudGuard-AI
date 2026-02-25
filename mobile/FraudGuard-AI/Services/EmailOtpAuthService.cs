using System.Diagnostics;
using System.Net.Http.Json;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// OTP auth via backend API — Brevo key never touches the APK.
    /// Backend endpoints: POST /auth/send-otp  |  POST /auth/verify-otp
    /// </summary>
    public class EmailOtpAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private readonly IAppSettings _appSettings;
        private AuthenticationState _currentState;
        private string? _pendingEmail;

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public EmailOtpAuthService(SecureStorageService secureStorage, IAppSettings appSettings)
        {
            _secureStorage = secureStorage;
            _appSettings = appSettings;
            _currentState = new AuthenticationState();
        }

        // ── Response DTOs ────────────────────────────────────────────────────

        private sealed class SendOtpResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }

        private sealed class VerifyOtpResponse
        {
            public bool Success { get; set; }
            public string? Token { get; set; }
            public string? UserId { get; set; }
            public string? Email { get; set; }
            public string? Error { get; set; }
        }

        // ── IAuthenticationService ───────────────────────────────────────────

        public async Task<string> SendOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không hợp lệ");

            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (!IsValidEmail(normalizedEmail))
                throw new ArgumentException("Email không hợp lệ");

            _pendingEmail = normalizedEmail;
            await _secureStorage.SaveEmailAsync(normalizedEmail);

            var url = $"{_appSettings.GetApiBaseUrl()}/auth/send-otp";
            Debug.WriteLine($"[EmailOtpAuth] POST {url} (email={normalizedEmail})");

            HttpResponseMessage httpResp;
            try
            {
                httpResp = await _http.PostAsJsonAsync(url, new { email = normalizedEmail });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] Network error: {ex.Message}");
                throw new Exception("Không thể kết nối máy chủ. Vui lòng kiểm tra kết nối mạng.");
            }

            if (!httpResp.IsSuccessStatusCode)
            {
                var err = await httpResp.Content.ReadFromJsonAsync<SendOtpResponse>();
                var msg = err?.Error ?? "Không thể gửi mã OTP. Vui lòng thử lại.";
                throw new Exception(msg);
            }

            Debug.WriteLine($"[EmailOtpAuth] OTP sent to {normalizedEmail} via backend");

            // verificationId = email (backend keys OTP by email)
            return normalizedEmail;
        }

        public async Task<bool> VerifyOtpAsync(string verificationId, string otpCode)
        {
            // verificationId is the email returned from SendOtpAsync
            var email = verificationId;

            var url = $"{_appSettings.GetApiBaseUrl()}/auth/verify-otp";
            Debug.WriteLine($"[EmailOtpAuth] POST {url}");

            HttpResponseMessage httpResp;
            try
            {
                httpResp = await _http.PostAsJsonAsync(url, new { email, otp = otpCode });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] Network error: {ex.Message}");
                return false;
            }

            if (!httpResp.IsSuccessStatusCode)
                return false;

            var result = await httpResp.Content.ReadFromJsonAsync<VerifyOtpResponse>();
            if (result?.Success != true || string.IsNullOrEmpty(result.Token))
                return false;

            // Persist backend-issued token & user data
            var userId = result.UserId ?? GenerateFallbackUserId(email);
            var displayName = GetDisplayNameFromEmail(email);
            var tokenExpiry = DateTime.UtcNow.AddDays(30);

            await _secureStorage.SaveAuthTokenAsync(result.Token);
            await _secureStorage.SaveUserDataAsync(userId, string.Empty, displayName);
            await _secureStorage.SaveEmailAsync(email);
            await _secureStorage.SaveTokenExpiryAsync(tokenExpiry);

            var user = new User
            {
                UserId = userId,
                Email = email,
                DisplayName = displayName,
                LastLoginAt = DateTime.UtcNow
            };

            _currentState = new AuthenticationState(user, result.Token, tokenExpiry);
            AuthenticationStateChanged?.Invoke(this, _currentState);

            Debug.WriteLine($"[EmailOtpAuth] Login successful for {email}");
            return true;
        }

        public async Task<string> RegisterAsync(string email, string? password = null)
            => await SendOtpAsync(email);

        public async Task<string> LoginAsync(string email)
            => await SendOtpAsync(email);

        public Task LogoutAsync()
        {
            try
            {
                _secureStorage.ClearAll();
                _currentState = new AuthenticationState();
                AuthenticationStateChanged?.Invoke(this, _currentState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] Logout error: {ex.Message}");
                throw new Exception($"Không thể đăng xuất: {ex.Message}", ex);
            }
            return Task.CompletedTask;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var userId = await _secureStorage.GetUserIdAsync();
                var email  = await _secureStorage.GetEmailAsync();
                var name   = await _secureStorage.GetDisplayNameAsync();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    return new User
                    {
                        UserId      = userId,
                        Email       = email,
                        DisplayName = name ?? email,
                        LastLoginAt = DateTime.UtcNow
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] GetCurrentUser error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                return await _secureStorage.HasUserDataAsync()
                    && await _secureStorage.IsTokenValidAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] IsAuthenticated error: {ex.Message}");
                return false;
            }
        }

        public async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (await IsAuthenticatedAsync())
            {
                var user   = await GetCurrentUserAsync();
                var token  = await _secureStorage.GetAuthTokenAsync();
                var expiry = await _secureStorage.GetTokenExpiryAsync();

                if (user != null && token != null && expiry != null)
                    _currentState = new AuthenticationState(user, token, expiry.Value);
            }
            else
            {
                _currentState = new AuthenticationState();
            }
            return _currentState;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string GetDisplayNameFromEmail(string email)
        {
            var at = email.IndexOf('@');
            return at > 0 ? email[..at] : email;
        }

        private static string GenerateFallbackUserId(string email)
            => $"user_{Math.Abs(email.GetHashCode()):x}";
    }
}
