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
            try
            {
                System.Diagnostics.Debug.WriteLine("[App] Initializing App...");
                InitializeComponent();

                // Initialize shared audio service (singleton) with error handling
                try
                {
                    _audioService = new AudioStreamingServiceLowLevel();
                    System.Diagnostics.Debug.WriteLine("[App] Audio service initialized");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Audio service init error: {ex.Message}");
                    // Continue without audio service
                }

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
                
                System.Diagnostics.Debug.WriteLine("[App] App initialized successfully");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] CRITICAL ERROR in constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
                
                // Show error page instead of crashing
                MainPage = CreateErrorPage("Lỗi khởi tạo", ex.Message);
            }
        }

        protected override async void OnStart()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[App] OnStart called");
                base.OnStart();
                await CheckAuthenticationAndNavigate();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] OnStart error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
                
                // Show error page
                MainPage = CreateErrorPage("Lỗi khởi động", ex.Message);
            }
        }

        private async Task CheckAuthenticationAndNavigate()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[App] Checking authentication...");
                
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
                System.Diagnostics.Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
                
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
            try
            {
                if (_audioService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[App] Creating new audio service instance");
                    _audioService = new AudioStreamingServiceLowLevel();
                }
                return _audioService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error creating audio service: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stop service when app is closing
        /// </summary>
        public static async Task CleanupAudioService()
        {
            try
            {
                if (_audioService != null)
                {
                    await _audioService.StopStreamingAsync();
                    _audioService.Dispose();
                    _audioService = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error cleaning up audio service: {ex.Message}");
            }
        }
        
        private ContentPage CreateErrorPage(string title, string message)
        {
            return new ContentPage
            {
                BackgroundColor = Color.FromArgb("#0D1B2A"),
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 16,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "⚠️",
                            FontSize = 48,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = title,
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#EF4444"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = message,
                            FontSize = 14,
                            TextColor = Color.FromArgb("#E0E6ED"),
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Button
                        {
                            Text = "Thử lại",
                            BackgroundColor = Color.FromArgb("#14B8A6"),
                            TextColor = Colors.White,
                            CornerRadius = 8,
                            Margin = new Thickness(0, 20, 0, 0),
                            Command = new Command(async () =>
                            {
                                try
                                {
                                    await CheckAuthenticationAndNavigate();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[App] Retry error: {ex.Message}");
                                }
                            })
                        }
                    }
                }
            };
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
