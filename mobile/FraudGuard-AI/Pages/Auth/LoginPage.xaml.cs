using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        private IAuthenticationService? _authService;

        public LoginPage()
        {
            InitializeComponent();

            // Try resolving auth service. MauiContext may not be ready at this point.
            _authService = TryResolveAuthService();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Resolve again when the page is on screen.
            if (_authService == null)
            {
                _authService = TryResolveAuthService();
                if (_authService == null)
                {
                    ShowError("Không thể khởi tạo dịch vụ đăng nhập. Vui lòng thử lại sau.");
                    return;
                }
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
