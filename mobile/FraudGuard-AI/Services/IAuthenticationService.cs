using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Interface for authentication service
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Send OTP to email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Verification ID for OTP verification</returns>
        Task<string> SendOtpAsync(string email);

        /// <summary>
        /// Verify OTP code
        /// </summary>
        /// <param name="verificationId">Verification ID from SendOtpAsync</param>
        /// <param name="otpCode">6-digit OTP code</param>
        /// <returns>True if verification successful</returns>
        Task<bool> VerifyOtpAsync(string verificationId, string otpCode);

        /// <summary>
        /// Register new user (not used in email-only flow)
        /// </summary>
        /// <param name="phoneNumber">Legacy parameter</param>
        /// <param name="password">User password (optional, for future use)</param>
        /// <returns>Verification ID for OTP verification</returns>
        Task<string> RegisterAsync(string phoneNumber, string? password = null);

        /// <summary>
        /// Login existing user with email
        /// </summary>
        /// <param name="phoneNumber">Email address</param>
        /// <returns>Verification ID for OTP verification</returns>
        Task<string> LoginAsync(string phoneNumber);

        /// <summary>
        /// Logout current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        /// <returns>Current user or null if not authenticated</returns>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        /// <returns>True if user is logged in</returns>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Get authentication state
        /// </summary>
        Task<AuthenticationState> GetAuthenticationStateAsync();

        /// <summary>
        /// Event fired when authentication state changes
        /// </summary>
        event EventHandler<AuthenticationState>? AuthenticationStateChanged;
    }
}
