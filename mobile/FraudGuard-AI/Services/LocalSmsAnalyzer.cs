using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Local SMS fraud analyzer using keyword matching.
    /// Subset of backend keyword detection adapted for SMS content.
    /// </summary>
    public static class LocalSmsAnalyzer
    {
        // Keyword categories with weights (Vietnamese + English)
        private static readonly List<(string Pattern, int Weight, string Category)> FraudPatterns = new()
        {
            // CRITICAL — Impersonation & authority (Vietnamese)
            ("c\u00f4ng an", 30, "CRITICAL: Authority impersonation"),
            ("c\u1ea3nh s\u00e1t", 30, "CRITICAL: Authority impersonation"),
            ("vi\u1ec7n ki\u1ec3m s\u00e1t", 30, "CRITICAL: Authority impersonation"),
            ("t\u00f2a \u00e1n", 25, "CRITICAL: Authority impersonation"),
            ("thu\u1ebf v\u1ee5", 25, "CRITICAL: Authority impersonation"),
            ("b\u1ed9 c\u00f4ng an", 35, "CRITICAL: Authority impersonation"),
            ("ng\u00e2n h\u00e0ng nh\u00e0 n\u01b0\u1edbc", 30, "CRITICAL: Authority impersonation"),

            // CRITICAL — Money transfer
            ("chuy\u1ec3n ti\u1ec1n", 35, "CRITICAL: Money transfer request"),
            ("chuy\u1ec3n kho\u1ea3n", 35, "CRITICAL: Money transfer request"),
            ("n\u1ed9p ti\u1ec1n", 30, "CRITICAL: Money deposit request"),
            ("s\u1ed1 t\u00e0i kho\u1ea3n", 25, "CRITICAL: Bank account request"),
            ("m\u00e3 otp", 30, "CRITICAL: OTP theft attempt"),
            ("m\u00e3 x\u00e1c nh\u1eadn", 25, "CRITICAL: Verification code theft"),

            // WARNING — Urgency & pressure
            ("kh\u1ea9n c\u1ea5p", 20, "WARNING: Urgency pressure"),
            ("ngay l\u1eadp t\u1ee9c", 20, "WARNING: Urgency pressure"),
            ("trong v\u00f2ng.*gi\u1edd", 20, "WARNING: Time pressure"),
            ("n\u1ebfu kh\u00f4ng", 15, "WARNING: Threat language"),
            ("b\u1eaft gi\u1eef", 25, "WARNING: Arrest threat"),
            ("ph\u1ea1t ti\u1ec1n", 20, "WARNING: Fine threat"),
            ("gi\u1ea5y vi ph\u1ea1m", 20, "WARNING: Violation document"),
            ("l\u1ec7nh b\u1eaft", 25, "WARNING: Arrest warrant"),

            // SUSPICIOUS — Prize/reward scams
            ("tr\u00fang th\u01b0\u1edfng", 20, "SUSPICIOUS: Prize scam"),
            ("nh\u1eadn th\u01b0\u1edfng", 20, "SUSPICIOUS: Prize scam"),
            ("qu\u00e0 t\u1eb7ng", 15, "SUSPICIOUS: Gift scam"),
            ("mi\u1ec5n ph\u00ed", 10, "SUSPICIOUS: Free offer"),
            ("khuy\u1ebfn m\u00e3i", 10, "SUSPICIOUS: Promotion scam"),

            // SUSPICIOUS — Link phishing
            ("nh\u1ea5p v\u00e0o link", 25, "SUSPICIOUS: Phishing link"),
            ("click v\u00e0o", 20, "SUSPICIOUS: Phishing link"),
            ("truy c\u1eadp", 15, "SUSPICIOUS: Phishing link"),
            ("x\u00e1c minh t\u00e0i kho\u1ea3n", 25, "SUSPICIOUS: Account verification scam"),
            ("c\u1eadp nh\u1eadt th\u00f4ng tin", 20, "SUSPICIOUS: Info update scam"),

            // English keywords
            ("police", 25, "CRITICAL: Authority impersonation"),
            ("arrest warrant", 30, "CRITICAL: Authority impersonation"),
            ("transfer money", 30, "CRITICAL: Money transfer request"),
            ("wire transfer", 30, "CRITICAL: Money transfer request"),
            ("account number", 25, "CRITICAL: Bank account request"),
            ("otp code", 30, "CRITICAL: OTP theft attempt"),
            ("verification code", 25, "CRITICAL: Verification code theft"),
            ("urgent", 20, "WARNING: Urgency pressure"),
            ("immediately", 20, "WARNING: Urgency pressure"),
            ("you have won", 20, "SUSPICIOUS: Prize scam"),
            ("click here", 20, "SUSPICIOUS: Phishing link"),
            ("verify your account", 25, "SUSPICIOUS: Account verification scam"),
        };

        // Known phishing URL patterns
        private static readonly Regex SuspiciousUrlRegex = new(
            @"(https?://)?[\w\-]+(\.[\w\-]+)*(\.tk|\.ml|\.ga|\.cf|\.gq|\.xyz|\.top|\.buzz|\.click|\.link|bit\.ly|tinyurl|t\.co)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Analyze an SMS body for fraud indicators
        /// </summary>
        public static SmsAnalysisResult Analyze(string smsBody)
        {
            if (string.IsNullOrWhiteSpace(smsBody))
                return new SmsAnalysisResult { IsSuspicious = false, Score = 0 };

            string lower = smsBody.ToLowerInvariant();
            int totalScore = 0;
            var detectedPatterns = new List<string>();

            // Check keyword patterns
            foreach (var (pattern, weight, category) in FraudPatterns)
            {
                try
                {
                    if (Regex.IsMatch(lower, pattern, RegexOptions.IgnoreCase))
                    {
                        totalScore += weight;
                        if (!detectedPatterns.Contains(category))
                            detectedPatterns.Add(category);
                    }
                }
                catch
                {
                    // Skip invalid regex patterns
                    if (lower.Contains(pattern))
                    {
                        totalScore += weight;
                        if (!detectedPatterns.Contains(category))
                            detectedPatterns.Add(category);
                    }
                }
            }

            // Check suspicious URLs
            if (SuspiciousUrlRegex.IsMatch(smsBody))
            {
                totalScore += 25;
                detectedPatterns.Add("SUSPICIOUS: Contains phishing URL");
            }

            // Cap at 100
            totalScore = Math.Min(totalScore, 100);

            return new SmsAnalysisResult
            {
                IsSuspicious = totalScore >= 30,
                Score = totalScore,
                DetectedPatterns = detectedPatterns,
                RiskLevel = totalScore >= 80 ? "CRITICAL" :
                           totalScore >= 60 ? "HIGH" :
                           totalScore >= 30 ? "MEDIUM" : "LOW"
            };
        }
    }

    public class SmsAnalysisResult
    {
        public bool IsSuspicious { get; set; }
        public int Score { get; set; }
        public string RiskLevel { get; set; } = "LOW";
        public List<string> DetectedPatterns { get; set; } = new();
    }
}
