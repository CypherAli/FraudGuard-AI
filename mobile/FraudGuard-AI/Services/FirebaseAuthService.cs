using FraudGuardAI.Models;
using Plugin.Firebase.Auth;
using System.Diagnostics;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Firebase Phone Authentication Service - FREE OTP SMS via Firebase
    /// </summary>
    public class FirebaseAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private AuthenticationState _currentState;
        private string? _pendingPhoneNumber;
        private string? _verificationId; // Store verification ID from callback

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public FirebaseAuthService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _currentState = new AuthenticationState();
        }

        /// <summary>Send OTP to phone number</summary>
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
                _verificationId = null;

#if ANDROID
                // Android: Use native handler with proper callbacks
                var activity = Platform.CurrentActivity 
                    ?? throw new InvalidOperationException("Current Activity is null");
                
                _verificationId = await Platforms.Android.PhoneAuthHandler.SendOtpAsync(
                    phoneNumber, 
                    activity
                );
                
                Debug.WriteLine($"[FirebaseAuth] OTP sent. VerificationId: {_verificationId}");
                return _verificationId;
#else
                // iOS: Use Plugin.Firebase
                await CrossFirebaseAuth.Current.VerifyPhoneNumberAsync(phoneNumber);
                _verificationId = phoneNumber;
                Debug.WriteLine($"[FirebaseAuth] OTP sent to iOS device");
                return phoneNumber;
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error sending OTP: {ex.Message}");
                throw new Exception($"Không thể gửi mã OTP: {ex.Message}", ex);
            }
        }

        /// <summary>Verify OTP code</summary>
        public async Task<bool> VerifyOtpAsync(string verificationId, string otpCode)
        {
            try
            {
                Debug.WriteLine($"[FirebaseAuth] Verifying OTP code: {otpCode}");

                if (string.IsNullOrEmpty(otpCode) || otpCode.Length != 6)
                {
                    throw new ArgumentException("Mã OTP phải có 6 chữ số");
                }

                // Use stored verification ID if available
                var actualVerificationId = !string.IsNullOrEmpty(_verificationId) ? _verificationId : verificationId;

                if (string.IsNullOrEmpty(actualVerificationId))
                {
                    throw new InvalidOperationException("VerificationId not found. Please send OTP again.");
                }

                Debug.WriteLine($"[FirebaseAuth] Using VerificationId: {actualVerificationId.Substring(0, Math.Min(10, actualVerificationId.Length))}...");

#if ANDROID
                // Android: Create native credential and sign in
                var credential = Platforms.Android.PhoneAuthHandler.GetCredential(
                    actualVerificationId, 
                    otpCode
                );
                
                // Sign in using Firebase Android SDK
                var auth = Firebase.Auth.FirebaseAuth.Instance;
                var authResult = await auth.SignInWithCredentialAsync(credential);
                
                if (authResult?.User != null)
                {
                    var firebaseUser = authResult.User;
                    Debug.WriteLine($"[FirebaseAuth] Android auth successful. UID: {firebaseUser.Uid}");
                    
                    // Convert to Plugin.Firebase user for consistency
                    var user = CrossFirebaseAuth.Current.CurrentUser;
                    
                    if (user != null)
                    {
                        // Save user data
                        await SaveUserToStorage(user);
                        await UpdateAuthenticationState(user);
                        _verificationId = null;
                        return true;
                    }
                }
#else
                // iOS: Use Plugin.Firebase
                var user = await CrossFirebaseAuth.Current.SignInWithPhoneNumberVerificationCodeAsync(
                    actualVerificationId, 
                    otpCode
                );
                
                if (user != null)
                {
                    await SaveUserToStorage(user);
                    await UpdateAuthenticationState(user);
                    _verificationId = null;
                    return true;
                }
#endif

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseAuth] Error verifying OTP: {ex.Message}");
                Debug.WriteLine($"[FirebaseAuth] Stack trace: {ex.StackTrace}");
                throw new Exception($"Mã OTP không đúng hoặc đã hết hạn: {ex.Message}", ex);
            }
        }

        /// <summary>Register/Login user (same flow for Firebase Phone Auth)</summary>
        public async Task<string> RegisterAsync(string phoneNumber, string? password = null)
            => await SendOtpAsync(phoneNumber);

        /// <summary>Login user</summary>
        public async Task<string> LoginAsync(string phoneNumber)
            => await SendOtpAsync(phoneNumber);

        /// <summary>Logout current user</summary>
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

        /// <summary>Get current authenticated user</summary>
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

        /// <summary>Check if user is authenticated</summary>
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

        /// <summary>Get authentication state</summary>
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

        #region Private Helpers

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
