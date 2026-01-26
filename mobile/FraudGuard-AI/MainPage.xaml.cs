using FraudGuardAI.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace FraudGuardAI
{
    public partial class MainPage : ContentPage
    {
        #region Fields & Constants

        private AudioStreamingServiceLowLevel _audioService;
        private bool _isProtectionActive = false;

        // Risk Score Threshold
        private const double HIGH_RISK_THRESHOLD = 80.0;

        // Animation durations
        private const uint PULSE_DURATION = 1000;
        private const uint DANGER_FLASH_DURATION = 500;

        #endregion

        #region Constructor

        public MainPage()
        {
            InitializeComponent();
            InitializeAudioService();
            
            string wsUrl = SettingsPage.GetWebSocketUrl();
            string deviceId = SettingsPage.GetDeviceID();
            UpdateDebugInfo($"Initialized - WS: {wsUrl}, Device: {deviceId}");
        }

        #endregion

        #region Initialization

        private void InitializeAudioService()
        {
            try
            {
                _audioService = new AudioStreamingServiceLowLevel();

                // Đăng ký các sự kiện
                _audioService.AlertReceived += OnAlertReceived;
                _audioService.ErrorOccurred += OnErrorOccurred;
                _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;

                UpdateDebugInfo("Audio service initialized successfully");
            }
            catch (Exception ex)
            {
                UpdateDebugInfo($"Init Error: {ex.Message}");
                DisplayAlert("Error", $"Cannot initialize service: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Button Event Handlers

        private async void OnToggleButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (!_isProtectionActive)
                {
                    // Bật bảo vệ
                    await StartProtectionAsync();
                }
                else
                {
                    // Tắt bảo vệ
                    await StopProtectionAsync();
                }
            }
            catch (Exception ex)
            {
                UpdateDebugInfo($"Toggle Error: {ex.Message}");
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        #endregion

        #region Protection Control

        private async Task StartProtectionAsync()
        {
            UpdateDebugInfo("Starting protection...");

            // Disable button to prevent double-click
            ToggleButton.IsEnabled = false;

            try
            {
                var success = await _audioService.StartStreamingAsync();

                if (success)
                {
                    _isProtectionActive = true;

                    // Update UI to "Protected" state
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Change to safe/protected mode
                        await AnimateToSafeMode();

                        StatusLabel.Text = "🔒 Protected";
                        ToggleButton.Text = "STOP PROTECTION";
                        ToggleButton.BackgroundColor = Color.FromArgb("#FF5252");

                        // Start shield pulse animation
                        _ = PulseShieldAnimation();

                        UpdateDebugInfo("Protection ACTIVE - Listening...");
                    });
                }
                else
                {
                    await DisplayAlert("Error", "Cannot start protection. Check connection and microphone permission.", "OK");
                    UpdateDebugInfo("Failed to start protection");
                }
            }
            finally
            {
                ToggleButton.IsEnabled = true;
            }
        }

        private async Task StopProtectionAsync()
        {
            UpdateDebugInfo("Stopping protection...");

            ToggleButton.IsEnabled = false;

            try
            {
                await _audioService.StopStreamingAsync();
                _isProtectionActive = false;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Reset to inactive state
                    await AnimateToInactiveMode();

                    StatusLabel.Text = "Not Active";
                    ToggleButton.Text = "START PROTECTION";
                    ToggleButton.BackgroundColor = Color.FromArgb("#1E88E5");

                    // Hide alert banner
                    AlertBanner.IsVisible = false;

                    UpdateDebugInfo("Protection STOPPED");
                });
            }
            finally
            {
                ToggleButton.IsEnabled = true;
            }
        }

        #endregion

        #region Audio Service Event Handlers

        /// <summary>
        /// Xử lý khi nhận được cảnh báo từ Server
        /// QUAN TRỌNG: Phải chạy trên Main Thread
        /// </summary>
        private void OnAlertReceived(object sender, AlertEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var alert = e.Alert;
                    UpdateDebugInfo($"Alert: {alert.AlertType} - Confidence: {alert.Confidence:P}");

                    // Tính Risk Score (giả sử confidence * 100)
                    double riskScore = alert.Confidence * 100;

                    if (riskScore >= HIGH_RISK_THRESHOLD)
                    {
                        // NGUY HIỂM CAO - Chuyển sang chế độ đỏ rực
                        await HandleHighRiskAlert(alert, riskScore);
                    }
                    else
                    {
                        // Rủi ro thấp - Chỉ hiện thông báo nhỏ
                        await HandleLowRiskAlert(alert, riskScore);
                    }
                }
                catch (Exception ex)
                {
                    UpdateDebugInfo($"Alert Handler Error: {ex.Message}");
                }
            });
        }

        private void OnErrorOccurred(object sender, Services.ErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDebugInfo($"Error: {e.Message}");
                // Có thể hiện toast hoặc log error
            });
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (e.IsConnected)
                {
                    ConnectionIndicator.Fill = Color.FromArgb("#4CAF50"); // Green
                    ConnectionStatusLabel.Text = "Đã kết nối";
                }
                else
                {
                    ConnectionIndicator.Fill = Colors.Gray;
                    ConnectionStatusLabel.Text = "Ngắt kết nối";
                }

                UpdateDebugInfo($"Connection: {e.Message}");
            });
        }

        #endregion

        #region Alert Handling

        /// <summary>
        /// Xử lý cảnh báo nguy hiểm cao (Risk Score > 80)
        /// - Chuyển toàn bộ màn hình sang ĐỎ RỰC
        /// - Rung điện thoại
        /// - Hiện cảnh báo lớn
        /// </summary>
        private async Task HandleHighRiskAlert(AlertData alert, double riskScore)
        {
            // 1. Chuyển sang chế độ DANGER (Đỏ rực)
            await AnimateToDangerMode();

            // 2. Rung điện thoại
            TriggerVibration();

            // 3. Hiện banner cảnh báo
            ShowAlertBanner(alert, riskScore, isHighRisk: true);

            // 4. Flash animation để gây chú ý
            _ = DangerFlashAnimation();

            // 5. Hiện popup cảnh báo
            await DisplayAlert(
                "🚨 CẢNH BÁO NGUY HIỂM",
                $"Phát hiện dấu hiệu lừa đảo!\n\n" +
                $"Loại: {alert.AlertType}\n" +
                $"Độ nguy hiểm: {riskScore:F0}%\n" +
                $"Nội dung: {alert.Transcript}\n\n" +
                $"⚠️ Hãy cẩn thận và cúp máy ngay!",
                "Đã hiểu"
            );
        }

        /// <summary>
        /// Xử lý cảnh báo rủi ro thấp
        /// - Chỉ hiện banner nhỏ
        /// - Không đổi màu toàn màn hình
        /// </summary>
        private async Task HandleLowRiskAlert(AlertData alert, double riskScore)
        {
            ShowAlertBanner(alert, riskScore, isHighRisk: false);

            // Auto-hide sau 5 giây
            await Task.Delay(5000);
            if (AlertBanner.IsVisible && riskScore < HIGH_RISK_THRESHOLD)
            {
                AlertBanner.IsVisible = false;
            }
        }

        private void ShowAlertBanner(AlertData alert, double riskScore, bool isHighRisk)
        {
            AlertBanner.IsVisible = true;
            AlertBanner.BackgroundColor = isHighRisk 
                ? Color.FromArgb("#D32F2F") 
                : Color.FromArgb("#FF9800"); // Orange for low risk

            AlertTypeLabel.Text = isHighRisk 
                ? "🚨 NGUY HIỂM CAO" 
                : $"⚠️ {alert.AlertType}";

            AlertMessageLabel.Text = string.IsNullOrEmpty(alert.Transcript)
                ? "Phát hiện dấu hiệu bất thường"
                : alert.Transcript;

            AlertConfidenceLabel.Text = $"Độ nguy hiểm: {riskScore:F0}%";
        }

        #endregion

        #region Animations

        /// <summary>
        /// Chuyển sang chế độ an toàn (Xanh dương/xanh lá)
        /// </summary>
        private async Task AnimateToSafeMode()
        {
            await Task.WhenAll(
                MainGrid.FadeTo(0, 200),
                MainGrid.ScaleTo(0.95, 200)
            );

            // Change colors
            MainGrid.BackgroundColor = Color.FromArgb("#0A1929"); // Dark blue
            ShieldIcon.Opacity = 1.0;
            ShieldIcon.TextColor = Color.FromArgb("#4CAF50"); // Green shield

            await Task.WhenAll(
                MainGrid.FadeTo(1, 200),
                MainGrid.ScaleTo(1, 200)
            );
        }

        /// <summary>
        /// Chuyển sang chế độ NGUY HIỂM (Đỏ rực)
        /// </summary>
        private async Task AnimateToDangerMode()
        {
            await Task.WhenAll(
                MainGrid.FadeTo(0, 150),
                MainGrid.ScaleTo(0.95, 150)
            );

            // Change to RED
            MainGrid.BackgroundColor = Color.FromArgb("#B71C1C"); // Deep red
            ShieldIcon.TextColor = Color.FromArgb("#FFEBEE"); // Light red
            StatusLabel.Text = "🚨 PHÁT HIỆN LỪA ĐẢO";
            StatusLabel.TextColor = Color.FromArgb("#FFEBEE");

            await Task.WhenAll(
                MainGrid.FadeTo(1, 150),
                MainGrid.ScaleTo(1, 150)
            );
        }

        /// <summary>
        /// Chuyển về chế độ không hoạt động (Xám)
        /// </summary>
        private async Task AnimateToInactiveMode()
        {
            await MainGrid.FadeTo(0, 200);

            MainGrid.BackgroundColor = Color.FromArgb("#0A1929");
            ShieldIcon.Opacity = 0.5;
            ShieldIcon.TextColor = Colors.Gray;
            StatusLabel.TextColor = Color.FromArgb("#E3F2FD");

            await MainGrid.FadeTo(1, 200);
        }

        /// <summary>
        /// Animation nhấp nháy shield khi đang bảo vệ
        /// </summary>
        private async Task PulseShieldAnimation()
        {
            while (_isProtectionActive)
            {
                await ShieldIcon.ScaleTo(1.1, PULSE_DURATION, Easing.SinInOut);
                await ShieldIcon.ScaleTo(1.0, PULSE_DURATION, Easing.SinInOut);
            }
        }

        /// <summary>
        /// Flash animation khi phát hiện nguy hiểm
        /// </summary>
        private async Task DangerFlashAnimation()
        {
            for (int i = 0; i < 3; i++)
            {
                await MainGrid.FadeTo(0.7, DANGER_FLASH_DURATION);
                await MainGrid.FadeTo(1.0, DANGER_FLASH_DURATION);
            }
        }

        #endregion

        #region Vibration

        /// <summary>
        /// Rung điện thoại để cảnh báo
        /// </summary>
        private void TriggerVibration()
        {
            try
            {
                // Pattern: Rung 500ms, nghỉ 200ms, rung 500ms
                var duration = TimeSpan.FromMilliseconds(500);
                Vibration.Default.Vibrate(duration);

                Task.Delay(700).ContinueWith(_ =>
                {
                    Vibration.Default.Vibrate(duration);
                });
            }
            catch (Exception ex)
            {
                UpdateDebugInfo($"Vibration Error: {ex.Message}");
            }
        }

        #endregion

        #region Debug Helpers

        private void UpdateDebugInfo(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                DebugLabel.Text = $"[{timestamp}] {message}";
                System.Diagnostics.Debug.WriteLine($"[FraudGuard] {message}");
            });
        }

        #endregion

        #region Lifecycle

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Cleanup
            if (_isProtectionActive)
            {
                _ = StopProtectionAsync();
            }

            if (_audioService != null)
            {
                _audioService.AlertReceived -= OnAlertReceived;
                _audioService.ErrorOccurred -= OnErrorOccurred;
                _audioService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _audioService.Dispose();
            }
        }

        #endregion
    }
}
