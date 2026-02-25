using FraudGuardAI.Localization;
using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;
using System.Diagnostics;

namespace FraudGuardAI.Pages
{
    public partial class StartupPage : ContentPage
    {
        private int _retryCount = 0;
        private const int MAX_RETRIES = 3;

        public StartupPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                Debug.WriteLine("[StartupPage] Starting app initialization...");
                StatusLabel.Text = T("Startup_Status_InitializingApp");
                await Task.Delay(500); // Give UI time to render

                // Step 0.5: Check onboarding (first launch)
                if (!Preferences.Get("OnboardingCompleted", false))
                {
                    Debug.WriteLine("[StartupPage] First launch — showing onboarding");
                    if (Application.Current != null)
                        Application.Current.MainPage = new OnboardingPage();
                    return;
                }

                // Step 1: Check MauiContext
                Debug.WriteLine("[StartupPage] Checking MauiContext...");
                StatusLabel.Text = T("Startup_Status_CheckingEnvironment");
                await Task.Delay(300);

                if (Application.Current?.Handler?.MauiContext == null)
                {
                    throw new Exception(T("Startup_Error_MauiContextNotReady"));
                }

                // Step 2: Resolve authentication service
                Debug.WriteLine("[StartupPage] Resolving authentication service...");
                StatusLabel.Text = T("Startup_LoadingAuth");
                await Task.Delay(300);

                var authService = Application.Current.Handler.MauiContext.Services
                    .GetService<IAuthenticationService>();

                if (authService == null)
                {
                    throw new Exception(T("Startup_Error_AuthServiceLoadFailed"));
                }

                Debug.WriteLine("[StartupPage] ✅ Authentication service loaded");

                // Step 3: Check if already authenticated
                Debug.WriteLine("[StartupPage] Checking authentication status...");
                StatusLabel.Text = T("Startup_Status_CheckingAuthStatus");
                await Task.Delay(300);

                var isAuthenticated = await authService.IsAuthenticatedAsync();

                if (isAuthenticated)
                {
                    Debug.WriteLine("[StartupPage] Already authenticated, navigating to AppShell");
                    StatusLabel.Text = T("Startup_Status_Authenticated");
                    await Task.Delay(500);
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    Debug.WriteLine("[StartupPage] Not authenticated, navigating to LoginPage");
                    StatusLabel.Text = T("Startup_Status_NavigateLogin");
                    await Task.Delay(500);
                    Application.Current.MainPage = new NavigationPage(new LoginPage())
                    {
                        BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                        BarTextColor = Color.FromArgb("#E0E6ED")
                    };
                }

                Debug.WriteLine("[StartupPage] ✅ Startup completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupPage] ❌ Initialization failed: {ex.Message}");
                Debug.WriteLine($"[StartupPage] Stack trace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorBorder.IsVisible = true;
                    ErrorLabel.Text = string.Format(
                        T("Startup_Error_RetryFormat"),
                        ex.Message,
                        _retryCount + 1,
                        MAX_RETRIES
                    );
                    
                    if (_retryCount < MAX_RETRIES)
                    {
                        RetryButton.IsVisible = true;
                    }
                    else
                    {
                        ErrorLabel.Text += $"\n\n{T("Startup_Error_RestartMessage")}";
                    }
                });
            }
        }

        private async void OnRetryClicked(object sender, EventArgs e)
        {
            _retryCount++;
            ErrorBorder.IsVisible = false;
            RetryButton.IsVisible = false;
            await InitializeAppAsync();
        }

        private static string T(string key)
            => LocalizationResourceManager.Instance[key];
    }
}
