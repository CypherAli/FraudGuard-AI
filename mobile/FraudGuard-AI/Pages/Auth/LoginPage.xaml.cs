using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        private IAuthenticationService? _authService;

        private IAuthenticationService? AuthService => 
            _authService ??= Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                // Check if auth service is available
                if (AuthService == null)
                {
                    ShowError("Dịch vụ xác thực chưa sẵn sàng. Vui lòng khởi động lại ứng dụng.");
                    return;
                }

                // Hide error message
                ErrorLabel.IsVisible = false;

                // Get phone number
                var phoneNumber = PhoneEntry.Text?.Trim();

                // Validate input
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    ShowError("Vui lòng nhập số điện thoại");
                    return;
                }

                // Ensure phone number starts with +
                if (!phoneNumber.StartsWith("+"))
                {
                    // Auto-add +84 for Vietnam if not provided
                    phoneNumber = "+84" + phoneNumber.TrimStart('0');
                }

                // Show loading
                SetLoading(true);

                Debug.WriteLine($"[LoginPage] Sending OTP to {phoneNumber}");

                // Send OTP
                var verificationId = await AuthService.LoginAsync(phoneNumber);

                Debug.WriteLine($"[LoginPage] OTP sent. Verification ID: {verificationId}");

                // Navigate to OTP verification page
                await Navigation.PushAsync(new OtpVerificationPage(verificationId, phoneNumber, false));
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
            // Navigate to register page
            await Navigation.PushAsync(new RegisterPage());
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
            RegisterButton.IsEnabled = !isLoading;
            PhoneEntry.IsEnabled = !isLoading;
        }
    }
}
