using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        private IAuthenticationService? _authService;
        private string? _pendingEmail;
        private DateTime _otpSentTime;
        private IDispatcherTimer? _countdownTimer;

        private IAuthenticationService? AuthService => 
            _authService ??= Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>();

        public LoginPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Check if user is already authenticated (persistent login)
            try
            {
                if (AuthService != null && await AuthService.IsAuthenticatedAsync())
                {
                    Debug.WriteLine("[LoginPage] User already authenticated, navigating to main");
                    Application.Current!.MainPage = new AppShell();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Auth check error: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopCountdownTimer();
        }

        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            await SendOtp();
        }

        private async void OnResendOtpClicked(object sender, EventArgs e)
        {
            await SendOtp();
        }

        private async Task SendOtp()
        {
            try
            {
                if (AuthService == null)
                {
                    ShowError("Dịch vụ xác thực chưa sẵn sàng. Vui lòng khởi động lại ứng dụng.");
                    return;
                }

                HideError();

                var email = EmailEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("Vui lòng nhập email");
                    return;
                }

                if (!email.Contains("@") || !email.Contains("."))
                {
                    ShowError("Email không hợp lệ");
                    return;
                }

                SetLoading(true);

                Debug.WriteLine($"[LoginPage] Sending OTP to: {email}");

                var success = await AuthService.SendOtpAsync(email);

                if (success)
                {
                    _pendingEmail = email;
                    _otpSentTime = DateTime.Now;

                    // Show OTP section
                    EmailSection.IsVisible = false;
                    OtpSection.IsVisible = true;
                    OtpSentToLabel.Text = $"Kiểm tra email {email}";
                    OtpEntry.Text = "";
                    OtpEntry.Focus();

                    // Start countdown timer
                    StartCountdownTimer();

                    Debug.WriteLine($"[LoginPage] OTP sent successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] SendOtp error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnVerifyOtpClicked(object sender, EventArgs e)
        {
            try
            {
                if (AuthService == null)
                {
                    ShowError("Dịch vụ xác thực chưa sẵn sàng.");
                    return;
                }

                HideError();

                var otp = OtpEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(otp))
                {
                    ShowError("Vui lòng nhập mã OTP");
                    return;
                }

                if (otp.Length != 6 || !otp.All(char.IsDigit))
                {
                    ShowError("Mã OTP phải là 6 chữ số");
                    return;
                }

                if (string.IsNullOrEmpty(_pendingEmail))
                {
                    ShowError("Email không hợp lệ. Vui lòng thử lại.");
                    OnChangeEmailClicked(sender, e);
                    return;
                }

                SetLoading(true);

                Debug.WriteLine($"[LoginPage] Verifying OTP for: {_pendingEmail}");

                var success = await AuthService.VerifyOtpAsync(_pendingEmail, otp);

                if (success)
                {
                    Debug.WriteLine("[LoginPage] OTP verified successfully!");
                    StopCountdownTimer();

                    await DisplayAlert("🎉 Thành công!", 
                        "Đăng nhập thành công!\nChào mừng bạn đến với FraudGuard AI.", 
                        "Bắt đầu");

                    // Navigate to main page
                    Application.Current!.MainPage = new AppShell();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] VerifyOtp error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void OnChangeEmailClicked(object sender, EventArgs e)
        {
            StopCountdownTimer();
            OtpSection.IsVisible = false;
            EmailSection.IsVisible = true;
            EmailEntry.Focus();
            HideError();
        }

        private void StartCountdownTimer()
        {
            StopCountdownTimer();

            _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_countdownTimer != null)
            {
                _countdownTimer.Interval = TimeSpan.FromSeconds(1);
                _countdownTimer.Tick += UpdateCountdown;
                _countdownTimer.Start();
            }
        }

        private void StopCountdownTimer()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }
        }

        private void UpdateCountdown(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _otpSentTime;
            var remaining = TimeSpan.FromMinutes(5) - elapsed;

            if (remaining.TotalSeconds <= 0)
            {
                CountdownLabel.Text = "⚠️ Mã OTP đã hết hạn";
                CountdownLabel.TextColor = Color.FromArgb("#EF4444");
                ResendLabel.TextColor = Color.FromArgb("#34D399");
                StopCountdownTimer();
            }
            else
            {
                CountdownLabel.Text = $"Mã hết hạn sau: {remaining.Minutes}:{remaining.Seconds:D2}";
                CountdownLabel.TextColor = Color.FromArgb("#8B95A5");
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private void HideError()
        {
            ErrorLabel.IsVisible = false;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsRunning = isLoading;
            LoadingIndicator.IsVisible = isLoading;
            SendOtpButton.IsEnabled = !isLoading;
            VerifyOtpButton.IsEnabled = !isLoading;
            EmailEntry.IsEnabled = !isLoading;
            OtpEntry.IsEnabled = !isLoading;
        }
    }
}
