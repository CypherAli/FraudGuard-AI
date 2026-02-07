using FraudGuardAI.Models;
using Plugin.Firebase.Auth;
using System.Diagnostics;

namespace FraudGuardAI.Services
{
    /// <summary>Firebase Phone Authentication Service</summary>
    public class FirebaseAuthService : IAuthenticationService
    {
        private readonly SecureStorageService _secureStorage;
        private AuthenticationState _currentState;
        private string? _pendingPhoneNumber;
        private string? _verificationId;

        public event EventHandler<AuthenticationState>? AuthenticationStateChanged;

        public FirebaseAuthService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _currentState = new AuthenticationState();
        }

        public async Task<string> SendOtpAsync(string phoneNumber)
        {
            try
            {
                if (!IsValidPhoneNumber(phoneNumber))
                    throw new ArgumentException("Số điện thoại không hợp lệ. Vui lòng nhập theo định dạng +84xxxxxxxxx");

                _pendingPhoneNumber = phoneNumber;
                _verificationId = null;

#if ANDROID
                var activity = Platform.CurrentActivity 
                    ?? throw new InvalidOperationException("Current Activity is null");
                
                _verificationId = await Platforms.Android.PhoneAuthHandler.SendOtpAsync(phoneNumber, activity);
                return _verificationId;
#else
                await CrossFirebaseAuth.Current.VerifyPhoneNumberAsync(phoneNumber);
                _verificationId = phoneNumber;
                return phoneNumber;
#endif
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể gửi mã OTP: {ex.Message}", ex);
            }
        }

        public async Task<bool> VerifyOtpAsync(string verificationId, string otpCode)
        {
            try
            {
                if (string.IsNullOrEmpty(otpCode) || otpCode.Length != 6)
                    throw new ArgumentException("Mã OTP phải có 6 chữ số");

                var actualVerificationId = !string.IsNullOrEmpty(_verificationId) ? _verificationId : verificationId;
                if (string.IsNullOrEmpty(actualVerificationId))
                    throw new InvalidOperationException("VerificationId not found. Please send OTP again.");

#if ANDROID
                var credential = Platforms.Android.PhoneAuthHandler.GetCredential(actualVerificationId, otpCode);
                var authResult = await Firebase.Auth.FirebaseAuth.Instance.SignInWithCredentialAsync(credential);
                
                if (authResult?.User != null)
                {
                    var user = CrossFirebaseAuth.Current.CurrentUser;
                    if (user != null)
                    {
                        await SaveUserToStorage(user);
                        await UpdateAuthenticationState(user);
                        _verificationId = null;
                        return true;
                    }
                }
#else
                var user = await CrossFirebaseAuth.Current.SignInWithPhoneNumberVerificationCodeAsync(actualVerificationId, otpCode);
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
                throw new Exception($"Mã OTP không đúng hoặc đã hết hạn: {ex.Message}", ex);
            }
        }

        public async Task<string> RegisterAsync(string phoneNumber, string? password = null)
            => await SendOtpAsync(phoneNumber);

        public async Task<string> LoginAsync(string phoneNumber)
            => await SendOtpAsync(phoneNumber);

        public async Task LogoutAsync()
        {
            try
            {
                var firebaseAuth = CrossFirebaseAuth.Current;
                if (firebaseAuth != null)
                {
                    await firebaseAuth.SignOutAsync();
                }
                _secureStorage.ClearAll();
                _currentState = new AuthenticationState();
                AuthenticationStateChanged?.Invoke(this, _currentState);
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
                var firebaseAuth = CrossFirebaseAuth.Current;
                var firebaseUser = firebaseAuth?.CurrentUser;
                
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
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var firebaseAuth = CrossFirebaseAuth.Current;
                if (firebaseAuth?.CurrentUser != null)
                    return true;

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

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.StartsWith("+"))
                return false;

            var digits = phoneNumber.Substring(1);
            return digits.All(char.IsDigit) && digits.Length >= 10 && digits.Length <= 15;
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
                var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
                var token = tokenResult?.Token ?? string.Empty;
                var phoneNumber = GetPhoneNumberFromUser(firebaseUser) ?? string.Empty;

                await _secureStorage.SaveAuthTokenAsync(token);
                await _secureStorage.SaveUserDataAsync(
                    firebaseUser.Uid,
                    phoneNumber,
                    firebaseUser.DisplayName ?? phoneNumber ?? "User"
                );
                await _secureStorage.SaveTokenExpiryAsync(DateTime.UtcNow.AddHours(1));
            }
            catch { }
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
