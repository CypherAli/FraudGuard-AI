using FraudGuardAI.Services;
using FraudGuardAI.Localization;
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
                if (AuthService == null)
                {
                    ShowError(T("Register_Error_ServiceNotReady"));
                    return;
                }

                ErrorLabel.IsVisible = false;

                var phoneNumber = PhoneEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    ShowError(T("Register_Error_PhoneRequired"));
                    return;
                }

                // Auto-add +84 for Vietnam numbers
                if (!phoneNumber.StartsWith("+"))
                    phoneNumber = "+84" + phoneNumber.TrimStart('0');

                if (!TermsCheckbox.IsChecked)
                {
                    ShowError(T("Register_Error_AcceptTerms"));
                    return;
                }

                SetLoading(true);

                Debug.WriteLine($"[RegisterPage] Registering: {phoneNumber}");

                var verificationId = await AuthService.RegisterAsync(phoneNumber, null);

                Debug.WriteLine($"[RegisterPage] OTP sent. ID: {verificationId}");

                await Navigation.PushAsync(new OtpVerificationPage(verificationId, phoneNumber));
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

        private async void OnBackClicked(object sender, EventArgs e)
            => await Navigation.PopAsync();

        private async void OnLoginTapped(object sender, EventArgs e)
            => await Navigation.PopAsync();

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
            TermsCheckbox.IsEnabled = !isLoading;
        }

        private static string T(string key) => LocalizationResourceManager.Instance[key];
    }
}
