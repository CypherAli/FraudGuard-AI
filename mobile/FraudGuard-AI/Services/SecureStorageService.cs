using System.Text.Json;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Service for secure storage of authentication tokens and sensitive data
    /// Uses .NET MAUI SecureStorage API
    /// </summary>
    public class SecureStorageService
    {
        private const string KEY_AUTH_TOKEN = "auth_token";
        private const string KEY_USER_ID = "user_id";
        private const string KEY_PHONE_NUMBER = "phone_number";
        private const string KEY_DISPLAY_NAME = "display_name";
        private const string KEY_TOKEN_EXPIRY = "token_expiry";

        /// <summary>
        /// Save authentication token
        /// </summary>
        public async Task SaveAuthTokenAsync(string token)
        {
            await SecureStorage.SetAsync(KEY_AUTH_TOKEN, token);
        }

        /// <summary>
        /// Get authentication token
        /// </summary>
        public async Task<string?> GetAuthTokenAsync()
        {
            return await SecureStorage.GetAsync(KEY_AUTH_TOKEN);
        }

        /// <summary>
        /// Save user data
        /// </summary>
        public async Task SaveUserDataAsync(string userId, string phoneNumber, string displayName)
        {
            await SecureStorage.SetAsync(KEY_USER_ID, userId);
            await SecureStorage.SetAsync(KEY_PHONE_NUMBER, phoneNumber);
            await SecureStorage.SetAsync(KEY_DISPLAY_NAME, displayName);
        }

        /// <summary>
        /// Get user ID
        /// </summary>
        public async Task<string?> GetUserIdAsync()
        {
            return await SecureStorage.GetAsync(KEY_USER_ID);
        }

        /// <summary>
        /// Get phone number
        /// </summary>
        public async Task<string?> GetPhoneNumberAsync()
        {
            return await SecureStorage.GetAsync(KEY_PHONE_NUMBER);
        }

        /// <summary>
        /// Get display name
        /// </summary>
        public async Task<string?> GetDisplayNameAsync()
        {
            return await SecureStorage.GetAsync(KEY_DISPLAY_NAME);
        }

        /// <summary>
        /// Save token expiry
        /// </summary>
        public async Task SaveTokenExpiryAsync(DateTime expiry)
        {
            await SecureStorage.SetAsync(KEY_TOKEN_EXPIRY, expiry.ToString("o")); // ISO 8601 format
        }

        /// <summary>
        /// Get token expiry
        /// </summary>
        public async Task<DateTime?> GetTokenExpiryAsync()
        {
            var expiryStr = await SecureStorage.GetAsync(KEY_TOKEN_EXPIRY);
            if (string.IsNullOrEmpty(expiryStr))
                return null;

            if (DateTime.TryParse(expiryStr, out var expiry))
                return expiry;

            return null;
        }

        /// <summary>
        /// Check if token is valid (not expired)
        /// </summary>
        public async Task<bool> IsTokenValidAsync()
        {
            var expiry = await GetTokenExpiryAsync();
            if (expiry == null)
                return false;

            return DateTime.UtcNow < expiry.Value;
        }

        /// <summary>
        /// Clear all stored data (logout)
        /// </summary>
        public void ClearAll()
        {
            SecureStorage.Remove(KEY_AUTH_TOKEN);
            SecureStorage.Remove(KEY_USER_ID);
            SecureStorage.Remove(KEY_PHONE_NUMBER);
            SecureStorage.Remove(KEY_DISPLAY_NAME);
            SecureStorage.Remove(KEY_TOKEN_EXPIRY);
        }

        /// <summary>
        /// Check if user data exists (for auto-login)
        /// </summary>
        public async Task<bool> HasUserDataAsync()
        {
            var userId = await GetUserIdAsync();
            var token = await GetAuthTokenAsync();
            return !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token);
        }
    }
}
