using System.Diagnostics;
using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    public class EmailOtpAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private readonly BrevoEmailService _emailService;
        private AuthenticationState _currentState;
        private string? _pendingEmail;

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public EmailOtpAuthService(SecureStorageService secureStorage, BrevoEmailService emailService)
        {
            _secureStorage = secureStorage;
            _emailService = emailService;
            _currentState = new AuthenticationState();
        }

        public async Task<string> SendOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không hợp lệ");

            var normalizedEmail = email.Trim();
            if (!IsValidEmail(normalizedEmail))
                throw new ArgumentException("Email không hợp lệ");

            _pendingEmail = normalizedEmail;
            await _secureStorage.SaveEmailAsync(_pendingEmail);

            var otp = GenerateOtp();
            var expiry = DateTime.UtcNow.AddMinutes(5);
            await _secureStorage.SaveOtpAsync(otp, expiry);

            var sent = await _emailService.SendOtpEmailAsync(_pendingEmail, otp);
            if (!sent)
                throw new Exception("Không thể gửi mã OTP. Vui lòng thử lại.");

            Debug.WriteLine($"[EmailOtpAuth] OTP sent to {_pendingEmail}");
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> VerifyOtpAsync(string verificationId, string otpCode)
        {
            var storedOtp = await _secureStorage.GetOtpAsync();
            var expiry = await _secureStorage.GetOtpExpiryAsync();

            if (string.IsNullOrEmpty(storedOtp) || expiry == null)
                return false;

            if (DateTime.UtcNow > expiry.Value)
                return false;

            if (!string.Equals(storedOtp, otpCode, StringComparison.Ordinal))
                return false;

            var email = _pendingEmail ?? await _secureStorage.GetEmailAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Email = email,
                PhoneNumber = string.Empty,
                DisplayName = GetDisplayNameFromEmail(email),
                LastLoginAt = DateTime.UtcNow
            };

            var token = "EMAIL_TOKEN:" + Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddYears(10);

            await _secureStorage.SaveAuthTokenAsync(token);
            await _secureStorage.SaveUserDataAsync(user.UserId, user.PhoneNumber, user.DisplayName);
            await _secureStorage.SaveEmailAsync(email);
            await _secureStorage.SaveTokenExpiryAsync(tokenExpiry);

            _currentState = new AuthenticationState(user, token, tokenExpiry);
            AuthenticationStateChanged?.Invoke(this, _currentState);

            return true;
        }

        public async Task<string> RegisterAsync(string phoneNumber, string? password = null)
        {
            return await SendOtpAsync(phoneNumber);
        }

        public async Task<string> LoginAsync(string phoneNumber)
        {
            return await SendOtpAsync(phoneNumber);
        }

        public async Task LogoutAsync()
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
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var userId = await _secureStorage.GetUserIdAsync();
                var displayName = await _secureStorage.GetDisplayNameAsync();
                var email = await _secureStorage.GetEmailAsync();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    return new User
                    {
                        UserId = userId,
                        Email = email,
                        DisplayName = displayName ?? email,
                        LastLoginAt = DateTime.UtcNow
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] Error getting current user: {ex.Message}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmailOtpAuth] Error checking authentication: {ex.Message}");
                return false;
            }
        }

        public async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var isAuthenticated = await IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                var user = await GetCurrentUserAsync();
                var token = await _secureStorage.GetAuthTokenAsync();
                var expiry = await _secureStorage.GetTokenExpiryAsync();

                if (user != null && token != null && expiry != null)
                {
                    _currentState = new AuthenticationState(user, token, expiry.Value);
                }
            }
            else
            {
                _currentState = new AuthenticationState();
            }

            return _currentState;
        }

        private static string GenerateOtp()
        {
            var rng = new Random();
            return rng.Next(100000, 999999).ToString();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string GetDisplayNameFromEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
                return email.Substring(0, atIndex);
            return email;
        }
    }
}
