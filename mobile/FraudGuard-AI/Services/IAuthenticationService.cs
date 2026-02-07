using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Interface for Email OTP authentication service
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Send OTP to email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>True if OTP sent successfully</returns>
        Task<bool> SendOtpAsync(string email);

        /// <summary>
        /// Verify OTP and login
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="otp">6-digit OTP code</param>
        /// <returns>True if verification successful</returns>
        Task<bool> VerifyOtpAsync(string email, string otp);

        /// <summary>
        /// Logout current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
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
