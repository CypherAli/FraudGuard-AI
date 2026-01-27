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
        private const string PREF_USB_MODE = "UsbMode";
        private const string DEFAULT_IP = "192.168.1.234";
        private const string USB_IP = "10.0.2.2"; // For emulator use 10.0.2.2, for real device use localhost
        private const string DEFAULT_DEVICE_ID = "android_device";

        private readonly Color SuccessColor = Color.FromArgb("#34D399");
        private readonly Color ErrorColor = Color.FromArgb("#F87171");

        #endregion

        #region Constructor

        public SettingsPage()
        {
            try
            {
                InitializeComponent();
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] OnAppearing Error: {ex.Message}");
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
                UsbModeSwitch.IsToggled = usbMode;

                string savedIP = Preferences.Get(PREF_SERVER_IP, DEFAULT_IP);
                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded IP: {savedIP}");
                Console.WriteLine($"[Settings] Loaded IP: {savedIP}");
                ServerIPEntry.Text = savedIP;

                string savedDeviceID = Preferences.Get(PREF_DEVICE_ID, DEFAULT_DEVICE_ID);
                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded Device ID: {savedDeviceID}");
                Console.WriteLine($"[Settings] Loaded Device ID: {savedDeviceID}");
                DeviceIDEntry.Text = savedDeviceID;

                // Update UI based on USB mode
                UpdateUIForUsbMode(usbMode);
                
                // Update config display with appropriate IP
                string displayIP = usbMode ? USB_IP : savedIP;
                UpdateConfigurationDisplay(displayIP);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
                Console.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
                Console.WriteLine($"[Settings] Load error: {ex.Message}");
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

        #region USB Mode Handling

        private void OnUsbModeToggled(object sender, ToggledEventArgs e)
        {
            try
            {
                bool isUsbMode = e.Value;
                Preferences.Set(PREF_USB_MODE, isUsbMode);
                
                UpdateUIForUsbMode(isUsbMode);
                
                string displayIP = isUsbMode ? USB_IP : ServerIPEntry.Text?.Trim() ?? DEFAULT_IP;
                UpdateConfigurationDisplay(displayIP);
                
                string message = isUsbMode 
                    ? "USB Mode ON - Using localhost" 
                    : "WiFi Mode - Using custom IP";
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
                // Get IP based on USB mode
                bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
                string ip = isUsbMode ? USB_IP : ServerIPEntry.Text?.Trim();
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Testing connection to: {ip} (USB Mode: {isUsbMode})");
                Console.WriteLine($"[Settings] Testing connection to: {ip} (USB Mode: {isUsbMode})");

                if (string.IsNullOrWhiteSpace(ip) || !IsValidIP(ip))
                {
                    System.Diagnostics.Debug.WriteLine($"[Settings] Invalid IP: {ip}");
                    Console.WriteLine($"[Settings] Invalid IP: {ip}");
                    ShowStatus("Invalid IP address", isError: true);
                    return;
                }

                TestButton.IsEnabled = false;
                TestButton.Text = "Testing...";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var url = $"http://{ip}:8080/health";
                System.Diagnostics.Debug.WriteLine($"[Settings] Requesting: {url}");
                Console.WriteLine($"[Settings] Requesting: {url}");
                
                var response = await httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"[Settings] Response status: {response.StatusCode}");
                Console.WriteLine($"[Settings] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Settings] Response: {content}");
                    Console.WriteLine($"[Settings] Response: {content}");
                    ShowStatus("Connection successful", isError: false);
                    await DisplayAlert("Success", $"Connected to server at {ip}", "OK");
                }
                else
                {
                    ShowStatus($"Server error: {response.StatusCode}", isError: true);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] HttpRequestException: {ex.Message}");
                Console.WriteLine($"[Settings] HttpRequestException: {ex.Message}");
                ShowStatus("Cannot connect to server", isError: true);
                await DisplayAlert("Connection Failed",
                    "Cannot connect to server.\n\nPlease check:\n• IP address is correct\n• Server is running\n• Same WiFi network",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Exception: {ex.Message}");
                Console.WriteLine($"[Settings] Exception: {ex.Message}");
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
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            if (isUsbMode)
            {
                return $"ws://{USB_IP}:8080/ws";
            }
            
            string ip = GetServerIP();
            return $"ws://{ip}:8080/ws";
        }

        public static string GetAPIBaseUrl()
        {
            bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
            if (isUsbMode)
            {
                return $"http://{USB_IP}:8080";
            }
            
            string ip = GetServerIP();
            return $"http://{ip}:8080";
        }

        #endregion
    }
}
