using FraudGuardAI.Services;
using System.Diagnostics;

namespace FraudGuardAI.Pages
{
    public partial class PhoneAuthPage : ContentPage
    {
        private readonly IAuthenticationService _authService;
        private string? _verificationId;
        private string? _phoneNumber;

        public PhoneAuthPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        /// <summary>
        /// Send OTP button clicked
        /// </summary>
        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            try
            {
                var phoneNumber = PhoneNumberEntry.Text?.Trim();

                // Validate input
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    await DisplayAlert("Lỗi", "Vui lòng nhập số điện thoại", "OK");
                    return;
                }

                // Show loading
                SendOtpButton.IsEnabled = false;
                SendingIndicator.IsVisible = true;
                SendingIndicator.IsRunning = true;

                Debug.WriteLine($"[PhoneAuth] Sending OTP to: {phoneNumber}");

                // Send OTP via Firebase
                _verificationId = await _authService.SendOtpAsync(phoneNumber);
                _phoneNumber = phoneNumber;

                Debug.WriteLine($"[PhoneAuth] OTP sent. Verification ID: {_verificationId}");

                // Hide phone input, show OTP verification
                PhoneInputFrame.IsVisible = false;
                OtpVerificationFrame.IsVisible = true;

                // Update label with masked phone number
                var maskedPhone = MaskPhoneNumber(phoneNumber);
                OtpSentLabel.Text = $"Mã đã được gửi đến {maskedPhone}";

                // Focus on OTP entry
                OtpCodeEntry.Focus();

                await DisplayAlert("Thành công", 
                    "Mã OTP đã được gửi đến số điện thoại của bạn. Vui lòng kiểm tra tin nhắn SMS.", 
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhoneAuth] Error: {ex.Message}");
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                // Hide loading
                SendOtpButton.IsEnabled = true;
                SendingIndicator.IsVisible = false;
                SendingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Verify OTP button clicked
        /// </summary>
        private async void OnVerifyOtpClicked(object sender, EventArgs e)
        {
            try
            {
                var otpCode = OtpCodeEntry.Text?.Trim();

                // Validate input
                if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length != 6)
                {
                    await DisplayAlert("Lỗi", "Vui lòng nhập đủ 6 chữ số", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_verificationId))
                {
                    await DisplayAlert("Lỗi", "Vui lòng gửi lại mã OTP", "OK");
                    return;
                }

                // Show loading
                VerifyOtpButton.IsEnabled = false;
                VerifyingIndicator.IsVisible = true;
                VerifyingIndicator.IsRunning = true;

                Debug.WriteLine($"[PhoneAuth] Verifying OTP: {otpCode}");

                // Verify OTP
                var success = await _authService.VerifyOtpAsync(_verificationId, otpCode);

                if (success)
                {
                    Debug.WriteLine("[PhoneAuth] OTP verified successfully");

                    await DisplayAlert("Thành công", 
                        "Xác thực thành công! Chào mừng bạn đến với FraudGuard AI.", 
                        "OK");

                    // Navigate to main page
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    await DisplayAlert("Lỗi", "Mã OTP không chính xác", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhoneAuth] Error: {ex.Message}");
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                // Hide loading
                VerifyOtpButton.IsEnabled = true;
                VerifyingIndicator.IsVisible = false;
                VerifyingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Resend OTP button clicked
        /// </summary>
        private async void OnResendOtpClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_phoneNumber))
            {
                await DisplayAlert("Lỗi", "Không tìm thấy số điện thoại", "OK");
                return;
            }

            try
            {
                ResendOtpButton.IsEnabled = false;

                Debug.WriteLine($"[PhoneAuth] Resending OTP to: {_phoneNumber}");

                // Resend OTP
                _verificationId = await _authService.SendOtpAsync(_phoneNumber);

                await DisplayAlert("Thành công", "Mã OTP mới đã được gửi", "OK");

                // Clear OTP entry
                OtpCodeEntry.Text = string.Empty;
                OtpCodeEntry.Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhoneAuth] Resend error: {ex.Message}");
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                ResendOtpButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Change phone number button clicked
        /// </summary>
        private void OnChangePhoneClicked(object sender, EventArgs e)
        {
            // Reset and go back to phone input
            OtpVerificationFrame.IsVisible = false;
            PhoneInputFrame.IsVisible = true;
            
            OtpCodeEntry.Text = string.Empty;
            _verificationId = null;
            
            PhoneNumberEntry.Focus();
        }

        /// <summary>
        /// Mask phone number for display (e.g., +84 xxx xxx 789)
        /// </summary>
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
                return phoneNumber;

            // Keep country code and last 3 digits
            var countryCode = phoneNumber.Substring(0, 3); // +84
            var lastDigits = phoneNumber.Substring(phoneNumber.Length - 3); // last 3
            
            return $"{countryCode} xxx xxx {lastDigits}";
        }
    }
}
