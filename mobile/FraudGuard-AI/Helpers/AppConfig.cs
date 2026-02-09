using System.Text.Json;
using System.IO;
using Microsoft.Maui.Storage;

namespace FraudGuardAI.Helpers
{
    public class BrevoOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "FraudGuard AI";
    }

    public static class AppConfig
    {
        private static BrevoOptions? _cachedBrevo;

        public static async Task<BrevoOptions> GetBrevoOptionsAsync()
        {
            if (_cachedBrevo != null)
                return _cachedBrevo;

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Brevo", out var brevo))
                {
                    _cachedBrevo = new BrevoOptions
                    {
                        ApiKey = brevo.TryGetProperty("ApiKey", out var apiKey)
                            ? apiKey.GetString() ?? string.Empty
                            : string.Empty,
                        FromEmail = brevo.TryGetProperty("FromEmail", out var fromEmail)
                            ? fromEmail.GetString() ?? string.Empty
                            : string.Empty,
                        FromName = brevo.TryGetProperty("FromName", out var fromName)
                            ? fromName.GetString() ?? "FraudGuard AI"
                            : "FraudGuard AI"
                    };
                }
            }
            catch
            {
                _cachedBrevo = new BrevoOptions();
            }

            return _cachedBrevo ?? new BrevoOptions();
        }
    }
}
