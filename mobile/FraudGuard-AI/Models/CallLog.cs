using System;
using System.Text.Json.Serialization;

namespace FraudGuardAI.Models
{
    /// <summary>
    /// Model representing a call log entry from the server
    /// Maps to the JSON response from GET /api/history
    /// Backend returns snake_case keys — [JsonPropertyName] ensures correct deserialization
    /// even though ReadFromJsonAsync uses PropertyNameCaseInsensitive (which ignores case
    /// but NOT underscores, so "risk_score" ≠ "riskscore" without these attributes).
    /// </summary>
    public class CallLog
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long DurationSeconds { get; set; }

        [JsonPropertyName("risk_score")]
        public int RiskScore { get; set; }

        [JsonPropertyName("deepfake_score")]
        public int DeepfakeScore { get; set; }

        [JsonPropertyName("is_fraud")]
        public bool IsFraud { get; set; }

        [JsonPropertyName("evidence")]
        public string? Evidence { get; set; }

        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        // Computed: parse detected keywords from Evidence field
        public List<string> DetectedKeywords
        {
            get
            {
                var keywords = new List<string>();
                if (string.IsNullOrEmpty(Evidence)) return keywords;
                // Evidence format: "Patterns: CRITICAL: keyword (+50); WARNING: keyword (+25) | Transcript: ..."
                var parts = Evidence.Split('|');
                if (parts.Length > 0 && parts[0].Contains("Patterns:"))
                {
                    var patternSection = parts[0].Replace("Patterns:", "").Trim();
                    foreach (var p in patternSection.Split(';'))
                    {
                        var trimmed = p.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            keywords.Add(trimmed);
                    }
                }
                return keywords;
            }
        }

        public string AlertTypeDisplay => RiskScore >= 80 ? "CRITICAL"
            : RiskScore >= 60 ? "HIGH"
            : RiskScore >= 40 ? "MEDIUM"
            : RiskScore >= 15 ? "LOW" : "SAFE";

        // Additional properties for stats calculation
        /// <summary>
        /// Confidence score from 0.0 to 1.0 (e.g., 0.85 = 85%)
        /// Computed from RiskScore if not provided by API
        /// </summary>
        public double Confidence => RiskScore / 100.0;
        
        /// <summary>
        /// Alias for StartTime, used for filtering by date
        /// </summary>
        public DateTime Timestamp => StartTime;

        // Computed properties for UI display
        public string DurationDisplay => FormatDuration(DurationSeconds);

        /// <summary>
        /// Returns the granular risk level: CRITICAL / HIGH / MEDIUM / LOW / SAFE
        /// (based on AlertTypeDisplay, not the old binary DANGEROUS/SAFE)
        /// </summary>
        public string RiskLevelDisplay => AlertTypeDisplay;

        public Color CardBackgroundColor => IsFraud
            ? Color.FromArgb("#1AEF4444")  // Dark red tint
            : Color.FromArgb("#1A2DD4BF"); // Dark teal tint

        /// <summary>
        /// Dark-theme palette: matches the dashboard accent colors exactly.
        /// Critical ≥ 80  → Red    #EF4444
        /// High     ≥ 60  → Orange #F97316
        /// Medium   ≥ 40  → Amber  #FBBF24
        /// Low      ≥ 15  → Blue   #60A5FA
        /// Safe      &lt; 15 → Teal   #2DD4BF
        /// </summary>
        public Color RiskScoreColor => RiskScore >= 80
            ? Color.FromArgb("#EF4444")
            : RiskScore >= 60
                ? Color.FromArgb("#F97316")
                : RiskScore >= 40
                    ? Color.FromArgb("#FBBF24")
                    : RiskScore >= 15
                        ? Color.FromArgb("#60A5FA")
                        : Color.FromArgb("#2DD4BF");

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

        public CallLog[]? Data { get; set; }
    }

    public class CallDetailResponse
    {
        public bool Success { get; set; }
        public CallLog? Data { get; set; }
    }
}
