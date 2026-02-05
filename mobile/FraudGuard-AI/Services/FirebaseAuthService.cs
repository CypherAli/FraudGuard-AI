using FraudGuardAI.Models;
using Plugin.Firebase.Auth;
using System.Diagnostics;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Firebase Phone Authentication Service
    /// Provides FREE OTP SMS delivery via Firebase
    /// Uses Plugin.Firebase.Auth 3.x API
    /// </summary>
    public class FirebaseAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private AuthenticationState _currentState;
        private string? _pendingPhoneNumber;

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public FirebaseAuthService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _currentState = new AuthenticationState();
        }

        /// <summary>
        /// Send OTP to phone number (FREE via Firebase)
        /// Plugin.Firebase 3.x: VerifyPhoneNumberAsync is void
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

                // Send OTP via Firebase Phone Authentication
                // Plugin.Firebase 3.x: This method is void
                await CrossFirebaseAuth.Current.VerifyPhoneNumberAsync(phoneNumber);

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
        /// Plugin.Firebase 3.x: SignInWithPhoneNumberVerificationCodeAsync
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

                // Sign in with verification code
                // Plugin.Firebase 3.x: SignInWithPhoneNumberVerificationCodeAsync
                var user = await CrossFirebaseAuth.Current.SignInWithPhoneNumberVerificationCodeAsync(otpCode);

                if (user != null)
                {
                    Debug.WriteLine($"[FirebaseAuth] OTP verified successfully. User ID: {user.Uid}");

                    // Save user data to secure storage
                    await SaveUserToStorage(user);

                    // Update authentication state
                    await UpdateAuthenticationState(user);

                    return true;
                }

                return false;
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
            // For Firebase Phone Auth, registration and login are the same
            // Firebase automatically creates a new user if phone number doesn't exist
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

                // Sign out from Firebase
                await CrossFirebaseAuth.Current.SignOutAsync();

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
                var firebaseUser = CrossFirebaseAuth.Current.CurrentUser;
                
                if (firebaseUser != null)
                {
                    // Get phone number from provider info
                    var phoneNumber = GetPhoneNumberFromUser(firebaseUser);
                    
                    return new User
                    {
                        UserId = firebaseUser.Uid,
                        PhoneNumber = phoneNumber,
                        DisplayName = firebaseUser.DisplayName ?? phoneNumber ?? "User",
                        Email = firebaseUser.Email,
                        PhotoUrl = firebaseUser.PhotoUrl,
                        LastLoginAt = DateTime.UtcNow
                    };
                }

                // Try to restore from secure storage
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
                // Check Firebase current user
                var firebaseUser = CrossFirebaseAuth.Current.CurrentUser;
                if (firebaseUser != null)
                {
                    return true;
                }

                // Check secure storage
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

        /// <summary>
        /// Validate phone number format
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Must start with + and country code
            if (!phoneNumber.StartsWith("+"))
                return false;

            // Remove + and check if remaining is digits
            var digits = phoneNumber.Substring(1);
            if (!digits.All(char.IsDigit))
                return false;

            // Length should be between 10-15 digits (including country code)
            return digits.Length >= 10 && digits.Length <= 15;
        }

        /// <summary>
        /// Get phone number from Firebase user (via provider infos)
        /// </summary>
        private string? GetPhoneNumberFromUser(IFirebaseUser user)
        {
            // Check provider infos for phone number
            var phoneProvider = user.ProviderInfos?.FirstOrDefault(p => 
                p.ProviderId == "phone" || !string.IsNullOrEmpty(p.PhoneNumber));
            
            if (phoneProvider != null && !string.IsNullOrEmpty(phoneProvider.PhoneNumber))
            {
                return phoneProvider.PhoneNumber;
            }

            // Fall back to pending phone number
            return _pendingPhoneNumber;
        }

        /// <summary>
        /// Save user data to secure storage
        /// </summary>
        private async Task SaveUserToStorage(IFirebaseUser firebaseUser)
        {
            try
            {
                // Get ID token using GetIdTokenResultAsync
                var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
                var token = tokenResult?.Token ?? string.Empty;

                // Get phone number
                var phoneNumber = GetPhoneNumberFromUser(firebaseUser) ?? string.Empty;

                // Save to secure storage
                await _secureStorage.SaveAuthTokenAsync(token);
                await _secureStorage.SaveUserDataAsync(
                    firebaseUser.Uid,
                    phoneNumber,
                    firebaseUser.DisplayName ?? phoneNumber ?? "User"
                );

                // Token expires in 1 hour by default
                var expiry = DateTime.UtcNow.AddHours(1);
                await _secureStorage.SaveTokenExpiryAsync(expiry);

                Debug.WriteLine("[FirebaseAuth] User data saved to secure storage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error saving user to storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Update authentication state
        /// </summary>
        private async Task UpdateAuthenticationState(IFirebaseUser firebaseUser)
        {
            var phoneNumber = GetPhoneNumberFromUser(firebaseUser) ?? string.Empty;
            
            var user = new User
            {
                UserId = firebaseUser.Uid,
                PhoneNumber = phoneNumber,
                DisplayName = firebaseUser.DisplayName ?? phoneNumber ?? "User",
                Email = firebaseUser.Email,
                PhotoUrl = firebaseUser.PhotoUrl,
                LastLoginAt = DateTime.UtcNow
            };

            var token = await _secureStorage.GetAuthTokenAsync();
            var expiry = await _secureStorage.GetTokenExpiryAsync();

            _currentState = new AuthenticationState(user, token ?? string.Empty, expiry ?? DateTime.UtcNow.AddHours(1));
            
            AuthenticationStateChanged?.Invoke(this, _currentState);
        }

        #endregion
    }
}
