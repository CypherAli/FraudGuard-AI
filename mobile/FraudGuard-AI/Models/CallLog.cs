using System;

namespace FraudGuardAI.Models
{
    /// <summary>
    /// Model representing a call log entry from the server
    /// Maps to the JSON response from GET /api/history
    /// </summary>
    public class CallLog
    {
        public int Id { get; set; }
        
        public string DeviceId { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        public long DurationSeconds { get; set; }
        
        public int RiskScore { get; set; }
        
        public bool IsFraud { get; set; }
        
        public string Evidence { get; set; }
        
        public DateTime CreatedAt { get; set; }

        // Computed properties for UI display
        public string DurationDisplay => FormatDuration(DurationSeconds);
        
        public string RiskLevelDisplay => IsFraud ? "🚨 NGUY HIỂM" : "✅ An toàn";
        
        public Color CardBackgroundColor => IsFraud 
            ? Color.FromArgb("#FFEBEE") // Light red
            : Color.FromArgb("#E8F5E9"); // Light green
        
        public Color RiskScoreColor => RiskScore >= 80 
            ? Color.FromArgb("#D32F2F") // Dark red
            : RiskScore >= 60 
                ? Color.FromArgb("#FF9800") // Orange
                : Color.FromArgb("#4CAF50"); // Green

        public string TimeDisplay => StartTime.ToString("dd/MM/yyyy HH:mm");

        private string FormatDuration(long seconds)
        {
            if (seconds < 60)
                return $"{seconds}s";
            
            var minutes = seconds / 60;
            var remainingSeconds = seconds % 60;
            
            if (minutes < 60)
                return $"{minutes}m {remainingSeconds}s";
            
            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            return $"{hours}h {remainingMinutes}m";
        }
    }

    /// <summary>
    /// Response wrapper from the API
    /// </summary>
    public class HistoryResponse
    {
        public bool Success { get; set; }
        
        public CallLog[] Data { get; set; }
    }
}
