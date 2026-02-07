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
                System.Diagnostics.Debug.WriteLine("=== [App] Constructor START ===");
                
                System.Diagnostics.Debug.WriteLine("[App] Calling InitializeComponent...");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("[App] InitializeComponent completed");

                // DON'T initialize audio service here - do it later when needed
                System.Diagnostics.Debug.WriteLine("[App] Creating loading page...");
                
                // Simple loading page
                MainPage = new ContentPage
                {
                    BackgroundColor = Color.FromArgb("#0D1B2A"),
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Spacing = 16,
                        Children = 
                        {
                            new ActivityIndicator 
                            { 
                                IsRunning = true, 
                                Color = Color.FromArgb("#14B8A6")
                            },
                            new Label 
                            { 
                                Text = "FraudGuard AI",
                                TextColor = Color.FromArgb("#E0E6ED"),
                                HorizontalOptions = LayoutOptions.Center,
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold
                            },
                            new Label 
                            { 
                                Text = "Đang khởi động...",
                                TextColor = Color.FromArgb("#94A3B8"),
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
                };
                
                System.Diagnostics.Debug.WriteLine("=== [App] Constructor END ===");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== [App] FATAL Constructor Error ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                
                // Fallback simple page
                try
                {
                    MainPage = new ContentPage
                    {
                        Content = new Label 
                        { 
                            Text = $"Lỗi khởi tạo:\n{ex.Message}",
                            TextColor = Colors.White,
                            BackgroundColor = Colors.Red,
                            Padding = 20
                        }
                    };
                }
                catch
                {
                    // If even simple page fails, let it crash with error details
                    throw;
                }
            }
        }

        protected override async void OnStart()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== [App] OnStart START ===");
                base.OnStart();
                
                // Initialize audio service here (after app is fully loaded)
                if (_audioService == null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[App] Initializing audio service...");
                        _audioService = new AudioStreamingServiceLowLevel();
                        System.Diagnostics.Debug.WriteLine("[App] Audio service ready");
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] Audio init failed: {ex.Message}");
                        // Continue without audio - user can still see UI
                    }
                }
                
                await CheckAuthenticationAndNavigate();
                System.Diagnostics.Debug.WriteLine("=== [App] OnStart END ===");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== [App] OnStart Error ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                
                // Show error but don't crash
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
