using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI
{
    public partial class App : Application
    {
        private static AudioStreamingServiceLowLevel _audioService;
        private IAuthenticationService _authService;
        
        public App()
        {
            InitializeComponent();

            // Initialize shared audio service (singleton)
            _audioService = new AudioStreamingServiceLowLevel();

            // Set a temporary loading page first
            // Authentication check will happen in OnStart when services are available
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#0D1B2A"),
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = 
                    {
                        new ActivityIndicator 
                        { 
                            IsRunning = true, 
                            Color = Color.FromArgb("#14B8A6"),
                            HeightRequest = 50,
                            WidthRequest = 50
                        },
                        new Label 
                        { 
                            Text = "Đang tải...",
                            TextColor = Color.FromArgb("#E0E6ED"),
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 16, 0, 0)
                        }
                    }
                }
            };
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await CheckAuthenticationAndNavigate();
        }

        private async Task CheckAuthenticationAndNavigate()
        {
            try
            {
                // Get authentication service - now safe to access
                _authService = Handler?.MauiContext?.Services.GetService<IAuthenticationService>();
                
                if (_authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[App] Auth service not available, defaulting to login");
                    MainPage = new NavigationPage(new LoginPage())
                    {
                        BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                        BarTextColor = Color.FromArgb("#E0E6ED")
                    };
                    return;
                }

                // Check if user is authenticated
                var isAuthenticated = await _authService.IsAuthenticatedAsync();

                if (isAuthenticated)
                {
                    // User is logged in, go to main app
                    System.Diagnostics.Debug.WriteLine("[App] User is authenticated, navigating to AppShell");
                    MainPage = new AppShell();
                }
                else
                {
                    // User is not logged in, go to login page
                    System.Diagnostics.Debug.WriteLine("[App] User is not authenticated, navigating to LoginPage");
                    MainPage = new NavigationPage(new LoginPage())
                    {
                        BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                        BarTextColor = Color.FromArgb("#E0E6ED")
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error checking authentication: {ex.Message}");
                // On error, default to login page
                MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                    BarTextColor = Color.FromArgb("#E0E6ED")
                };
            }
        }
        
        /// <summary>
        /// Get shared audio service instance (singleton pattern)
        /// This ensures connection persists across tab navigation
        /// </summary>
        public static AudioStreamingServiceLowLevel GetAudioService()
        {
            if (_audioService == null)
            {
                _audioService = new AudioStreamingServiceLowLevel();
            }
            return _audioService;
        }
        
        /// <summary>
        /// Stop service when app is closing
        /// </summary>
        public static async Task CleanupAudioService()
        {
            if (_audioService != null)
            {
                await _audioService.StopStreamingAsync();
                _audioService.Dispose();
                _audioService = null;
            }
        }
        
        protected override void OnSleep()
        {
            base.OnSleep();
            // App is going to background (Home button pressed)
            // ✅ KHÔNG NGẮT CONNECTION - Giữ nguyên để app chạy background
            System.Diagnostics.Debug.WriteLine("[App] OnSleep - App entering background, connection MAINTAINED");
            
            // Connection continues running in background
            // Only way to stop is pressing Stop Protection button
        }
        
        protected override void OnResume()
        {
            base.OnResume();
            // App is returning from background
            System.Diagnostics.Debug.WriteLine("[App] OnResume - App returning to foreground");
            
            // Check if service is still streaming
            if (_audioService != null)
            {
                System.Diagnostics.Debug.WriteLine($"[App] OnResume - Service status: Streaming={_audioService.IsStreaming}, Connected={_audioService.IsConnected}");
            }
        }
    }
}
