namespace FraudGuardAI.Models
{
    /// <summary>
    /// Authentication state model
    /// </summary>
    public class AuthenticationState
    {
        public bool IsAuthenticated { get; set; }
        public User? CurrentUser { get; set; }
        public string? AuthToken { get; set; }
        public DateTime? TokenExpiry { get; set; }

        public AuthenticationState()
        {
            IsAuthenticated = false;
        }

        public AuthenticationState(User user, string token, DateTime expiry)
        {
            IsAuthenticated = true;
            CurrentUser = user;
            AuthToken = token;
            TokenExpiry = expiry;
        }

        public bool IsTokenValid()
        {
            if (!IsAuthenticated || TokenExpiry == null)
                return false;

            return DateTime.UtcNow < TokenExpiry.Value;
        }
    }
}
