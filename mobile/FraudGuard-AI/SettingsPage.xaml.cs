using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FraudGuardAI
{
    public partial class SettingsPage : ContentPage
    {
        #region Constants

        private const string PREF_SERVER_IP = "ServerIP";
        private const string PREF_DEVICE_ID = "DeviceID";
        private const string DEFAULT_IP = "192.168.1.234";
        private const string DEFAULT_DEVICE_ID = "android_device";

        private readonly Color SuccessColor = Color.FromArgb("#34D399");
        private readonly Color ErrorColor = Color.FromArgb("#F87171");

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

        private void LoadSettings()
        {
            try
            {
                string savedIP = Preferences.Get(PREF_SERVER_IP, DEFAULT_IP);
                ServerIPEntry.Text = savedIP;

                string savedDeviceID = Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
                DeviceIDEntry.Text = savedDeviceID;

                UpdateConfigurationDisplay(savedIP);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
                ShowStatus("Error loading settings", isError: true);
            }
        }

        private void SaveServerIP()
        {
            try
            {
                string ip = ServerIPEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(ip))
                {
                    ShowStatus("Please enter an IP address", isError: true);
                    return;
                }

                if (!IsValidIP(ip))
                {
                    ShowStatus("Invalid IP format", isError: true);
                    return;
                }

                Preferences.Set(PREF_SERVER_IP, ip);
                UpdateConfigurationDisplay(ip);
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

        #region UI Updates

        private void UpdateConfigurationDisplay(string ip)
        {
            CurrentConfigLabel.Text = $"ws://{ip}:8080/ws";
            CurrentAPILabel.Text = $"http://{ip}:8080/api";
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusLabel.Text = isError ? $"✕ {message}" : $"✓ {message}";
            StatusLabel.TextColor = isError ? ErrorColor : SuccessColor;
            StatusLabel.IsVisible = true;

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

        private async Task TestConnectionAsync()
        {
            try
            {
                string ip = ServerIPEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(ip) || !IsValidIP(ip))
                {
                    ShowStatus("Invalid IP address", isError: true);
                    return;
                }

                TestButton.IsEnabled = false;
                TestButton.Text = "Testing...";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var url = $"http://{ip}:8080/health";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    ShowStatus("Connection successful", isError: false);
                    await DisplayAlert("Success", $"Connected to server at {ip}", "OK");
                }
                else
                {
                    ShowStatus($"Server error: {response.StatusCode}", isError: true);
                }
            }
            catch (HttpRequestException)
            {
                ShowStatus("Cannot connect to server", isError: true);
                await DisplayAlert("Connection Failed",
                    "Cannot connect to server.\n\nPlease check:\n• IP address is correct\n• Server is running\n• Same WiFi network",
                    "OK");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
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

        #endregion

        #region Public Static Helpers

        public static string GetServerIP() => Preferences.Get(PREF_SERVER_IP, DEFAULT_IP);

        public static string GetDeviceID() => Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);

        public static string GetWebSocketUrl()
        {
            string host = GetServerIP();
            
            if (host.Contains("ngrok") || host.Contains(".app") || host.Contains("tunnel"))
            {
                return $"wss://{host}/ws";
            }
            return $"ws://{host}:8080/ws";
        }

        public static string GetAPIBaseUrl()
        {
            string host = GetServerIP();
            
            if (host.Contains("ngrok") || host.Contains(".app") || host.Contains("tunnel"))
            {
                return $"https://{host}";
            }
            return $"http://{host}:8080";
        }

        #endregion
    }
}
