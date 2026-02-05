using FraudGuardAI.Models;
using System.Diagnostics;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Firebase Phone Authentication Service
    /// Uses Firebase REST API with test numbers for development
    /// For production: configure Firebase Console with SHA-1/SHA-256 + test phone numbers
    /// </summary>
    public class FirebaseAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private readonly FirebasePhoneAuthService _phoneAuthService;
        private AuthenticationState _currentState;
        private string? _pendingPhoneNumber;

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public FirebaseAuthService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _phoneAuthService = new FirebasePhoneAuthService();
            _currentState = new AuthenticationState();
        }

        /// <summary>
        /// Send OTP to phone number via Firebase REST API
        /// For development: use Firebase test phone numbers added to Firebase Console
        /// Example: Phone +84900000000 with test code 123456
        /// </summary>
        public async Task<string> SendOtpAsync(string phoneNumber)
        {
            try
            {
                Debug.WriteLine($"[FirebaseAuth] Sending OTP to {phoneNumber}");

                // Validate phone number format
                if (!IsValidPhoneNumber(phoneNumber))
                {
                    throw new ArgumentException("Số điện thoại không hợp lệ. Vui lòng nhập theo định dạng +84xxxxxxxxx");
                }

                // Store phone number for later use
                _pendingPhoneNumber = phoneNumber;

                // Send OTP via Firebase REST API
                var success = await _phoneAuthService.SendOtpAsync(phoneNumber);
                
                if (!success)
                {
                    throw new Exception("Không thể gửi mã OTP. Vui lòng thử lại.");
                }

                Debug.WriteLine($"[FirebaseAuth] OTP sent successfully to {phoneNumber}");
                
                // Return phone number as verification identifier
                return phoneNumber;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error sending OTP: {ex.Message}");
                throw new Exception($"Không thể gửi mã OTP: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verify OTP code
        /// For development: use test OTP codes from Firebase Console (e.g., 123456)
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string verificationId, string otpCode)
        {
            try
            {
                Debug.WriteLine($"[FirebaseAuth] Verifying OTP code: {otpCode}");

                if (string.IsNullOrEmpty(otpCode) || otpCode.Length != 6)
                {
                    throw new ArgumentException("Mã OTP phải có 6 chữ số");
                }

                // Verify OTP via Firebase REST API
                var idToken = await _phoneAuthService.VerifyOtpAsync(otpCode);

                if (string.IsNullOrEmpty(idToken))
                {
                    throw new Exception("Mã OTP không đúng hoặc đã hết hạn");
                }

                Debug.WriteLine($"[FirebaseAuth] OTP verified successfully");

                // Create a user object with the phone number
                var user = new Models.User
                {
                    UserId = Guid.NewGuid().ToString(),
                    PhoneNumber = _pendingPhoneNumber ?? "",
                    DisplayName = _pendingPhoneNumber ?? "User",
                    LastLoginAt = DateTime.UtcNow
                };

                // Save user data to secure storage
                await _secureStorage.SaveAuthTokenAsync(idToken);
                await _secureStorage.SaveUserDataAsync(user.UserId, user.PhoneNumber, user.DisplayName);
                await _secureStorage.SaveTokenExpiryAsync(DateTime.UtcNow.AddHours(1));

                // Update authentication state
                _currentState = new AuthenticationState(user, idToken, DateTime.UtcNow.AddHours(1));
                AuthenticationStateChanged?.Invoke(this, _currentState);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error verifying OTP: {ex.Message}");
                throw new Exception($"Mã OTP không đúng hoặc đã hết hạn: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Register new user (same as login for phone auth)
        /// </summary>
        public async Task<string> RegisterAsync(string phoneNumber, string? password = null)
        {
            Debug.WriteLine($"[FirebaseAuth] Registering new user: {phoneNumber}");
            return await SendOtpAsync(phoneNumber);
        }

        /// <summary>
        /// Login existing user
        /// </summary>
        public async Task<string> LoginAsync(string phoneNumber)
        {
            Debug.WriteLine($"[FirebaseAuth] Logging in user: {phoneNumber}");
            return await SendOtpAsync(phoneNumber);
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                Debug.WriteLine("[FirebaseAuth] Logging out user");

                // Clear secure storage
                _secureStorage.ClearAll();

                // Update authentication state
                _currentState = new AuthenticationState();
                AuthenticationStateChanged?.Invoke(this, _currentState);

                Debug.WriteLine("[FirebaseAuth] User logged out successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error logging out: {ex.Message}");
                throw new Exception($"Không thể đăng xuất: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var userId = await _secureStorage.GetUserIdAsync();
                var storedPhoneNumber = await _secureStorage.GetPhoneNumberAsync();
                var displayName = await _secureStorage.GetDisplayNameAsync();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(storedPhoneNumber))
                {
                    return new User
                    {
                        UserId = userId,
                        PhoneNumber = storedPhoneNumber,
                        DisplayName = displayName ?? storedPhoneNumber,
                        LastLoginAt = DateTime.UtcNow
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error getting current user: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
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
                Debug.WriteLine($"[FirebaseAuth] Error checking authentication: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get authentication state
        /// </summary>
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

        #region Private Helper Methods

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            if (!phoneNumber.StartsWith("+"))
                return false;

            var digits = phoneNumber.Substring(1);
            if (!digits.All(char.IsDigit))
                return false;

            return digits.Length >= 10 && digits.Length <= 15;
        }

        #endregion
    }
}
