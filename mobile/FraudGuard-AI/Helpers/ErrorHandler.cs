using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FraudGuardAI.Helpers
{
    /// <summary>
    /// Centralized error handling and user-friendly error messages
    /// </summary>
    public static class ErrorHandler
    {
        public enum ErrorType
        {
            Timeout,
            ConnectionRefused,
            ServerUnreachable,
            PermissionDenied,
            NetworkUnavailable,
            Unknown
        }

        public class ErrorInfo
        {
            public ErrorType Type { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string ActionHint { get; set; }
            public bool CanRetry { get; set; }
        }

        /// <summary>
        /// Classify exception into user-friendly error type
        /// </summary>
        public static ErrorInfo ClassifyError(Exception ex)
        {
            return ex switch
            {
                TimeoutException => new ErrorInfo
                {
                    Type = ErrorType.Timeout,
                    Title = "Connection Timeout",
                    Message = "The server is taking too long to respond.",
                    ActionHint = "• Check your internet connection\n• Verify server is running\n• Try again in a moment",
                    CanRetry = true
                },
                WebSocketException wse => new ErrorInfo
                {
                    Type = ErrorType.ConnectionRefused,
                    Title = "Cannot Connect",
                    Message = "Unable to establish connection to the server.",
                    ActionHint = "• Check server IP address in Settings\n• Ensure server is running\n• Verify firewall settings",
                    CanRetry = true
                },
                HttpRequestException hre when hre.StatusCode == HttpStatusCode.NotFound => new ErrorInfo
                {
                    Type = ErrorType.ServerUnreachable,
                    Title = "Server Not Found",
                    Message = "Cannot reach the fraud protection server.",
                    ActionHint = "• Verify IP address is correct\n• Check if server is running\n• Ensure both devices on same network",
                    CanRetry = true
                },
                HttpRequestException => new ErrorInfo
                {
                    Type = ErrorType.NetworkUnavailable,
                    Title = "Network Error",
                    Message = "Unable to access network.",
                    ActionHint = "• Check WiFi/mobile data\n• Try switching networks\n• Restart your device",
                    CanRetry = true
                },
                UnauthorizedAccessException => new ErrorInfo
                {
                    Type = ErrorType.PermissionDenied,
                    Title = "Permission Required",
                    Message = "Microphone access is needed to analyze calls.",
                    ActionHint = "• Go to Settings → Apps → FraudGuard\n• Enable Microphone permission",
                    CanRetry = false
                },
                _ => new ErrorInfo
                {
                    Type = ErrorType.Unknown,
                    Title = "Unexpected Error",
                    Message = ex.Message,
                    ActionHint = "• Restart the app\n• Check for updates\n• Contact support if issue persists",
                    CanRetry = true
                }
            };
        }

        /// <summary>
        /// Show error dialog with retry option
        /// </summary>
        public static async Task<bool> ShowErrorWithRetry(Exception ex)
        {
            var errorInfo = ClassifyError(ex);
            return await ShowErrorDialog(errorInfo);
        }

        /// <summary>
        /// Show error dialog from ErrorInfo
        /// </summary>
        public static async Task<bool> ShowErrorDialog(ErrorInfo errorInfo)
        {
            if (Application.Current?.MainPage == null)
                return false;

            string message = $"{errorInfo.Message}\n\n{errorInfo.ActionHint}";

            if (errorInfo.CanRetry)
            {
                return await Application.Current.MainPage.DisplayAlert(
                    errorInfo.Title,
                    message,
                    "Retry",
                    "Cancel"
                );
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    errorInfo.Title,
                    message,
                    "OK"
                );
                return false;
            }
        }

        /// <summary>
        /// Execute action with retry logic
        /// </summary>
        public static async Task<bool> ExecuteWithRetry<T>(
            Func<Task<T>> action,
            Func<T, bool> validateResult,
            int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var result = await action();
                    if (validateResult(result))
                        return true;
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1)
                    {
                        // Last attempt failed
                        return await ShowErrorWithRetry(ex);
                    }

                    // Wait before retry (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }

            return false;
        }

        /// <summary>
        /// Log error for debugging
        /// </summary>
        public static void LogError(string context, Exception ex)
        {
            var errorInfo = ClassifyError(ex);
            System.Diagnostics.Debug.WriteLine($"[ERROR] {context}:");
            System.Diagnostics.Debug.WriteLine($"  Type: {errorInfo.Type}");
            System.Diagnostics.Debug.WriteLine($"  Title: {errorInfo.Title}");
            System.Diagnostics.Debug.WriteLine($"  Message: {errorInfo.Message}");
            System.Diagnostics.Debug.WriteLine($"  Exception: {ex}");
        }
    }
}
