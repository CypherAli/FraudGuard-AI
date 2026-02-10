using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FraudGuardAI.Helpers;

namespace FraudGuardAI.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;

        public BrevoEmailService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var options = await AppConfig.GetBrevoOptionsAsync();
            if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.FromEmail))
            {
                throw new InvalidOperationException("Brevo config is missing (ApiKey/FromEmail)");
            }

            var payload = new
            {
                sender = new { email = options.FromEmail, name = options.FromName },
                to = new[] { new { email = toEmail } },
                subject = "Your FraudGuard OTP Code",
                textContent = $"Your OTP code is: {otpCode}. It expires in 5 minutes."
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", options.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}
