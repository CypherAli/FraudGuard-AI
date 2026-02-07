using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class RegisterPage : ContentPage
    {
        private IAuthenticationService? _authService;

        private IAuthenticationService? AuthService => 
            _authService ??= Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>();

        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
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

                // Get input values
                var phoneNumber = PhoneEntry.Text?.Trim();
                var password = PasswordEntry.Text?.Trim();
                var confirmPassword = ConfirmPasswordEntry.Text?.Trim();

                // Validate inputs
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

                // Validate password if provided
                if (!string.IsNullOrWhiteSpace(password))
                {
                    if (password.Length < 6)
                    {
                        ShowError("Mật khẩu phải có ít nhất 6 ký tự");
                        return;
                    }

                    if (password != confirmPassword)
                    {
                        ShowError("Mật khẩu xác nhận không khớp");
                        return;
                    }
                }

                // Check terms acceptance
                if (!TermsCheckbox.IsChecked)
                {
                    ShowError("Vui lòng đồng ý với Điều khoản sử dụng và Chính sách bảo mật");
                    return;
                }

                // Show loading
                SetLoading(true);

                Debug.WriteLine($"[RegisterPage] Registering user: {phoneNumber}");

                // Register user (send OTP)
                var verificationId = await AuthService.RegisterAsync(phoneNumber, password);

                Debug.WriteLine($"[RegisterPage] OTP sent. Verification ID: {verificationId}");

                // Navigate to OTP verification page
                await Navigation.PushAsync(new OtpVerificationPage(verificationId, phoneNumber, true));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RegisterPage] Error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void OnPasswordChanged(object sender, TextChangedEventArgs e)
        {
            var password = e.NewTextValue;
            
            if (string.IsNullOrWhiteSpace(password))
            {
                PasswordStrengthLabel.IsVisible = false;
                return;
            }

            // Calculate password strength
            var strength = CalculatePasswordStrength(password);
            PasswordStrengthLabel.IsVisible = true;
            
            switch (strength)
            {
                case 0:
                    PasswordStrengthLabel.Text = "Mật khẩu yếu";
                    PasswordStrengthLabel.TextColor = Color.FromArgb("#EF4444");
                    break;
                case 1:
                    PasswordStrengthLabel.Text = "Mật khẩu trung bình";
                    PasswordStrengthLabel.TextColor = Color.FromArgb("#F59E0B");
                    break;
                case 2:
                    PasswordStrengthLabel.Text = "Mật khẩu mạnh";
                    PasswordStrengthLabel.TextColor = Color.FromArgb("#34D399");
                    break;
            }
        }

        private int CalculatePasswordStrength(string password)
        {
            var score = 0;
            
            if (password.Length >= 8) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
            if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;

            if (score <= 1) return 0; // Weak
            if (score <= 2) return 1; // Medium
            return 2; // Strong
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
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
            RegisterButton.IsEnabled = !isLoading;
            PhoneEntry.IsEnabled = !isLoading;
            PasswordEntry.IsEnabled = !isLoading;
            ConfirmPasswordEntry.IsEnabled = !isLoading;
            TermsCheckbox.IsEnabled = !isLoading;
        }
    }
}
