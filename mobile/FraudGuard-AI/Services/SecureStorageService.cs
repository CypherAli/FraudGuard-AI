using System.Text.Json;

namespace FraudGuardAI.Services
{
    public class SecureStorageService
    {
        private const string KEY_AUTH_TOKEN = "auth_token";
        private const string KEY_USER_ID = "user_id";
        private const string KEY_PHONE_NUMBER = "phone_number";
        private const string KEY_DISPLAY_NAME = "display_name";
        private const string KEY_TOKEN_EXPIRY = "token_expiry";

        public async Task SaveAuthTokenAsync(string token)
            => await SecureStorage.SetAsync(KEY_AUTH_TOKEN, token);

        public async Task<string?> GetAuthTokenAsync()
            => await SecureStorage.GetAsync(KEY_AUTH_TOKEN);

        public async Task SaveUserDataAsync(string userId, string phoneNumber, string displayName)
        {
            await SecureStorage.SetAsync(KEY_USER_ID, userId);
            await SecureStorage.SetAsync(KEY_PHONE_NUMBER, phoneNumber);
            await SecureStorage.SetAsync(KEY_DISPLAY_NAME, displayName);
        }

        public async Task<string?> GetUserIdAsync()
            => await SecureStorage.GetAsync(KEY_USER_ID);

        public async Task<string?> GetPhoneNumberAsync()
            => await SecureStorage.GetAsync(KEY_PHONE_NUMBER);

        public async Task<string?> GetDisplayNameAsync()
            => await SecureStorage.GetAsync(KEY_DISPLAY_NAME);

        public async Task SaveTokenExpiryAsync(DateTime expiry)
            => await SecureStorage.SetAsync(KEY_TOKEN_EXPIRY, expiry.ToString("o"));

        public async Task<DateTime?> GetTokenExpiryAsync()
        {
            var expiryStr = await SecureStorage.GetAsync(KEY_TOKEN_EXPIRY);
            return string.IsNullOrEmpty(expiryStr) ? null
                : DateTime.TryParse(expiryStr, out var expiry) ? expiry : null;
        }

        public async Task<bool> IsTokenValidAsync()
        {
            var expiry = await GetTokenExpiryAsync();
            return expiry.HasValue && DateTime.UtcNow < expiry.Value;
        }

        public void ClearAll()
        {
            SecureStorage.Remove(KEY_AUTH_TOKEN);
            SecureStorage.Remove(KEY_USER_ID);
            SecureStorage.Remove(KEY_PHONE_NUMBER);
            SecureStorage.Remove(KEY_DISPLAY_NAME);
            SecureStorage.Remove(KEY_TOKEN_EXPIRY);
        }

        public async Task<bool> HasUserDataAsync()
        {
            var userId = await GetUserIdAsync();
            var token = await GetAuthTokenAsync();
            return !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token);
        }
    }
}
