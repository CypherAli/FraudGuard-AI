using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FraudGuardAI
{
    public partial class SettingsPage : ContentPage
    {
        #region Constants

        private const string PREF_SERVER_IP = "ServerIP";
        private const string PREF_DEVICE_ID = "DeviceID";
        private const string DEFAULT_IP = "192.168.1.234"; // Change this to your computer's LAN IP
        private const string DEFAULT_DEVICE_ID = "android_device";

        #endregion

        #region Constructor

        public SettingsPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Lifecycle

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSettings();
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Load saved settings from Preferences
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Load Server IP
                string savedIP = Preferences.Get(PREF_SERVER_IP, DEFAULT_IP);
                ServerIPEntry.Text = savedIP;

                // Load Device ID
                string savedDeviceID = Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
                DeviceIDEntry.Text = savedDeviceID;

                // Update current configuration display
                UpdateConfigurationDisplay(savedIP);

                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded IP: {savedIP}, DeviceID: {savedDeviceID}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error loading settings: {ex.Message}");
                ShowStatus("Error loading configuration", isError: true);
            }
        }

        /// <summary>
        /// Save Server IP to Preferences
        /// </summary>
        private void SaveServerIP()
        {
            try
            {
                string ip = ServerIPEntry.Text?.Trim();

                // Validate IP
                if (string.IsNullOrWhiteSpace(ip))
                {
                    ShowStatus("❌ Please enter an IP address", isError: true);
                    return;
                }

                // Basic IP validation (simple check)
                if (!IsValidIP(ip))
                {
                    ShowStatus("❌ Invalid IP address format", isError: true);
                    return;
                }

                // Save to Preferences
                Preferences.Set(PREF_SERVER_IP, ip);

                // Update display
                UpdateConfigurationDisplay(ip);

                ShowStatus("✅ Configuration saved successfully!", isError: false);

                System.Diagnostics.Debug.WriteLine($"[Settings] Saved Server IP: {ip}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error saving IP: {ex.Message}");
                ShowStatus($"❌ Error: {ex.Message}", isError: true);
            }
        }

        /// <summary>
        /// Save Device ID to Preferences
        /// </summary>
        private void SaveDeviceID()
        {
            try
            {
                string deviceID = DeviceIDEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(deviceID))
                {
                    ShowStatus("❌ Please enter a Device ID", isError: true);
                    return;
                }

                Preferences.Set(PREF_DEVICE_ID, deviceID);
                ShowStatus("✅ Device ID saved!", isError: false);

                System.Diagnostics.Debug.WriteLine($"[Settings] Saved Device ID: {deviceID}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error saving Device ID: {ex.Message}");
                ShowStatus($"❌ Error: {ex.Message}", isError: true);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Basic IP address validation
        /// </summary>
        private bool IsValidIP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            // Simple validation: check for dots and numbers
            var parts = ip.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Update the current configuration display
        /// </summary>
        private void UpdateConfigurationDisplay(string ip)
        {
            CurrentConfigLabel.Text = $"WebSocket: ws://{ip}:8080/ws";
            CurrentAPILabel.Text = $"API: http://{ip}:8080/api/history";
        }

        /// <summary>
        /// Show status message to user
        /// </summary>
        private void ShowStatus(string message, bool isError)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = isError ? Colors.Red : Color.FromArgb("#4CAF50");
            StatusLabel.IsVisible = true;

            // Auto-hide after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.IsVisible = false;
                });
            });
        }

        #endregion

        #region Connection Testing

        /// <summary>
        /// Test connection to the server
        /// </summary>
        private async Task TestConnectionAsync()
        {
            try
            {
                string ip = ServerIPEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(ip) || !IsValidIP(ip))
                {
                    ShowStatus("❌ Địa chỉ IP không hợp lệ", isError: true);
                    return;
                }

                TestButton.IsEnabled = false;
                TestButton.Text = "⏳ Đang kiểm tra...";

                // Test HTTP connection to health endpoint
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var url = $"http://{ip}:8080/health";
                System.Diagnostics.Debug.WriteLine($"[Settings] Testing connection to: {url}");

                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    ShowStatus("✅ Kết nối thành công!", isError: false);
                    await DisplayAlert("Success", $"Connected to server:\n{url}", "OK");
                }
                else
                {
                    ShowStatus($"❌ Server returned error: {response.StatusCode}", isError: true);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Connection test failed: {ex.Message}");
                ShowStatus("❌ Cannot connect to server", isError: true);
                await DisplayAlert("Connection Error",
                    "Cannot connect to server.\n\n" +
                    "Check:\n" +
                    "• Is the IP correct?\n" +
                    "• Is the server running?\n" +
                    "• Are you on the same WiFi?",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Test error: {ex.Message}");
                ShowStatus($"❌ Lỗi: {ex.Message}", isError: true);
            }
            finally
            {
                TestButton.IsEnabled = true;
                TestButton.Text = "🔌 KIỂM TRA KẾT NỐI";
            }
        }

        #endregion

        #region Event Handlers

        private void OnSaveButtonClicked(object sender, EventArgs e)
        {
            SaveServerIP();
        }

        private void OnSaveDeviceIDClicked(object sender, EventArgs e)
        {
            SaveDeviceID();
        }

        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            await TestConnectionAsync();
        }

        #endregion

        #region Public Static Helpers

        /// <summary>
        /// Get the configured server IP (for use by other services)
        /// </summary>
        public static string GetServerIP()
        {
            return Preferences.Get(PREF_SERVER_IP, DEFAULT_IP);
        }

        /// <summary>
        /// Get the configured device ID (for use by other services)
        /// </summary>
        public static string GetDeviceID()
        {
            return Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
        }

        /// <summary>
        /// Build WebSocket URL from saved IP
        /// Supports both local (ws://) and Ngrok (wss://) connections
        /// </summary>
        public static string GetWebSocketUrl()
        {
            string host = GetServerIP();
            
            // Auto-detect: If contains ngrok/tunnel domain → use wss:// (secure)
            if (host.Contains("ngrok") || host.Contains(".app") || host.Contains("tunnel"))
            {
                // Ngrok/Cloud: wss://domain/ws (no port)
                return $"wss://{host}/ws";
            }
            else
            {
                // Local network: ws://ip:port/ws
                return $"ws://{host}:8080/ws";
            }
        }

        /// <summary>
        /// Build API base URL from saved IP
        /// Supports both local (http://) and Ngrok (https://) connections
        /// </summary>
        public static string GetAPIBaseUrl()
        {
            string host = GetServerIP();
            
            // Auto-detect scheme based on host
            if (host.Contains("ngrok") || host.Contains(".app") || host.Contains("tunnel"))
            {
                return $"https://{host}";
            }
            else
            {
                return $"http://{host}:8080";
            }
        }

        #endregion
    }
}
