using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI
{
    public partial class App : Application
    {
        private static AudioStreamingServiceLowLevel _audioService;
        
        public App()
        {
            InitializeComponent();

            // Initialize shared audio service
            _audioService = new AudioStreamingServiceLowLevel();

            // Set default page
            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                BarTextColor = Color.FromArgb("#E0E6ED")
            };
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
