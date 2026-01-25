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
        private const string DEFAULT_IP = "10.0.2.2";
        private const string DEFAULT_DEVICE_ID = "test_device";

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
                ShowStatus("Lỗi khi tải cấu hình", isError: true);
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
                    ShowStatus("❌ Vui lòng nhập địa chỉ IP", isError: true);
                    return;
                }

                // Basic IP validation (simple check)
                if (!IsValidIP(ip))
                {
                    ShowStatus("❌ Địa chỉ IP không hợp lệ", isError: true);
                    return;
                }

                // Save to Preferences
                Preferences.Set(PREF_SERVER_IP, ip);

                // Update display
                UpdateConfigurationDisplay(ip);

                ShowStatus("✅ Đã lưu cấu hình thành công!", isError: false);

                System.Diagnostics.Debug.WriteLine($"[Settings] Saved Server IP: {ip}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error saving IP: {ex.Message}");
                ShowStatus($"❌ Lỗi: {ex.Message}", isError: true);
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
                    ShowStatus("❌ Vui lòng nhập Device ID", isError: true);
                    return;
                }

                Preferences.Set(PREF_DEVICE_ID, deviceID);
                ShowStatus("✅ Đã lưu Device ID!", isError: false);

                System.Diagnostics.Debug.WriteLine($"[Settings] Saved Device ID: {deviceID}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error saving Device ID: {ex.Message}");
                ShowStatus($"❌ Lỗi: {ex.Message}", isError: true);
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
                    await DisplayAlert("Thành công", $"Đã kết nối tới server:\n{url}", "OK");
                }
                else
                {
                    ShowStatus($"❌ Server trả về lỗi: {response.StatusCode}", isError: true);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Connection test failed: {ex.Message}");
                ShowStatus("❌ Không thể kết nối tới server", isError: true);
                await DisplayAlert("Lỗi kết nối",
                    "Không thể kết nối tới server.\n\n" +
                    "Kiểm tra:\n" +
                    "• IP có đúng không?\n" +
                    "• Server có đang chạy không?\n" +
                    "• Cùng mạng WiFi chưa?",
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
        /// </summary>
        public static string GetWebSocketUrl()
        {
            string ip = GetServerIP();
            return $"ws://{ip}:8080/ws";
        }

        /// <summary>
        /// Build API base URL from saved IP
        /// </summary>
        public static string GetAPIBaseUrl()
        {
            string ip = GetServerIP();
            return $"http://{ip}:8080";
        }

        #endregion
    }
}
