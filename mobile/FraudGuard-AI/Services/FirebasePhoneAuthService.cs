using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Firebase Phone Authentication Service using REST API
    /// Sends OTP via Firebase using signupWithPhoneNumber endpoint
    /// </summary>
    public class FirebasePhoneAuthService
    {
        private const string FirebaseApiKey = "AIzaSyAgEUKbUPY0z3oipWWz3KE9_IG-I3LVMGM"; // From google-services.json
        private const string FirebaseSignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signupWithPhoneNumber?key={0}";
        private const string FirebaseSignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPhoneNumber?key={0}";
        private const string FirebaseVerifyUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={0}";
        
        private readonly HttpClient _httpClient;
        private string? _sessionInfo;
        private string? _phoneNumber;

        public FirebasePhoneAuthService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Send OTP to phone number
        /// </summary>
        public async Task<bool> SendOtpAsync(string phoneNumber)
        {
            try
            {
                // Normalize and log input so we can debug on-device
                phoneNumber = phoneNumber?.Trim();
                Debug.WriteLine($"[FirebasePhoneAuth] Sending OTP to {phoneNumber}");
                Console.WriteLine($"[FirebasePhoneAuth] Sending OTP to {phoneNumber}");
#if ANDROID
                Android.Util.Log.Info("FirebasePhoneAuth", $"Sending OTP to {phoneNumber}");
#endif
                
                _phoneNumber = phoneNumber;
                
                // Development shortcut: if using Firebase "test phone numbers" configured in Firebase Console
                // we can short-circuit and simulate sending/receiving OTP so tests work without reCAPTCHA/REST.
                // Add more numbers here if needed.
                var devTestNumbers = new[] { "+84900000000" };
                var normalizedPhone = phoneNumber ?? string.Empty;
                if (devTestNumbers.Contains(normalizedPhone))
                {
                    // Simulate a session id for test flows
                    _sessionInfo = "TEST_SESSION:" + Guid.NewGuid().ToString();
                    Debug.WriteLine($"[FirebasePhoneAuth] (DEV) OTP simulated for {normalizedPhone}. Session: {_sessionInfo}");
                    Console.WriteLine($"[FirebasePhoneAuth] (DEV) OTP simulated for {normalizedPhone}. Session: {_sessionInfo}");
#if ANDROID
                    Android.Util.Log.Info("FirebasePhoneAuth", $"(DEV) OTP simulated for {normalizedPhone}. Session: {_sessionInfo}");
#endif
                    return await Task.FromResult(true);
                }

                // Production flow: attempt REST call to Firebase Identity Toolkit
                var requestBody = new
                {
                    phoneNumber = phoneNumber,
                    recaptchaToken = ""
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = string.Format(FirebaseSignUpUrl, FirebaseApiKey);
                var response = await _httpClient.PostAsync(url, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[FirebasePhoneAuth] Response: {responseContent}");
                Console.WriteLine($"[FirebasePhoneAuth] Response: {responseContent}");
#if ANDROID
                Android.Util.Log.Info("FirebasePhoneAuth", $"Response: {responseContent}");
#endif

                if (response.IsSuccessStatusCode)
                {
                    // Parse response to get session info
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    if (jsonDoc.RootElement.TryGetProperty("sessionInfo", out var sessionInfo))
                    {
                        _sessionInfo = sessionInfo.GetString();
                        Debug.WriteLine($"[FirebasePhoneAuth] OTP sent successfully. Session: {_sessionInfo}");
                        return true;
                    }
                }

                Debug.WriteLine($"[FirebasePhoneAuth] Failed to send OTP: {responseContent}");
                Console.WriteLine($"[FirebasePhoneAuth] Failed to send OTP: {responseContent}");
#if ANDROID
                Android.Util.Log.Warn("FirebasePhoneAuth", $"Failed to send OTP: {responseContent}");
#endif
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebasePhoneAuth] Error sending OTP: {ex.Message}");
                Console.WriteLine($"[FirebasePhoneAuth] Error sending OTP: {ex}");
#if ANDROID
                Android.Util.Log.Error("FirebasePhoneAuth", ex.ToString());
#endif
                return false;
            }
        }

        /// <summary>
        /// Verify OTP code
        /// </summary>
        public async Task<string?> VerifyOtpAsync(string otpCode)
        {
            try
            {
                Debug.WriteLine($"[FirebasePhoneAuth] Verifying OTP code: {otpCode}");
                Console.WriteLine($"[FirebasePhoneAuth] Verifying OTP code: {otpCode}");
#if ANDROID
                Android.Util.Log.Info("FirebasePhoneAuth", $"Verifying OTP code: {otpCode}");
#endif

                if (string.IsNullOrEmpty(_sessionInfo))
                {
                    Debug.WriteLine("[FirebasePhoneAuth] No session info available");
                    Console.WriteLine("[FirebasePhoneAuth] No session info available");
#if ANDROID
                    Android.Util.Log.Warn("FirebasePhoneAuth", "No session info available");
#endif
                    return null;
                }
                
                // Development shortcut: accept test OTP code for known test numbers
                if (_sessionInfo != null && _sessionInfo.StartsWith("TEST_SESSION:") && _phoneNumber != null)
                {
                    // Expected test code for dev number above
                    var expected = "123456";
                    if (otpCode == expected)
                    {
                        var fakeToken = "DEV_TOKEN:" + Guid.NewGuid().ToString();
                        Debug.WriteLine($"[FirebasePhoneAuth] (DEV) OTP verified successfully. Token: {fakeToken}");
                        Console.WriteLine($"[FirebasePhoneAuth] (DEV) OTP verified successfully. Token: {fakeToken}");
#if ANDROID
                        Android.Util.Log.Info("FirebasePhoneAuth", $"(DEV) OTP verified successfully. Token: {fakeToken}");
#endif
                        return await Task.FromResult<string?>(fakeToken);
                    }

                    Debug.WriteLine("[FirebasePhoneAuth] (DEV) OTP mismatch");
                    Console.WriteLine("[FirebasePhoneAuth] (DEV) OTP mismatch");
#if ANDROID
                    Android.Util.Log.Warn("FirebasePhoneAuth", "(DEV) OTP mismatch");
#endif
                    return await Task.FromResult<string?>(null);
                }

                var requestBody = new
                {
                    sessionInfo = _sessionInfo,
                    code = otpCode
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = string.Format(FirebaseSignUpUrl, FirebaseApiKey);
                var response = await _httpClient.PostAsync(url, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[FirebasePhoneAuth] Verify Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    if (jsonDoc.RootElement.TryGetProperty("idToken", out var idToken))
                    {
                        var token = idToken.GetString();
                        Debug.WriteLine($"[FirebasePhoneAuth] OTP verified successfully. Token: {token?.Substring(0, 20)}...");
                        return token;
                    }
                }

                Debug.WriteLine($"[FirebasePhoneAuth] Failed to verify OTP: {responseContent}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebasePhoneAuth] Error verifying OTP: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get current session info
        /// </summary>
        public string? GetSessionInfo() => _sessionInfo;

        /// <summary>
        /// Clear session
        /// </summary>
        public void ClearSession()
        {
            _sessionInfo = null;
            _phoneNumber = null;
        }
    }
}
