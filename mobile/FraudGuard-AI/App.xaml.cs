using FraudGuardAI.Services;
using FraudGuardAI.Pages;
using FraudGuardAI.Pages.Auth;
using FraudGuardAI.Localization;
using System.Diagnostics;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace FraudGuardAI
{
    public partial class App : Application
    {
        private static AudioStreamingServiceLowLevel? _audioService;
        private static readonly object _audioServiceLock = new(); // guards lazy-init of _audioService
        private const string PREF_LIGHT_THEME = "LightThemeEnabled";
        private const string PREF_LIGHT_THEME_USER_SET = "LightThemeUserSet";
        private const string PREF_APP_LANGUAGE = "AppLanguage";
        
        public App()
        {
            try
            {
                Debug.WriteLine("[App] Initializing App...");
                InitializeComponent();
                Debug.WriteLine("[App] InitializeComponent() completed");

                string languageCode = Preferences.Get(PREF_APP_LANGUAGE, "en");
                LocalizationResourceManager.Instance.SetCulture(new CultureInfo(languageCode));

                bool lightThemeUserSet = Preferences.Get(PREF_LIGHT_THEME_USER_SET, false);
                bool lightThemeEnabled = lightThemeUserSet && Preferences.Get(PREF_LIGHT_THEME, false);
                if (!lightThemeUserSet)
                {
                    Preferences.Set(PREF_LIGHT_THEME, false);
                }
                UserAppTheme = lightThemeEnabled ? AppTheme.Light : AppTheme.Dark;
                ApplyThemeResources(lightThemeEnabled);

                // DO NOT initialize AudioService here - defer to when it's actually needed
                // Initializing services in App constructor can cause silent failures
                // that prevent UI from rendering (black screen)

                // Use StartupPage to handle initialization with better error handling
                // This prevents black screen by showing loading state
                MainPage = new StartupPage();

                Debug.WriteLine("[App] ✅ App initialized successfully with StartupPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] ❌ CRITICAL ERROR during initialization: {ex.Message}");
                Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");

                // Show error page instead of black screen
                MainPage = CreateErrorPage(ex);
            }
        }
        
        /// <summary>
        /// Create an error page to show when app fails to initialize
        /// This prevents the dreaded black screen
        /// </summary>
        private static ContentPage CreateErrorPage(Exception ex)
        {
            return new ContentPage
            {
                BackgroundColor = Color.FromArgb("#0D1B2A"),
                Content = new VerticalStackLayout
                {
                    Padding = 30,
                    Spacing = 20,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "❌",
                            FontSize = 64,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Lỗi khởi động ứng dụng",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#F87171"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = ex.Message,
                            FontSize = 14,
                            TextColor = Color.FromArgb("#8B9CAF"),
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Button
                        {
                            Text = "Khởi động lại",
                            BackgroundColor = Color.FromArgb("#34D399"),
                            TextColor = Color.FromArgb("#0D1B2A"),
                            CornerRadius = 12,
                            Margin = new Thickness(0, 20, 0, 0),
                            Command = new Command(() => Application.Current?.Quit())
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Get shared audio service instance (lazy singleton pattern)
        /// This ensures connection persists across tab navigation
        /// Audio service is only created when first needed, not at app startup
        /// </summary>
        public static AudioStreamingServiceLowLevel GetAudioService()
        {
            // Double-checked locking to prevent two threads creating separate instances
            if (_audioService == null)
            {
                lock (_audioServiceLock)
                {
                    if (_audioService == null)
                    {
                        Debug.WriteLine("[App] Creating AudioStreamingServiceLowLevel (lazy init)");
                        var settings = Application.Current?.Handler?.MauiContext?.Services
                            ?.GetService<IAppSettings>() ?? new AppSettings();
                        _audioService = new AudioStreamingServiceLowLevel(settings);
                    }
                }
            }
            return _audioService;
        }

        public static void ApplyThemeResources(bool useLightTheme)
        {
            if (Application.Current == null)
                return;

            var resources = Application.Current.Resources;
            if (useLightTheme)
            {
                resources["PageBackground"]  = Color.FromArgb("#F5F7FB");
                resources["TextPrimary"]     = Color.FromArgb("#1B2530");
                resources["TextSecondary"]   = Color.FromArgb("#5A6B7A");
                resources["CardBg"]          = Color.FromArgb("#FFFFFF");
                resources["InputBg"]         = Color.FromArgb("#EEF2F6");
                resources["StatCardBg"]      = Color.FromArgb("#F1F5F9");
                resources["BackgroundColor"] = Color.FromArgb("#F5F7FB");
                resources["CardBackground"]  = Color.FromArgb("#FFFFFF");
                resources["InputBackground"] = Color.FromArgb("#EEF2F6");
                resources["BorderColor"]     = Color.FromArgb("#D6E0EA");
            }
            else
            {
                resources["PageBackground"]  = Color.FromArgb("#0A0A0A");
                resources["TextPrimary"]     = Color.FromArgb("#E0E6ED");
                resources["TextSecondary"]   = Color.FromArgb("#8B9CAF");
                resources["CardBg"]          = Color.FromArgb("#1B2838");
                resources["InputBg"]         = Color.FromArgb("#0F1923");
                resources["StatCardBg"]      = Color.FromArgb("#1E2A3A");
                resources["BackgroundColor"] = Color.FromArgb("#0A0A0A");
                resources["CardBackground"]  = Color.FromArgb("#1B2838");
                resources["InputBackground"] = Color.FromArgb("#0F1923");
                resources["BorderColor"]     = Color.FromArgb("#2A3F54");
            }
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
