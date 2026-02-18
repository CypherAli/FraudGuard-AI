using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using FraudGuardAI.Constants;
using FraudGuardAI.Helpers;
using FraudGuardAI.Services;
using FraudGuardAI.Models;
using FraudGuardAI.Localization;
using System.Globalization;
#if ANDROID
using FraudGuardAI.Platforms.Android.Services;
using Android.Provider;
#endif

namespace FraudGuardAI
{
    public partial class MainPage : ContentPage
    {
        #region Fields

        private AudioStreamingServiceLowLevel _audioService;
        private bool _isProtectionActive = false;
        private bool _isConnecting = false;
        private CancellationTokenSource _animationCts;
        private bool _pulseAnimationRunning = false;
        private DashboardStats _stats = new();
        // Tracks which banner alert started the auto-dismiss timer (prevents race condition)
        private string _lastBannerAlertId = string.Empty;

        #endregion

        #region Constructor

        public MainPage()
        {
            InitializeComponent();
            InitializeAudioService();

            LocalizationResourceManager.Instance.PropertyChanged += (_, __) =>
            {
                UpdateProtectionUI(_isProtectionActive, _isConnecting);
                UpdateStatsDisplay();
            };
            
            // Load dashboard stats asynchronously
            _ = LoadDashboardStatsAsync();
            
            // Auto-start protection if enabled in settings
            _ = AutoStartProtectionIfEnabledAsync();
        }
        
        private async Task AutoStartProtectionIfEnabledAsync()
        {
            try
            {
                // Wait a bit for UI to initialize
                await Task.Delay(1000);
                
                // Check if auto protection is enabled and not already active
                if (SettingsPage.IsAutoProtectionEnabled() && !_isProtectionActive && !_isConnecting)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] Auto-starting protection...");
                    await StartProtectionAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Auto-start protection error: {ex.Message}");
            }
        }

        #endregion

        #region Initialization

        private void InitializeAudioService()
        {
            try
            {
                // Use shared service instance from App
                _audioService = App.GetAudioService();
                
                // Attach event handlers
                _audioService.AlertReceived += OnAlertReceived;
                _audioService.ErrorOccurred += OnErrorOccurred;
                _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;
                
                // Check if already streaming from previous session
                _isProtectionActive = _audioService.IsStreaming;
                
                if (_isProtectionActive)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] Service already streaming from previous session");
                    UpdateProtectionUI(true);
                }
                else
                {
                    // Ensure UI reflects inactive state on startup
                    System.Diagnostics.Debug.WriteLine("[MainPage] Service not active, setting inactive UI");
                    UpdateProtectionUI(false);
                }
                
                // Update stats display to reflect current protection state
                UpdateStatsDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Init Error: {ex.Message}");
                // Ensure UI shows inactive state on error
                UpdateProtectionUI(false);
                UpdateStatsDisplay();
            }
        }

        private async Task LoadDashboardStatsAsync()
        {
            try
            {
                var deviceId = SettingsPage.GetDeviceID();
                var historyService = new HistoryService();
                var allCalls = await historyService.GetHistoryAsync(deviceId, limit: 1000);
                var fraudCalls = allCalls.Where(c => c.IsFraud).ToList();
                
                _stats.BlockedTotal = fraudCalls.Count;
                _stats.BlockedToday = fraudCalls.Count(c => c.Timestamp.ToLocalTime().Date == DateTime.Today);
                _stats.SeriousThreats = fraudCalls.Count(c => c.Confidence >= AppConstants.HIGH_RISK_THRESHOLD);

                // Protection efficiency: percentage of calls analyzed where threats were detected and blocked
                if (allCalls.Count > 0 && fraudCalls.Count > 0)
                    _stats.ProtectionEfficiency = Math.Min(100, ((allCalls.Count - fraudCalls.Count) / (double)allCalls.Count) * 100);
                else if (allCalls.Count > 0)
                    _stats.ProtectionEfficiency = 100; // All calls safe = 100% protection
                else
                    _stats.ProtectionEfficiency = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Failed to load stats: {ex.Message}");
            }
            finally
            {
                UpdateStatsDisplay();
            }
        }

        private void UpdateStatsDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BlockedTotalLabel.Text = _stats.BlockedTotalDisplay;
                BlockedTodayLabel.Text = _stats.BlockedTodayDisplay;
                ThreatsLabel.Text = _stats.ThreatsDisplay;
                EfficiencyLabel.Text = _stats.EfficiencyDisplay;
                
                WeeklyChangeLabel.IsVisible = _stats.WeeklyChange > 0;
                if (WeeklyChangeLabel.IsVisible)
                    WeeklyChangeLabel.Text = string.Format(
                        CultureInfo.CurrentCulture,
                        T("Main_WeeklyChangeFormat"),
                        _stats.WeeklyChange
                    );
                
                EfficiencyChangeLabel.IsVisible = _stats.EfficiencyChange > 0;
                if (EfficiencyChangeLabel.IsVisible)
                    EfficiencyChangeLabel.Text = string.Format(
                        CultureInfo.CurrentCulture,
                        T("Main_EfficiencyChangeFormat"),
                        _stats.EfficiencyChangeDisplay
                    );
                
                BlockRateLabel.Text = !_isProtectionActive || _stats.BlockedTotal == 0
                    ? T("Main_NoData")
                    : string.Format(
                        CultureInfo.CurrentCulture,
                        T("Main_BlockRateFormat"),
                        _stats.EfficiencyDisplay
                    );
            });
        }

        #endregion

        #region Button Event Handlers

        private async void OnToggleProtectionClicked(object sender, EventArgs e)
        {
            if (_isProtectionActive)
            {
                await StopProtectionAsync();
            }
            else
            {
                if (!await PermissionManager.RequestAllPermissionsAsync())
                {
                    await DisplayAlert(
                        T("Main_PermissionTitle"),
                        T("Main_PermissionMessage"),
                        T("Common_OK")
                    );
                    return;
                }
                await StartProtectionAsync();
            }
        }

        private async void OnReportButtonClicked(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync(
                T("Main_ReportTitle"),
                T("Main_ReportPrompt"),
                T("Main_ReportConfirm"),
                T("Main_ReportCancel"),
                placeholder: "0912345678",
                keyboard: Keyboard.Telephone
            );

            if (!string.IsNullOrEmpty(result))
                await DisplayAlert(
                    T("Main_ReportSuccessTitle"),
                    string.Format(
                        CultureInfo.CurrentCulture,
                        T("Main_ReportSuccessMessage"),
                        result
                    ),
                    T("Common_OK")
                );
        }

        #endregion

        #region Protection Control

        public async Task StartProtectionAsync()
        {
            if (_isConnecting) return;
            
            _isConnecting = true;
            UpdateProtectionUI(false, connecting: true);

            try
            {
                var connectionTask = _audioService.StartStreamingAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);
                
                bool success = completedTask == connectionTask && await connectionTask;

                if (success)
                {
                    _isProtectionActive = true;
                    _animationCts?.Cancel();
                    _animationCts = new CancellationTokenSource();

#if ANDROID
                    ServiceHelper.StartProtectionService();

                    // Show overlay bubble if permission granted
                    if (Settings.CanDrawOverlays(global::Android.App.Application.Context))
                    {
                        OverlayService.Show(global::Android.App.Application.Context);
                    }
#endif
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await AnimateToActiveState();
                        UpdateProtectionUI(true);
                        _ = PulseAnimation(_animationCts.Token);
                    });
                }
                else
                {
                    await ShowConnectionFailed();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Start Protection", ex);
                await ShowConnectionFailed();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task StopProtectionAsync()
        {
            try
            {
                _animationCts?.Cancel();
                _isProtectionActive = false;
                
                var stopTask = _audioService.StopStreamingAsync();
                await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(5)));

#if ANDROID
                ServiceHelper.StopProtectionService();
                OverlayService.Hide(global::Android.App.Application.Context);
#endif
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateProtectionUI(false);
                    AlertBanner.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Stop protection error: {ex.Message}");
            }
        }

        private void UpdateProtectionUI(bool isActive, bool connecting = false)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (connecting)
                {
                    ProtectionIconLabel.Text = "⏳";
                    StatusLabel.Text = T("Main_ProtectionConnecting");
                    ProtectionStatusLabel.Text = T("Main_ProtectionConnectingShort");
                    ShieldBorder.Stroke = Color.FromArgb("#FBBF24");
                    ToggleProtectionButton.IsEnabled = false;
                    ToggleProtectionButton.Text = T("Main_ButtonConnecting");
                }
                else if (isActive)
                {
                    ProtectionIconLabel.Text = "✓";
                    StatusLabel.Text = T("Main_ProtectionActive");
                    ProtectionStatusLabel.Text = T("Main_ProtectionProtecting");
                    ShieldBorder.Stroke = Color.FromArgb("#14B8A6");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = T("Main_DisableProtection");
                    ToggleProtectionButton.BackgroundColor = Color.FromArgb("#EF4444");
                }
                else
                {
                    ProtectionIconLabel.Text = "🛡️";
                    StatusLabel.Text = T("Main_ProtectionInactive");
                    ProtectionStatusLabel.Text = T("Main_ProtectionOff");
                    ShieldBorder.Stroke = Color.FromArgb("#5C6B7A");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = T("Main_EnableProtection");
                    ToggleProtectionButton.BackgroundColor = Color.FromArgb("#14B8A6");
                }
            });
        }

        private async Task ShowConnectionFailed()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UpdateProtectionUI(false);
                
                bool retry = await Application.Current.MainPage.DisplayAlert(
                    T("Main_ConnectionFailedTitle"),
                    T("Main_ConnectionFailedMessage"),
                    T("Main_Retry"),
                    T("Main_Settings")
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
            });
        }

        #endregion

        #region Audio Service Event Handlers

        private void OnAlertReceived(object sender, AlertEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (e.Alert == null) return;
                    
                    double riskScore = e.Alert.Confidence * 100;
                    
                    _stats.BlockedTotal++;
                    _stats.BlockedToday++;
                    if (e.Alert.Confidence >= AppConstants.HIGH_RISK_THRESHOLD)
                        _stats.SeriousThreats++;
                    UpdateStatsDisplay();

                    if (e.Alert.Confidence >= AppConstants.HIGH_RISK_THRESHOLD)
                        await HandleHighRiskAlert(e.Alert, riskScore);
                    else
                        await HandleLowRiskAlert(e.Alert, riskScore);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Alert processing error: {ex.Message}");
                }
            });
        }

        private void OnErrorOccurred(object sender, Services.ErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Error: {e.Message}");

                    // Show connection error in status for connection issues
                    if (e.Message?.Contains("Connection") == true ||
                        e.Message?.Contains("WebSocket") == true ||
                        e.Message?.Contains("Reconnect") == true)
                    {
                        StatusLabel.Text = T("Main_ConnectionIssue");
                        StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                    }
                }
                catch { }
            });
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Connection: {e.IsConnected} - {e.Message}");

                    if (e.IsConnected)
                    {
                        _isProtectionActive = true;
                        UpdateProtectionUI(true);
                    }
                    else if (!_isConnecting && _isProtectionActive)
                    {
                        // Connection lost while protection was active
                        StatusLabel.Text = T("Main_Reconnecting");
                        StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                    }
                }
                catch { }
            });
        }

        #endregion

        #region Alert Handling

        private async Task HandleHighRiskAlert(AlertData alert, double riskScore)
        {
            await AnimateToDangerState();
            TriggerVibration();
            ShowAlertBanner(alert, riskScore, isHighRisk: true);

#if ANDROID
            var context = global::Android.App.Application.Context;
            AlertNotificationHelper.ShowFraudAlert(context, alert.AlertType, riskScore, alert.Transcript);

            // Update overlay bubble with risk info
            OverlayService.Update(context, (int)riskScore, alert.DeepfakeScore,
                $"🚨 {alert.AlertType} - {riskScore:F0}%");
#endif

            string deepfakeWarning = alert.DeepfakeScore > 70
                ? $"\n🎭 Deepfake: {alert.DeepfakeScore}% (Giọng nói có thể giả mạo!)\n"
                : "";

            await DisplayAlert(
                T("Main_HighRiskTitle"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    T("Main_HighRiskMessage"),
                    alert.AlertType,
                    riskScore.ToString("F0", CultureInfo.CurrentCulture),
                    alert.Transcript
                ),
                T("Main_HighRiskAcknowledge")
            );
        }

        private async Task HandleLowRiskAlert(AlertData alert, double riskScore)
        {
            // Unique ID per alert prevents race condition:
            // newer alert arriving during delay keeps its banner visible
            string thisAlertId = System.Guid.NewGuid().ToString();
            _lastBannerAlertId = thisAlertId;

            ShowAlertBanner(alert, riskScore, isHighRisk: false);
            await Task.Delay(AppConstants.ALERT_AUTO_DISMISS_DELAY);

            // Only dismiss if no newer alert replaced this one during the delay
            if (_lastBannerAlertId == thisAlertId && AlertBanner.IsVisible)
                AlertBanner.IsVisible = false;
        }
}
