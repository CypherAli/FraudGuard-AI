using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using FraudGuardAI.Constants;
using FraudGuardAI.Helpers;
using FraudGuardAI.Services;
using FraudGuardAI.Models;

namespace FraudGuardAI
{
    public partial class MainPage : ContentPage
    {
        #region Fields

        private AudioStreamingServiceLowLevel _audioService;
        private bool _isProtectionActive = false;
        private bool _isConnecting = false;

        #endregion

        #region Constructor

        public MainPage()
        {
            InitializeComponent();
            InitializeAudioService();

            string wsUrl = SettingsPage.GetWebSocketUrl();
            string deviceId = SettingsPage.GetDeviceID();
            UpdateDebugInfo($"Ready - WS: {wsUrl}");
        }

        #endregion

        #region Initialization

        private void InitializeAudioService()
        {
            try
            {
                _audioService = new AudioStreamingServiceLowLevel();
                _audioService.AlertReceived += OnAlertReceived;
                _audioService.ErrorOccurred += OnErrorOccurred;
                _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;
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
            if (_isConnecting) return; // Prevent double-tap

            try
            {
                if (!_isProtectionActive)
                {
                    // Check permissions first
                    bool hasPermission = await PermissionManager.RequestMicrophonePermissionAsync();
                    if (!hasPermission)
                    {
                        UpdateDebugInfo("Permission denied");
                        return;
                    }

                    await StartProtectionAsync();
                }
                else
                {
                    await StopProtectionAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Toggle Button", ex);
                bool retry = await ErrorHandler.ShowErrorWithRetry(ex);
                if (retry)
                {
                    await Task.Delay(500);
                    OnToggleButtonClicked(sender, e);
                }
            }
        }

        #endregion

        #region Protection Control

        private async Task StartProtectionAsync()
        {
            if (_isConnecting) return;
            
            _isConnecting = true;
            UpdateDebugInfo("Starting protection...");
            UpdateButtonState(isLoading: true);

            try
            {
                // Show connecting status
                await ShowConnectingState();

                // Add timeout for connection attempt
                var connectionTask = _audioService.StartStreamingAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);
                
                bool success = false;
                if (completedTask == connectionTask)
                {
                    success = await connectionTask;
                }
                else
                {
                    UpdateDebugInfo("Connection timeout after 10s");
                    success = false;
                }

                if (success)
                {
                    _isProtectionActive = true;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await AnimateToActiveState();
                        StatusLabel.Text = "Protected";
                        ToggleButton.Text = "Stop Protection";
                        ToggleButton.BackgroundColor = AppConstants.DangerColor;
                        _ = PulseAnimation();
                        UpdateDebugInfo("Protection ACTIVE");
                    });
                }
                else
                {
                    await ShowConnectionFailedState();
                    bool retry = await Application.Current.MainPage.DisplayAlert(
                        "Connection Failed",
                        "Unable to connect to protection server.\n\n" +
                        "• Check server IP in Settings\n" +
                        "• Verify server is running\n" +
                        "• Ensure same WiFi network",
                        "Retry",
                        "Settings"
                    );

                    if (retry)
                    {
                        await Task.Delay(500);
                        await StartProtectionAsync();
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//SettingsPage");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Start Protection", ex);
                await ShowConnectionFailedState();
                bool retry = await ErrorHandler.ShowErrorWithRetry(ex);
                if (retry)
                {
                    await Task.Delay(500);
                    await StartProtectionAsync();
                }
            }
            finally
            {
                _isConnecting = false;
                UpdateButtonState(isLoading: false);
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
                    await AnimateToInactiveState();
                    StatusLabel.Text = "Inactive";
                    ToggleButton.Text = "Start Protection";
                    ToggleButton.BackgroundColor = AppConstants.SafeColor;
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

        private void OnAlertReceived(object sender, AlertEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var alert = e.Alert;
                    double riskScore = alert.Confidence * 100;
                    UpdateDebugInfo($"Alert: {alert.AlertType} - {riskScore:F0}%");

                    if (riskScore >= AppConstants.HIGH_RISK_THRESHOLD)
                    {
                        await HandleHighRiskAlert(alert, riskScore);
                    }
                    else
                    {
                        await HandleLowRiskAlert(alert, riskScore);
                    }
                }
                catch (Exception ex)
                {
                    UpdateDebugInfo($"Alert Error: {ex.Message}");
                }
            });
        }

        private void OnErrorOccurred(object sender, Services.ErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDebugInfo($"Error: {e.Message}");
            });
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (e.IsConnected)
                {
                    ConnectionDot.BackgroundColor = AppConstants.SafeColor;
                    ConnectionStatusLabel.Text = "Connected";
                    ConnectionStatusLabel.TextColor = AppConstants.SafeColor;
                }
                else
                {
                    ConnectionDot.BackgroundColor = AppConstants.InactiveColor;
                    ConnectionStatusLabel.Text = "Disconnected";
                    ConnectionStatusLabel.TextColor = AppConstants.TextSecondary;
                }
                UpdateDebugInfo($"Connection: {e.Message}");
            });
        }

        #endregion

        #region Alert Handling

        private async Task HandleHighRiskAlert(AlertData alert, double riskScore)
        {
            await AnimateToDangerState();
            TriggerVibration();
            ShowAlertBanner(alert, riskScore, isHighRisk: true);
            _ = DangerFlashAnimation();

            await DisplayAlert(
                "⚠️ HIGH RISK DETECTED",
                $"Fraud indicators found!\n\n" +
                $"Type: {alert.AlertType}\n" +
                $"Risk Level: {riskScore:F0}%\n" +
                $"Content: {alert.Transcript}\n\n" +
                $"Consider ending this call immediately.",
                "Understood"
            );
        }

        private async Task HandleLowRiskAlert(AlertData alert, double riskScore)
        {
            ShowAlertBanner(alert, riskScore, isHighRisk: false);
            await Task.Delay(AppConstants.ALERT_AUTO_DISMISS_DELAY);
            if (AlertBanner.IsVisible && riskScore < AppConstants.HIGH_RISK_THRESHOLD)
            {
                AlertBanner.IsVisible = false;
            }
        }

        private void ShowAlertBanner(AlertData alert, double riskScore, bool isHighRisk)
        {
            AlertBanner.IsVisible = true;
            AlertBanner.BackgroundColor = isHighRisk 
                ? AppConstants.DangerBackground 
                : AppConstants.WarningBackground;

            AlertTypeLabel.Text = isHighRisk ? "High Risk Detected" : alert.AlertType;
            AlertMessageLabel.Text = string.IsNullOrEmpty(alert.Transcript)
                ? "Suspicious activity detected"
                : alert.Transcript;
            AlertConfidenceLabel.Text = $"Risk Level: {riskScore:F0}%";
        }

        #endregion

        #region Animations

        private async Task AnimateToActiveState()
        {
            await Task.WhenAll(
                ShieldBorder.ScaleTo(0.95, 150, Easing.CubicOut),
                ShieldIcon.FadeTo(0.5, 150)
            );

            ShieldIcon.Opacity = 1.0;
            ShieldBorder.Stroke = AppConstants.SafeColor;
            GlowRing.Stroke = AppConstants.GlowGreen;
            await GlowRing.FadeTo(1, AppConstants.FADE_DURATION);

            await Task.WhenAll(
                ShieldBorder.ScaleTo(1, AppConstants.SCALE_OUT_DURATION, Easing.SpringOut)
            );
        }

        private async Task AnimateToDangerState()
        {
            await ShieldBorder.ScaleTo(0.95, 100);
            
            MainGrid.BackgroundColor = AppConstants.DangerBackground;
            ShieldBorder.Stroke = AppConstants.DangerColor;
            GlowRing.Stroke = AppConstants.GlowRed;
            StatusLabel.Text = "⚠️ THREAT DETECTED";
            StatusLabel.TextColor = Color.FromArgb("#FCA5A5");

            await ShieldBorder.ScaleTo(1.05, 150, Easing.SpringOut);
            await ShieldBorder.ScaleTo(1, 100);
        }

        private async Task AnimateToInactiveState()
        {
            await Task.WhenAll(
                ShieldBorder.ScaleTo(0.95, AppConstants.SCALE_IN_DURATION),
                GlowRing.FadeTo(0, AppConstants.SCALE_IN_DURATION)
            );

            MainGrid.BackgroundColor = AppConstants.BackgroundDark;
            ShieldBorder.Stroke = Color.FromArgb("#2A3F54");
            ShieldIcon.Opacity = 0.4;
            StatusLabel.TextColor = Color.FromArgb("#E0E6ED");

            await ShieldBorder.ScaleTo(1, AppConstants.SCALE_OUT_DURATION, Easing.SpringOut);
        }

        private async Task PulseAnimation()
        {
            while (_isProtectionActive)
            {
                await GlowRing.ScaleTo(1.1, AppConstants.PULSE_DURATION / 2, Easing.SinInOut);
                await GlowRing.ScaleTo(1.0, AppConstants.PULSE_DURATION / 2, Easing.SinInOut);
            }
        }

        private async Task DangerFlashAnimation()
        {
            for (int i = 0; i < AppConstants.DANGER_FLASH_COUNT; i++)
            {
                await MainGrid.FadeTo(0.8, AppConstants.DANGER_FLASH_DURATION / 2);
                await MainGrid.FadeTo(1.0, AppConstants.DANGER_FLASH_DURATION / 2);
            }
        }

        #endregion

        #region Vibration

        private void TriggerVibration()
        {
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(AppConstants.VIBRATION_DURATION));
                Task.Delay((int)AppConstants.VIBRATION_PAUSE)
                    .ContinueWith(_ => Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(AppConstants.VIBRATION_DURATION)));
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
                var timestamp = DateTime.Now.ToString(AppConstants.TIMESTAMP_FORMAT);
                DebugLabel.Text = $"[{timestamp}] {message}";
                System.Diagnostics.Debug.WriteLine($"[FraudGuard] {message}");
            });
        }

        #endregion

        #region UI State Helpers

        private void UpdateButtonState(bool isLoading)
        {
            ToggleButton.IsEnabled = !isLoading;
            if (isLoading)
            {
                ToggleButton.Text = "Connecting...";
            }
        }

        private async Task ShowConnectingState()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = "Connecting...";
                ConnectionStatusLabel.Text = "Establishing connection";
            });
        }

        private async Task ShowConnectionFailedState()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = "Connection Failed";
                ToggleButton.Text = "Start Protection";
                ToggleButton.BackgroundColor = AppConstants.SafeColor;
            });
        }

        #endregion

        #region Lifecycle

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

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
