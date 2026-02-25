using Android.App;
using Android.Content;
using Android.Provider;
using Android.Telephony;
using FraudGuardAI.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// BroadcastReceiver to intercept incoming SMS and analyze for fraud patterns.
    /// Only active when SMS Detection is enabled in Settings.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Telephony.Sms.Intents.SmsReceivedAction })]
    public class SmsReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != Telephony.Sms.Intents.SmsReceivedAction)
                return;

            try
            {
                // Check if SMS Detection is enabled
                bool smsEnabled = Preferences.Get("SmsDetectionEnabled", false);
                if (!smsEnabled)
                {
                    Debug.WriteLine("[SmsReceiver] SMS detection disabled, skipping");
                    return;
                }

                // Parse SMS from PDU
                var bundle = intent.Extras;
                if (bundle == null) return;

                var pdusObj = bundle.Get("pdus");
                if (pdusObj == null) return;
#pragma warning disable CS8600
                var pdus = (Java.Lang.Object[])pdusObj;
#pragma warning restore CS8600
                if (pdus == null || pdus.Length == 0) return;

                string format = intent.GetStringExtra("format") ?? "3gpp";
                var messageBuilder = new StringBuilder();
                string? senderAddress = null;

                foreach (var pdu in pdus)
                {
                    byte[]? pduBytes = null;
                    try { pduBytes = (byte[])(object)pdu; } catch { continue; }
                    if (pduBytes == null) continue;
                    var smsMessage = global::Android.Telephony.SmsMessage.CreateFromPdu(pduBytes, format);
                    if (smsMessage != null)
                    {
                        messageBuilder.Append(smsMessage.MessageBody);
                        senderAddress ??= smsMessage.OriginatingAddress;
                    }
                }

                string fullMessage = messageBuilder.ToString();
                string sender = senderAddress ?? "Unknown";

                Debug.WriteLine($"[SmsReceiver] SMS from {sender}: {fullMessage.Substring(0, Math.Min(100, fullMessage.Length))}...");

                // Check if sender is whitelisted
                if (ContactWhitelistService.Instance.IsWhitelisted(sender))
                {
                    Debug.WriteLine($"[SmsReceiver] Sender {sender} is whitelisted, skipping");
                    return;
                }

                // Analyze SMS content
                var result = LocalSmsAnalyzer.Analyze(fullMessage);

                if (result.IsSuspicious)
                {
                    Debug.WriteLine($"[SmsReceiver] SUSPICIOUS SMS detected! Score: {result.Score}, Level: {result.RiskLevel}");
                    if (context != null) ShowSmsAlert(context, sender, result);
                }
                else
                {
                    Debug.WriteLine($"[SmsReceiver] SMS is clean (score: {result.Score})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SmsReceiver] Error processing SMS: {ex.Message}");
            }
        }

        private void ShowSmsAlert(Context context, string sender, SmsAnalysisResult result)
        {
            try
            {
                string patterns = result.DetectedPatterns.Count > 0
                    ? string.Join(", ", result.DetectedPatterns.Take(3))
                    : "Suspicious content";

                string alertMessage = $"Suspicious SMS from {sender}\n" +
                                     $"Risk: {result.RiskLevel} ({result.Score}%)\n" +
                                     $"Patterns: {patterns}";

                AlertNotificationHelper.ShowFraudAlert(
                    context,
                    result.RiskLevel,
                    result.Score,
                    alertMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SmsReceiver] Alert error: {ex.Message}");
            }
        }
    }
}
