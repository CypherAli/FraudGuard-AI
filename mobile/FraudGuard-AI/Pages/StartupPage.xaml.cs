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
                StatusLabel.Text = "Đang khởi tạo ứng dụng...";
                await Task.Delay(500); // Give UI time to render

                // Step 1: Check MauiContext
                Debug.WriteLine("[StartupPage] Checking MauiContext...");
                StatusLabel.Text = "Kiểm tra môi trường...";
                await Task.Delay(300);

                if (Application.Current?.Handler?.MauiContext == null)
                {
                    throw new Exception("MauiContext chưa sẵn sàng");
                }

                // Step 2: Resolve authentication service
                Debug.WriteLine("[StartupPage] Resolving authentication service...");
                StatusLabel.Text = "Đang tải dịch vụ xác thực...";
                await Task.Delay(300);

                var authService = Application.Current.Handler.MauiContext.Services
                    .GetService<IAuthenticationService>();

                if (authService == null)
                {
                    throw new Exception("Không thể tải dịch vụ xác thực");
                }

                Debug.WriteLine("[StartupPage] ✅ Authentication service loaded");

                // Step 3: Check if already authenticated
                Debug.WriteLine("[StartupPage] Checking authentication status...");
                StatusLabel.Text = "Kiểm tra trạng thái đăng nhập...";
                await Task.Delay(300);

                var isAuthenticated = await authService.IsAuthenticatedAsync();

                if (isAuthenticated)
                {
                    Debug.WriteLine("[StartupPage] Already authenticated, navigating to AppShell");
                    StatusLabel.Text = "Đã đăng nhập, đang chuyển trang...";
                    await Task.Delay(500);
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    Debug.WriteLine("[StartupPage] Not authenticated, navigating to LoginPage");
                    StatusLabel.Text = "Chuyển đến trang đăng nhập...";
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
                    ErrorLabel.Text = $"{ex.Message}\n\nLần thử: {_retryCount + 1}/{MAX_RETRIES}";
                    
                    if (_retryCount < MAX_RETRIES)
                    {
                        RetryButton.IsVisible = true;
                    }
                    else
                    {
                        ErrorLabel.Text += "\n\nVui lòng khởi động lại ứng dụng";
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
    }
}
