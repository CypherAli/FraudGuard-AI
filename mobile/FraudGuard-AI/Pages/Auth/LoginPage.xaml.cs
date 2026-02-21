using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        private IAuthenticationService? _authService;

        public LoginPage()
        {
            try
            {
                Debug.WriteLine("[LoginPage] Initializing LoginPage...");
                InitializeComponent();
                Debug.WriteLine("[LoginPage] InitializeComponent completed");

                // Try resolving auth service. MauiContext may not be ready at this point.
                _authService = TryResolveAuthService();
                
                if (_authService != null)
                {
                    Debug.WriteLine("[LoginPage] ✅ Auth service resolved in constructor");
                }
                else
                {
                    Debug.WriteLine("[LoginPage] ⚠️ Auth service not available yet, will retry in OnAppearing");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] ❌ Error in constructor: {ex.Message}");
                // Don't throw - let OnAppearing handle the error
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("[LoginPage] OnAppearing called");

            // Resolve again when the page is on screen.
            if (_authService == null)
            {
                Debug.WriteLine("[LoginPage] Attempting to resolve auth service in OnAppearing...");
                _authService = TryResolveAuthService();
                
                if (_authService == null)
                {
                    Debug.WriteLine("[LoginPage] ❌ Failed to resolve auth service");
                    ShowError("Không thể khởi tạo dịch vụ đăng nhập. Vui lòng khởi động lại ứng dụng.");
                    return;
                }
                
                Debug.WriteLine("[LoginPage] ✅ Auth service resolved in OnAppearing");
            }

            _ = TryAutoLoginAsync();
        }

        private async Task TryAutoLoginAsync()
        {
            try
            {
                if (_authService == null)
                    return;

                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (isAuthenticated)
                {
                    Application.Current!.MainPage = new AppShell();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Auto-login check failed: {ex.Message}");
            }
        }

        private static IAuthenticationService? TryResolveAuthService()
        {
            try
            {
                return Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Auth service resolve failed: {ex.Message}");
                return null;
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                // Hide error message
                ErrorLabel.IsVisible = false;

                if (_authService == null)
                {
                    ShowError("Dịch vụ đăng nhập chưa sẵn sàng. Vui lòng thử lại.");
                    return;
                }

                // Get email
                var email = EmailEntry.Text?.Trim();

                // Validate input
                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("Vui lòng nhập email");
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Email không hợp lệ");
                    return;
                }

                // Show loading
                SetLoading(true);

                Debug.WriteLine($"[LoginPage] Sending OTP to {email}");

                // Send OTP
                var verificationId = await _authService.LoginAsync(email);

                Debug.WriteLine($"[LoginPage] OTP sent. Verification ID: {verificationId}");

                // Navigate to OTP verification page
                await Navigation.PushAsync(new OtpVerificationPage(verificationId, email));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnForgotAccessClicked(object sender, EventArgs e)
        {
            try
            {
                if (_authService == null)
                {
                    ShowError("Dịch vụ đăng nhập chưa sẵn sàng. Vui lòng thử lại.");
                    return;
                }

                string email = await Application.Current.MainPage.DisplayPromptAsync(
                    Localization.LocalizationResourceManager.Instance["Login_RecoverTitle"],
                    Localization.LocalizationResourceManager.Instance["Login_RecoverPrompt"],
                    Localization.LocalizationResourceManager.Instance["Login_RecoverSend"],
                    Localization.LocalizationResourceManager.Instance["Common_Cancel"],
                    placeholder: "example@gmail.com",
                    keyboard: Keyboard.Email);

                if (string.IsNullOrWhiteSpace(email))
                    return;

                if (!IsValidEmail(email))
                {
                    ShowError("Email không hợp lệ");
                    return;
                }

                SetLoading(true);
                Debug.WriteLine($"[LoginPage] Account recovery for: {email}");

                // Reuse the login OTP flow for account recovery
                var verificationId = await _authService.LoginAsync(email);
                Debug.WriteLine($"[LoginPage] Recovery OTP sent. Verification ID: {verificationId}");

                await Navigation.PushAsync(new OtpVerificationPage(verificationId, email));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Recovery error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            if (_authService == null)
            {
                ShowError("Dịch vụ đăng nhập chưa sẵn sàng. Vui lòng thử lại.");
                return;
            }

            // Navigate to register page
            await Navigation.PushAsync(new RegisterPage());
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsRunning = isLoading;
            LoadingIndicator.IsVisible = isLoading;
            LoginButton.IsEnabled = !isLoading;
            EmailEntry.IsEnabled = !isLoading;
        }
    }
}
