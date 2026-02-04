using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using FraudGuardAI.Constants;
using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI
{
    public partial class SettingsPage : ContentPage
    {
        #region Constants

        private const string PREF_SERVER_URL = "ServerURL";  // Changed from ServerIP to support full URLs
        private const string PREF_DEVICE_ID = "DeviceID";
        private const string PREF_USB_MODE = "UsbMode";
        private const string DEFAULT_URL = AppConstants.PRODUCTION_SERVER_URL;  // Use production by default
        private const string USB_URL = AppConstants.USB_SERVER_URL; // For emulator
        private const string DEFAULT_DEVICE_ID = "android_device";
        
        // Legacy support for migration
        private const string LEGACY_PREF_SERVER_IP = "ServerIP";

        private readonly Color SuccessColor = Color.FromArgb("#34D399");
        private readonly Color ErrorColor = Color.FromArgb("#F87171");
        private readonly IAuthenticationService _authService;

        #endregion

        #region Constructor

        public SettingsPage()
        {
            try
            {
                InitializeComponent();
                
                // Get authentication service from DI
                _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>()
                    ?? throw new InvalidOperationException("Authentication service not found");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Constructor Error: {ex.Message}");
                // Log but don't crash - UI will initialize on appearing
            }
        }

        #endregion

        #region Lifecycle

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                LoadSettings();
                UpdateCurrentConfig();
                LoadUserInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] OnAppearing Error: {ex.Message}");
            }
        }
        
        private async void LoadUserInfo()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user != null && UserPhoneLabel != null)
                {
                    UserPhoneLabel.Text = user.PhoneNumber;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error loading user info: {ex.Message}");
            }
        }
        
        private void UpdateCurrentConfig()
        {
            try
            {
                if (CurrentConfigLabel != null)
                    CurrentConfigLabel.Text = GetWebSocketUrl();
                if (CurrentAPILabel != null)
                    CurrentAPILabel.Text = GetAPIBaseUrl();
            }
            catch { }
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                // Load USB Mode preference
                bool usbMode = Preferences.Get(PREF_USB_MODE, false);
                if (UsbModeSwitch != null)
                    UsbModeSwitch.IsToggled = usbMode;

                // Get saved URL or use default
                string savedURL = Preferences.Get(PREF_SERVER_URL, "");
                if (string.IsNullOrEmpty(savedURL))
                {
                    // Use production server as default
                    savedURL = DEFAULT_URL;
                    Preferences.Set(PREF_SERVER_URL, savedURL);
                    System.Diagnostics.Debug.WriteLine($"[Settings] No saved URL, using default: {savedURL}");
                }
                
                // Clean up legacy preference if it exists (one-time migration)
                if (Preferences.ContainsKey(LEGACY_PREF_SERVER_IP))
                {
                    Preferences.Remove(LEGACY_PREF_SERVER_IP);
                    System.Diagnostics.Debug.WriteLine($"[Settings] Removed legacy ServerIP preference");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded URL: {savedURL}");
                Console.WriteLine($"[Settings] Loaded URL: {savedURL}");
                if (ServerIPEntry != null)
                    ServerIPEntry.Text = savedURL;

                string savedDeviceID = Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
                if (string.IsNullOrEmpty(savedDeviceID))
                {
                    savedDeviceID = DEFAULT_DEVICE_ID;
                    Preferences.Set(PREF_DEVICE_ID, savedDeviceID); // Save default
                }
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded Device ID: {savedDeviceID}");
                Console.WriteLine($"[Settings] Loaded Device ID: {savedDeviceID}");
                if (DeviceIDEntry != null)
                    DeviceIDEntry.Text = savedDeviceID;

                // Update UI based on USB mode
                UpdateUIForUsbMode(usbMode);
                
                // Update config display with appropriate URL
                string displayURL = usbMode ? USB_URL : savedURL;
                UpdateConfigurationDisplay(displayURL);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
                Console.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
                Console.WriteLine($"[Settings] Load error: {ex.Message}");
                if (StatusLabel != null)
                    ShowStatus("Error loading settings", isError: true);
            }
        }

        private void SaveServerIP()
        {
            try
            {
                string url = ServerIPEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(url))
                {
                    ShowStatus("Please enter a server URL", isError: true);
                    return;
                }

                // Accept both full URLs and IP addresses
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    // Assume it's an IP, add http:// and port
                    if (IsValidIP(url))
                    {
                        url = $"http://{url}:8080";
                    }
                    else
                    {
                        ShowStatus("Invalid URL or IP format", isError: true);
                        return;
                    }
                }

                Preferences.Set(PREF_SERVER_URL, url);
                UpdateConfigurationDisplay(url);
                ShowStatus("Configuration saved", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
            }
        }

        private void SaveDeviceID()
        {
            try
            {
                string deviceID = DeviceIDEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(deviceID))
                {
                    ShowStatus("Please enter a Device ID", isError: true);
                    return;
                }

                Preferences.Set(PREF_DEVICE_ID, deviceID);
                ShowStatus("Device ID saved", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
            }
        }

        #endregion

        #region Validation

        private bool IsValidIP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;

            var parts = ip.Split('.');
            if (parts.Length != 4) return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        #endregion

        #region USB Mode Handling

        private void OnUsbModeToggled(object sender, ToggledEventArgs e)
        {
            try
            {
                bool isUsbMode = e.Value;
                Preferences.Set(PREF_USB_MODE, isUsbMode);
                
                UpdateUIForUsbMode(isUsbMode);
                
                string displayURL = isUsbMode ? USB_URL : ServerIPEntry.Text?.Trim() ?? DEFAULT_URL;
                UpdateConfigurationDisplay(displayURL);
                
                string message = isUsbMode 
                    ? "USB Mode ON - Using localhost" 
                    : "WiFi Mode - Using custom URL";
                ShowStatus(message, isError: false);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] USB Mode: {isUsbMode}");
                Console.WriteLine($"[Settings] USB Mode: {isUsbMode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] USB toggle error: {ex.Message}");
                ShowStatus("Error toggling mode", isError: true);
            }
        }

        private void UpdateUIForUsbMode(bool isUsbMode)
        {
            try
            {
                // Show/hide IP input based on mode
                if (IPInputSection != null)
                    IPInputSection.IsVisible = !isUsbMode;
                
                // Update hint label
                if (ServerModeLabel != null)
                {
                    ServerModeLabel.Text = isUsbMode 
                        ? "USB connected - Auto localhost" 
                        : "Enter your server IP";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] UI update error: {ex.Message}");
            }
        }

        #endregion

        #region UI Updates

        private void UpdateConfigurationDisplay(string url)
        {
            try
            {
                // Remove http/https prefix for display if exists
                string cleanUrl = url.Replace("http://", "").Replace("https://", "");
                bool isHttps = url.StartsWith("https://");
                string protocol = isHttps ? "wss" : "ws";
                string httpProtocol = isHttps ? "https" : "http";
                
                // If URL includes port, use it as is, otherwise add :8080
                if (cleanUrl.Contains(":"))
                {
                    if (CurrentConfigLabel != null)
                        CurrentConfigLabel.Text = $"{protocol}://{cleanUrl}/ws";
                    if (CurrentAPILabel != null)
                        CurrentAPILabel.Text = $"{httpProtocol}://{cleanUrl}/api";
                }
                else
                {
                    if (CurrentConfigLabel != null)
                        CurrentConfigLabel.Text = $"{protocol}://{cleanUrl}:8080/ws";
                    if (CurrentAPILabel != null)
                        CurrentAPILabel.Text = $"{httpProtocol}://{cleanUrl}:8080/api";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Config display error: {ex.Message}");
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            try
            {
                if (StatusLabel != null)
                {
                    StatusLabel.Text = isError ? $"✕ {message}" : $"✓ {message}";
                    StatusLabel.TextColor = isError ? ErrorColor : SuccessColor;
                    StatusLabel.IsVisible = true;

                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (StatusLabel != null)
                                StatusLabel.IsVisible = false;
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] ShowStatus error: {ex.Message}");
            }
        }

        #endregion

        #region Connection Testing

        private async Task TestConnectionAsync()
        {
            try
            {
                // Get URL based on USB mode
                bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
                string serverUrl = isUsbMode ? USB_URL : ServerIPEntry.Text?.Trim();
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Testing connection to: {serverUrl} (USB Mode: {isUsbMode})");
                Console.WriteLine($"[Settings] Testing connection to: {serverUrl} (USB Mode: {isUsbMode})");

                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[Settings] Empty URL");
                    Console.WriteLine($"[Settings] Empty URL");
                    ShowStatus("Please enter a server URL", isError: true);
                    return;
                }

                // Normalize URL
                string testUrl = serverUrl;
                if (!testUrl.StartsWith("http://") && !testUrl.StartsWith("https://"))
                {
                    // Assume it's an IP, add http:// and port
                    if (IsValidIP(testUrl))
                    {
                        testUrl = $"http://{testUrl}:8080";
                    }
                    else
                    {
                        ShowStatus("Invalid URL format", isError: true);
                        return;
                    }
                }

                TestButton.IsEnabled = false;
                TestButton.Text = "Testing...";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);  // Increased to 30s for Render cold starts

                // Add /health endpoint
                var healthUrl = testUrl.TrimEnd('/') + "/health";
                System.Diagnostics.Debug.WriteLine($"[Settings] Requesting: {healthUrl}");
                Console.WriteLine($"[Settings] Requesting: {healthUrl}");
                
                var response = await httpClient.GetAsync(healthUrl);
                System.Diagnostics.Debug.WriteLine($"[Settings] Response status: {response.StatusCode}");
                Console.WriteLine($"[Settings] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Settings] Response: {content}");
                    Console.WriteLine($"[Settings] Response: {content}");
                    ShowStatus("✅ Connection successful", isError: false);
                    await DisplayAlert("✅ Success", $"Connected to server!\n\n{testUrl}", "OK");
                }
                else
                {
                    ShowStatus($"❌ Server error: {response.StatusCode}", isError: true);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] HttpRequestException: {ex.Message}");
                Console.WriteLine($"[Settings] HttpRequestException: {ex.Message}");
                ShowStatus("❌ Cannot connect to server", isError: true);
                await DisplayAlert("❌ Connection Failed",
                    $"Cannot connect to server.\n\nError: {ex.Message}\n\nPlease check:\n• URL is correct\n• Server is running\n• Internet connection (for Ngrok)\n• Same WiFi (for LAN IP)",
                    "OK");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Timeout: {ex.Message}");
                Console.WriteLine($"[Settings] Timeout: {ex.Message}");
                ShowStatus("❌ Connection timeout", isError: true);
                await DisplayAlert("⏱️ Timeout",
                    "Connection timed out.\n\nThe server might be:\n• Not running\n• Blocked by firewall\n• Wrong URL",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Exception: {ex.Message}");
                Console.WriteLine($"[Settings] Exception: {ex.Message}");
                ShowStatus($"❌ Error: {ex.Message}", isError: true);
            }
            finally
            {
                TestButton.IsEnabled = true;
                TestButton.Text = "Test";
            }
        }

        #endregion

        #region Event Handlers

        private void OnSaveButtonClicked(object sender, EventArgs e) => SaveServerIP();

        private void OnSaveDeviceIDClicked(object sender, EventArgs e) => SaveDeviceID();

        private async void OnTestConnectionClicked(object sender, EventArgs e) => await TestConnectionAsync();

        private void OnUseProductionClicked(object sender, EventArgs e)
        {
            try
            {
                // Turn off USB mode
                if (UsbModeSwitch != null)
                    UsbModeSwitch.IsToggled = false;
                Preferences.Set(PREF_USB_MODE, false);
                
                // Set production URL
                string productionUrl = AppConstants.PRODUCTION_SERVER_URL;
                Preferences.Set(PREF_SERVER_URL, productionUrl);
                
                if (ServerIPEntry != null)
                    ServerIPEntry.Text = productionUrl;
                
                UpdateConfigurationDisplay(productionUrl);
                ShowStatus("✅ Production server configured", isError: false);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Switched to production: {productionUrl}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error switching to production: {ex.Message}");
                ShowStatus("Error setting production server", isError: true);
            }
        }
        
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                // Confirm logout
                bool confirm = await DisplayAlert(
                    "Đăng xuất",
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Đăng xuất",
                    "Hủy"
                );

                if (!confirm)
                    return;

                System.Diagnostics.Debug.WriteLine("[SettingsPage] Logging out user");

                // Logout
                await _authService.LogoutAsync();

                // Navigate to login page
                Application.Current!.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                    BarTextColor = Color.FromArgb("#E0E6ED")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Logout error: {ex.Message}");
                await DisplayAlert("Lỗi", $"Không thể đăng xuất: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Public Static Helpers

        public static string GetServerURL()
        {
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            string url = isUsbMode ? USB_URL : Preferences.Get(PREF_SERVER_URL, DEFAULT_URL);
            
            System.Diagnostics.Debug.WriteLine($"[SettingsPage.GetServerURL] USB Mode: {isUsbMode}, Returning: {url}");
            Console.WriteLine($"[SettingsPage.GetServerURL] USB Mode: {isUsbMode}, Returning: {url}");
            
            return url;
        }

        public static string GetDeviceID() => Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);

        public static string GetWebSocketUrl()
        {
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            string baseUrl = isUsbMode ? USB_URL : GetServerURL();
            
            System.Diagnostics.Debug.WriteLine($"[SettingsPage.GetWebSocketUrl] Base URL: {baseUrl}");
            Console.WriteLine($"[SettingsPage.GetWebSocketUrl] Base URL: {baseUrl}");
            
            // Convert http/https to ws/wss
            if (baseUrl.StartsWith("https://"))
            {
                return baseUrl.Replace("https://", "wss://") + "/ws";
            }
            else if (baseUrl.StartsWith("http://"))
            {
                return baseUrl.Replace("http://", "ws://") + "/ws";
            }
            
            // Fallback for malformed URLs
            return $"ws://{baseUrl}:8080/ws";
        }

        public static string GetAPIBaseUrl()
        {
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            string url = isUsbMode ? USB_URL : GetServerURL();
            
            System.Diagnostics.Debug.WriteLine($"[SettingsPage.GetAPIBaseUrl] Returning: {url}");
            Console.WriteLine($"[SettingsPage.GetAPIBaseUrl] Returning: {url}");
            
            return url;
        }

        #endregion
    }
}
