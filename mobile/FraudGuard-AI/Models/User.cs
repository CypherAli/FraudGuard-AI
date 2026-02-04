namespace FraudGuardAI.Models
{
    /// <summary>
    /// User model for authentication
    /// </summary>
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string? Email { get; set; }
        public string? PhotoUrl { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
        }

        public User(string userId, string phoneNumber)
        {
            UserId = userId;
            PhoneNumber = phoneNumber;
            DisplayName = phoneNumber; // Default display name is phone number
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
        }
    }
}
