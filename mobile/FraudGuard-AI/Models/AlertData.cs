using System;

namespace FraudGuardAI.Models
{
    /// <summary>
    /// Represents data received from the fraud detection alert
    /// </summary>
    public class AlertData
    {
        public string AlertType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Transcript { get; set; } = string.Empty;
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }
}
