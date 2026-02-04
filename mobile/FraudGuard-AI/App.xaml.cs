using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI
{
    public partial class App : Application
    {
        private static AudioStreamingServiceLowLevel _audioService;
        private readonly IAuthenticationService _authService;
        
        public App()
        {
            InitializeComponent();

            // Initialize shared audio service (singleton)
            _audioService = new AudioStreamingServiceLowLevel();

            // Get authentication service
            _authService = Handler?.MauiContext?.Services.GetService<IAuthenticationService>()
                ?? throw new InvalidOperationException("Authentication service not found");

            // Check authentication state and set initial page
            CheckAuthenticationAndNavigate();
        }

        private async void CheckAuthenticationAndNavigate()
        {
            try
            {
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
