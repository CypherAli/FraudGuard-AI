using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages.Auth
{
    public partial class OtpVerificationPage : ContentPage
    {
        private readonly IAuthenticationService _authService;
        private readonly string _verificationId;
        private readonly string _phoneNumber;
        private readonly bool _isRegistration;
        private System.Timers.Timer? _resendTimer;
        private int _resendCountdown = 60;
        private Entry[] _otpEntries;

        public OtpVerificationPage(string verificationId, string phoneNumber, bool isRegistration)
        {
            InitializeComponent();
            
            _verificationId = verificationId;
            _phoneNumber = phoneNumber;
            _isRegistration = isRegistration;

            // Get authentication service from DI
            _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>()
                ?? throw new InvalidOperationException("Authentication service not found");

            // Initialize OTP entries array
            _otpEntries = new[] { Otp1, Otp2, Otp3, Otp4, Otp5, Otp6 };

            // Set phone number label
            PhoneNumberLabel.Text = _phoneNumber;

            // Start resend countdown
            StartResendCountdown();

            // Auto-focus first entry
            Otp1.Focus();
        }

        private void OnOtpDigitChanged(object sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender;
            var currentIndex = Array.IndexOf(_otpEntries, entry);

            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                // Move to next entry
                if (currentIndex < _otpEntries.Length - 1)
                {
                    _otpEntries[currentIndex + 1].Focus();
                }
                else
                {
                    // Last digit entered, auto-verify
                    OnVerifyClicked(sender, EventArgs.Empty);
                }
            }
            else if (string.IsNullOrEmpty(e.NewTextValue) && !string.IsNullOrEmpty(e.OldTextValue))
            {
                // Backspace pressed, move to previous entry
                if (currentIndex > 0)
                {
                    _otpEntries[currentIndex - 1].Focus();
                }
            }
        }

        private async void OnVerifyClicked(object sender, EventArgs e)
        {
            try
            {
                // Hide error message
                ErrorLabel.IsVisible = false;

                // Get OTP code
                var otpCode = string.Concat(_otpEntries.Select(entry => entry.Text ?? ""));

                // Validate OTP
                if (otpCode.Length != 6)
                {
                    ShowError("Vui lòng nhập đầy đủ 6 chữ số");
                    return;
                }

                // Show loading
                SetLoading(true);

                Debug.WriteLine($"[OtpVerificationPage] Verifying OTP: {otpCode}");

                // Verify OTP
                var isValid = await _authService.VerifyOtpAsync(_verificationId, otpCode);

                if (isValid)
                {
                    Debug.WriteLine("[OtpVerificationPage] OTP verified successfully");

                    // Show success message
                    await DisplayAlert("Thành công", 
                        _isRegistration ? "Đăng ký thành công!" : "Đăng nhập thành công!", 
                        "OK");

                    // Navigate to main app
                    Application.Current!.MainPage = new AppShell();
                }
                else
                {
                    ShowError("Mã OTP không đúng");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OtpVerificationPage] Error: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnResendTapped(object sender, EventArgs e)
        {
            try
            {
                if (_resendCountdown > 0)
                {
                    return; // Still in countdown
                }

                // Hide error message
                ErrorLabel.IsVisible = false;

                // Show loading
                SetLoading(true);

                Debug.WriteLine($"[OtpVerificationPage] Resending OTP to {_phoneNumber}");

                // Resend OTP
                var newVerificationId = await _authService.SendOtpAsync(_phoneNumber);

                Debug.WriteLine($"[OtpVerificationPage] OTP resent. New Verification ID: {newVerificationId}");

                // Update verification ID
                typeof(OtpVerificationPage)
                    .GetField("_verificationId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(this, newVerificationId);

                // Clear OTP entries
                foreach (var entry in _otpEntries)
                {
                    entry.Text = string.Empty;
                }

                // Focus first entry
                Otp1.Focus();

                // Restart countdown
                StartResendCountdown();

                // Show success message
                await DisplayAlert("Thành công", "Mã OTP mới đã được gửi", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OtpVerificationPage] Error resending OTP: {ex.Message}");
                ShowError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            _resendTimer?.Stop();
            _resendTimer?.Dispose();
            await Navigation.PopAsync();
        }

        private void StartResendCountdown()
        {
            _resendCountdown = 60;
            ResendLabel.IsEnabled = false;
            ResendLabel.TextColor = Color.FromArgb("#5C6B7A");
            CountdownLabel.IsVisible = true;
            CountdownLabel.Text = $"Gửi lại sau {_resendCountdown}s";

            _resendTimer?.Stop();
            _resendTimer?.Dispose();

            _resendTimer = new System.Timers.Timer(1000);
            _resendTimer.Elapsed += (s, e) =>
            {
                _resendCountdown--;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_resendCountdown > 0)
                    {
                        CountdownLabel.Text = $"Gửi lại sau {_resendCountdown}s";
                    }
                    else
                    {
                        CountdownLabel.IsVisible = false;
                        ResendLabel.IsEnabled = true;
                        ResendLabel.TextColor = Color.FromArgb("#34D399");
                        _resendTimer?.Stop();
                    }
                });
            };
            _resendTimer.Start();
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
            VerifyButton.IsEnabled = !isLoading;
            
            foreach (var entry in _otpEntries)
            {
                entry.IsEnabled = !isLoading;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _resendTimer?.Stop();
            _resendTimer?.Dispose();
        }
    }
}
