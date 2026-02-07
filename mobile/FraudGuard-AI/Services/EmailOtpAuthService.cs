using FraudGuardAI.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Email OTP Authentication Service - calls backend API
    /// </summary>
    public class EmailOtpAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private readonly HttpClient _httpClient;
        private AuthenticationState _currentState;

        // Backend API URL - will be configured from settings
        private string ApiBaseUrl => GetApiBaseUrl();

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public EmailOtpAuthService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _currentState = new AuthenticationState();
            
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private string GetApiBaseUrl()
        {
            // Try to get from preferences, fallback to default
            try
            {
                var savedUrl = Preferences.Get("api_base_url", "");
                if (!string.IsNullOrEmpty(savedUrl))
                    return savedUrl.TrimEnd('/');
            }
            catch { }
            
            // Default URLs to try
            return "https://fraudguard-ai.onrender.com"; // Production URL
        }

        /// <summary>
        /// Send OTP to email
        /// </summary>
        public async Task<bool> SendOtpAsync(string email)
        {
            try
            {
                ValidateEmail(email);

                Debug.WriteLine($"[OtpAuth] Sending OTP to: {email}");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{ApiBaseUrl}/auth/send-otp",
                    new { email = email.Trim().ToLower() }
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[OtpAuth] Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent);
                    if (result?.success == true)
                    {
                        // Store email in preferences for verification step
                        Preferences.Set("pending_email", email.Trim().ToLower());
                        Debug.WriteLine($"[OtpAuth] OTP sent successfully to {email}");
                        return true;
                    }
                }

                // Parse error
                try
                {
                    var errorResult = JsonSerializer.Deserialize<ApiResponse>(responseContent);
                    throw new Exception(errorResult?.error ?? "Không thể gửi OTP");
                }
                catch (JsonException)
                {
                    throw new Exception("Không thể gửi OTP. Vui lòng thử lại.");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[OtpAuth] Network error: {ex.Message}");
                throw new Exception("Lỗi kết nối. Vui lòng kiểm tra Internet.", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Hết thời gian chờ. Vui lòng thử lại.");
            }
            catch (Exception ex) when (ex.Message.Contains("Không thể"))
            {
                throw; // Re-throw our custom errors
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OtpAuth] SendOtp error: {ex.Message}");
                throw new Exception($"Đã xảy ra lỗi: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verify OTP and login
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            try
            {
                ValidateEmail(email);
                ValidateOtp(otp);

                Debug.WriteLine($"[OtpAuth] Verifying OTP for: {email}");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{ApiBaseUrl}/auth/verify-otp",
                    new { email = email.Trim().ToLower(), otp = otp.Trim() }
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[OtpAuth] Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<VerifyOtpResponse>(responseContent);
                    if (result?.success == true)
                    {
                        Debug.WriteLine($"[OtpAuth] OTP verified successfully");
                        
                        // Save user data
                        await _secureStorage.SaveAuthTokenAsync(result.token ?? "");
                        await _secureStorage.SaveUserDataAsync(
                            result.user_id ?? "",
                            result.email ?? email,
                            result.email ?? email
                        );
                        await _secureStorage.SaveTokenExpiryAsync(DateTime.UtcNow.AddYears(1)); // Long session
                        
                        // Update state
                        var user = new User
                        {
                            UserId = result.user_id ?? "",
                            Email = result.email ?? email,
                            DisplayName = result.email ?? email,
                            IsEmailVerified = true,
                            LastLoginAt = DateTime.UtcNow
                        };
                        
                        _currentState = new AuthenticationState(
                            user, 
                            result.token ?? "", 
                            DateTime.UtcNow.AddYears(1)
                        );
                        
                        AuthenticationStateChanged?.Invoke(this, _currentState);
                        
                        return true;
                    }
                }

                // Parse error
                try
                {
                    var errorResult = JsonSerializer.Deserialize<ApiResponse>(responseContent);
                    throw new Exception(errorResult?.error ?? "OTP không chính xác");
                }
                catch (JsonException)
                {
                    throw new Exception("OTP không chính xác. Vui lòng thử lại.");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[OtpAuth] Network error: {ex.Message}");
                throw new Exception("Lỗi kết nối. Vui lòng kiểm tra Internet.", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Hết thời gian chờ. Vui lòng thử lại.");
            }
            catch (Exception ex) when (ex.Message.Contains("OTP") || ex.Message.Contains("Lỗi"))
            {
                throw; // Re-throw our custom errors
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OtpAuth] VerifyOtp error: {ex.Message}");
                throw new Exception($"Đã xảy ra lỗi: {ex.Message}", ex);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _secureStorage.ClearAll();
                _currentState = new AuthenticationState();
                AuthenticationStateChanged?.Invoke(this, _currentState);
                Debug.WriteLine("[OtpAuth] User logged out");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể đăng xuất: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var userId = await _secureStorage.GetUserIdAsync();
                var email = await _secureStorage.GetEmailAsync();
                var displayName = await _secureStorage.GetDisplayNameAsync();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    return new User
                    {
                        UserId = userId,
                        Email = email,
                        DisplayName = displayName ?? email,
                        IsEmailVerified = true,
                        LastLoginAt = DateTime.UtcNow
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var hasUserData = await _secureStorage.HasUserDataAsync();
                var isTokenValid = await _secureStorage.IsTokenValidAsync();
                return hasUserData && isTokenValid;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (await IsAuthenticatedAsync())
            {
                var user = await GetCurrentUserAsync();
                var token = await _secureStorage.GetAuthTokenAsync();
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

        #region Private Helpers

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Vui lòng nhập email");
            
            if (!email.Contains("@") || !email.Contains("."))
                throw new Exception("Email không hợp lệ");
        }

        private void ValidateOtp(string otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
                throw new Exception("Vui lòng nhập mã OTP");
            
            if (otp.Length != 6 || !otp.All(char.IsDigit))
                throw new Exception("Mã OTP phải là 6 chữ số");
        }

        #endregion

        #region Response Models

        private class ApiResponse
        {
            public bool success { get; set; }
            public string? error { get; set; }
            public string? message { get; set; }
        }

        private class VerifyOtpResponse : ApiResponse
        {
            public string? token { get; set; }
            public string? email { get; set; }
            public string? user_id { get; set; }
        }

        #endregion
    }
}
