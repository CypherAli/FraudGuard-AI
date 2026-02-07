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
        private const string PREF_AUTO_PROTECTION = "AutoProtection";  // Enable/Disable auto protection
        private const string DEFAULT_URL = AppConstants.PRODUCTION_SERVER_URL;  // Use production by default
        private const string USB_URL = AppConstants.USB_SERVER_URL; // For emulator
        private const string DEFAULT_DEVICE_ID = "android_device";
        
        // Legacy support for migration
        private const string LEGACY_PREF_SERVER_IP = "ServerIP";

        private readonly Color SuccessColor = Color.FromArgb("#34D399");
        private readonly Color ErrorColor = Color.FromArgb("#F87171");
        private readonly IAuthenticationService? _authService;

        #endregion

        #region Constructor

        public SettingsPage()
        {
            try
            {
                InitializeComponent();
                
                // Get authentication service from DI (null-safe)
                _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthenticationService>();
                
                if (_authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SettingsPage] WARNING: AuthService is null");
                }
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
                CheckServerConnection();
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
                // Check if auth service is available
                if (_authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SettingsPage] AuthService is null, skipping user info load");
                    return;
                }

                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    if (UserNameLabel != null)
                        UserNameLabel.Text = user.DisplayName ?? "Người dùng";
                    if (UserEmailLabel != null)
                        UserEmailLabel.Text = user.Email ?? "user@example.com";
                    if (PhoneNumberLabel != null)
                        PhoneNumberLabel.Text = !string.IsNullOrEmpty(user.PhoneNumber) ? user.PhoneNumber : "Chưa cập nhật";
                    if (AvatarInitials != null && !string.IsNullOrEmpty(user.DisplayName))
                        AvatarInitials.Text = GetInitials(user.DisplayName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error loading user info: {ex.Message}");
            }
        }
        
        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "ND";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
        
        private async void CheckServerConnection()
        {
            try
            {
                var audioService = App.GetAudioService();
                bool isConnected = audioService?.IsConnected ?? false;
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ServerStatusDot != null)
                        ServerStatusDot.BackgroundColor = isConnected ? SuccessColor : Color.FromArgb("#5C6B7A");
                    if (ServerStatusLabel != null)
                    {
                        ServerStatusLabel.Text = isConnected ? "Đã kết nối" : "Chưa kết nối";
                        ServerStatusLabel.TextColor = isConnected ? SuccessColor : Color.FromArgb("#8B9CAF");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error checking connection: {ex.Message}");
            }
        }
        
        private void UpdateCurrentConfig()
        {
            try
            {
                if (CurrentConfigLabel != null)
                    CurrentConfigLabel.Text = GetWebSocketUrl();
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
                
                // Load Auto Protection preference
                bool autoProtection = Preferences.Get(PREF_AUTO_PROTECTION, true);  // Default to enabled
                if (AutoProtectionSwitch != null)
                    AutoProtectionSwitch.IsToggled = autoProtection;

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
                if (ServerIPEntry != null)
                    ServerIPEntry.Text = savedURL;
                
                // Update config display
                UpdateConfigurationDisplay(usbMode ? USB_URL : savedURL);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
            }
        }

        private void SaveServerIP()
        {
            try
            {
                string url = ServerIPEntry.Text?.Trim();

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
                        System.Diagnostics.Debug.WriteLine("[Settings] Invalid URL or IP format");
                        return;
                    }
                }

                Preferences.Set(PREF_SERVER_URL, url);
                UpdateConfigurationDisplay(url);
                System.Diagnostics.Debug.WriteLine($"[Settings] Configuration saved: {url}");
            }
            catch { }
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
                Preferences.Set(PREF_USB_MODE, e.Value);
                var displayURL = e.Value ? USB_URL : ServerIPEntry?.Text?.Trim() ?? DEFAULT_URL;
                UpdateConfigurationDisplay(displayURL);
            }
            catch { }
        }

        #endregion

        #region UI Updates

        private void UpdateConfigurationDisplay(string url)
        {
            try
            {
                var cleanUrl = url.Replace("http://", "").Replace("https://", "");
                var protocol = url.StartsWith("https://") ? "wss" : "ws";
                
                if (CurrentConfigLabel != null)
                {
                    CurrentConfigLabel.Text = cleanUrl.Contains(":")
                        ? $"{protocol}://{cleanUrl}/ws"
                        : $"{protocol}://{cleanUrl}:8080/ws";
                }
            }
            catch { }
        }

        #endregion

        #region Connection Testing

        private async Task TestConnectionAsync()
        {
            try
            {
                bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
                string serverUrl = isUsbMode ? USB_URL : ServerIPEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    await DisplayAlert("Lỗi", "Vui lòng nhập URL server", "OK");
                    return;
                }

                string testUrl = serverUrl;
                if (!testUrl.StartsWith("http://") && !testUrl.StartsWith("https://"))
                {
                    if (IsValidIP(testUrl))
                        testUrl = $"http://{testUrl}:8080";
                    else
                    {
                        await DisplayAlert("Lỗi", "Định dạng URL không hợp lệ", "OK");
                        return;
                    }
                }

                TestButton.IsEnabled = false;
                TestButton.Text = "Testing...";

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var healthUrl = testUrl.TrimEnd('/') + "/health";
                var response = await httpClient.GetAsync(healthUrl);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("✅ Thành công", $"Đã kết nối đến server!\n\n{testUrl}", "OK");
                }
                else
                {
                    await DisplayAlert("Lỗi", $"Server trả về lỗi: {response.StatusCode}", "OK");
                }
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("❌ Kết nối thất bại",
                    $"Không thể kết nối đến server.\n\nLỗi: {ex.Message}\n\nKiểm tra:\n• URL đúng chưa\n• Server đang chạy\n• Kết nối mạng", "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlert("⏱️ Hết thời gian",
                    "Kết nối đã hết thời gian.\n\nServer có thể:\n• Không chạy\n• Bị firewall chặn\n• URL sai", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi: {ex.Message}", "OK");
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

        private async void OnTestConnectionClicked(object sender, EventArgs e) => await TestConnectionAsync();

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            try
            {
                string newName = await DisplayPromptAsync(
                    "Chỉnh sửa hồ sơ",
                    "Nhập tên hiển thị mới:",
                    "Lưu",
                    "Hủy",
                    placeholder: "Tên của bạn"
                );

                if (!string.IsNullOrEmpty(newName))
                {
                    UserNameLabel.Text = newName;
                    AvatarInitials.Text = GetInitials(newName);
                    // TODO: Save to server
                    await DisplayAlert("Thành công", "Đã cập nhật hồ sơ", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Edit profile error: {ex.Message}");
            }
        }

        private void OnDarkModeToggled(object sender, ToggledEventArgs e)
        {
            // Dark mode is always on for this design
            // Could implement theme switching here if needed
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Dark mode: {e.Value}");
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(
                "Chọn ngôn ngữ",
                "Hủy",
                null,
                "Tiếng Việt",
                "English"
            );

            if (!string.IsNullOrEmpty(action) && action != "Hủy")
            {
                LanguageLabel.Text = action;
                // TODO: Apply language change
            }
        }

        private async void OnSecurityClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                "Bảo mật tài khoản",
                "Tính năng đang được phát triển.\n\nSẽ bao gồm:\n• Đổi mật khẩu\n• Xác thực 2 bước\n• Quản lý phiên đăng nhập",
                "OK"
            );
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            // Hiển thị crash log nếu có
            try
            {
                var crashLogPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
                string crashInfo = "";
                
                if (System.IO.File.Exists(crashLogPath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(crashLogPath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        crashInfo = $"\n\n📋 Crash Log:\n{content.Substring(0, Math.Min(500, content.Length))}...";
                    }
                }
                
                bool clearLog = await DisplayAlert(
                    "Trợ giúp & Hỗ trợ",
                    $"FraudGuard AI\n\nỨng dụng bảo vệ cuộc gọi khỏi lừa đảo.\n\nLiên hệ: support@fraudguard.ai\nPhiên bản: 1.0.0{crashInfo}",
                    "Xóa Log",
                    "Đóng"
                );
                
                if (clearLog && System.IO.File.Exists(crashLogPath))
                {
                    System.IO.File.Delete(crashLogPath);
                    await DisplayAlert("Thành công", "Đã xóa crash log", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Trợ giúp & Hỗ trợ",
                    $"FraudGuard AI\n\nỨng dụng bảo vệ cuộc gọi khỏi lừa đảo.\n\nLiên hệ: support@fraudguard.ai\nPhiên bản: 1.0.0\n\nLỗi đọc log: {ex.Message}",
                    "OK"
                );
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

                // Check if auth service is available
                if (_authService == null)
                {
                    await DisplayAlert("Lỗi", "Dịch vụ xác thực không khả dụng", "OK");
                    return;
                }

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
            return isUsbMode ? USB_URL : Preferences.Get(PREF_SERVER_URL, DEFAULT_URL);
        }

        public static string GetDeviceID() => Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
        
        public static bool IsAutoProtectionEnabled() => Preferences.Get(PREF_AUTO_PROTECTION, true);

        public static string GetWebSocketUrl()
        {
            var baseUrl = Preferences.Get(PREF_USB_MODE, false) ? USB_URL : GetServerURL();
            
            if (baseUrl.StartsWith("https://"))
                return baseUrl.Replace("https://", "wss://") + "/ws";
            if (baseUrl.StartsWith("http://"))
                return baseUrl.Replace("http://", "ws://") + "/ws";
            
            return $"ws://{baseUrl}:8080/ws";
        }

        public static string GetAPIBaseUrl()
        {
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            return isUsbMode ? USB_URL : GetServerURL();
        }

        #endregion
    }
}
