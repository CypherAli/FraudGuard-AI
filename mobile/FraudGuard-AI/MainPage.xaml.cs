using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using FraudGuardAI.Constants;
using FraudGuardAI.Helpers;
using FraudGuardAI.Services;
using FraudGuardAI.Models;
#if ANDROID
using FraudGuardAI.Platforms.Android.Services;
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

        #endregion

        #region Constructor

        public MainPage()
        {
            InitializeComponent();
            InitializeAudioService();
            
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
                // Load real stats from backend API
                string deviceId = SettingsPage.GetDeviceID();
                var historyService = new HistoryService();
                
                // Get all call history to calculate stats
                var allCalls = await historyService.GetHistoryAsync(deviceId, limit: 1000);
                var fraudCalls = allCalls.Where(c => c.IsFraud).ToList();
                
                // Calculate real stats
                _stats.BlockedTotal = fraudCalls.Count;
                _stats.BlockedToday = fraudCalls.Count(c => c.Timestamp.Date == DateTime.Today);
                // Convert HIGH_RISK_THRESHOLD (80.0) to 0-1 scale (0.8) for comparison
                _stats.SeriousThreats = fraudCalls.Count(c => c.Confidence >= (AppConstants.HIGH_RISK_THRESHOLD / 100.0));
                
                // Calculate efficiency: (fraud detected / total calls) * 100
                if (allCalls.Count > 0)
                {
                    _stats.ProtectionEfficiency = (fraudCalls.Count / (double)allCalls.Count) * 100;
                }
                else
                {
                    _stats.ProtectionEfficiency = 0;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MainPage] Stats loaded: {_stats.BlockedTotal} blocked, {_stats.ProtectionEfficiency:F1}% efficiency");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Failed to load stats: {ex.Message}");
                // Keep zero values if API fails
            }
            finally
            {
                UpdateStatsDisplay();
            }
        }

        private async Task LoadDashboardStatsAsync()
        {
            LoadDashboardStats();
            await Task.CompletedTask;
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
                    WeeklyChangeLabel.Text = $"↑ +{_stats.WeeklyChange} tuần này";
                
                EfficiencyChangeLabel.IsVisible = _stats.EfficiencyChange > 0;
                if (EfficiencyChangeLabel.IsVisible)
                    EfficiencyChangeLabel.Text = $"↑ {_stats.EfficiencyChangeDisplay}";
                
                BlockRateLabel.Text = !_isProtectionActive || _stats.BlockedTotal == 0
                    ? "Chưa có dữ liệu"
                    : $"Tỷ lệ chặn: {_stats.EfficiencyDisplay}";
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
                    await DisplayAlert("Thiếu quyền",
                        "Cần cấp quyền Microphone và Notification để bảo vệ hoạt động.", "OK");
                    return;
                }
                await StartProtectionAsync();
            }
        }

        private async void OnReportButtonClicked(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync("Báo cáo số mới", "Nhập số điện thoại lừa đảo:",
                "Báo cáo", "Hủy", placeholder: "0912345678", keyboard: Keyboard.Telephone);

            if (!string.IsNullOrEmpty(result))
                await DisplayAlert("Thành công", $"Đã báo cáo số {result}", "OK");
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
#endif
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateProtectionUI(false);
                    AlertBanner.IsVisible = false;
                });
            }
            catch { }
        }

        private void UpdateProtectionUI(bool isActive, bool connecting = false)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (connecting)
                {
                    ProtectionIconLabel.Text = "⏳";
                    StatusLabel.Text = "Đang kết nối...";
                    ProtectionStatusLabel.Text = "Đang kết nối";
                    ShieldBorder.Stroke = Color.FromArgb("#FBBF24");
                    ToggleProtectionButton.IsEnabled = false;
                    ToggleProtectionButton.Text = "Đang kết nối...";
                }
                else if (isActive)
                {
                    ProtectionIconLabel.Text = "✅";
                    StatusLabel.Text = "Bảo vệ đang hoạt động";
                    ProtectionStatusLabel.Text = "Đang bảo vệ";
                    ShieldBorder.Stroke = Color.FromArgb("#14B8A6");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = "Tắt bảo vệ";
                    ToggleProtectionButton.BackgroundColor = Color.FromArgb("#EF4444");
                }
                else
                {
                    ProtectionIconLabel.Text = "🛡️";
                    StatusLabel.Text = "Chưa kích hoạt";
                    ProtectionStatusLabel.Text = "Đã tắt";
                    ShieldBorder.Stroke = Color.FromArgb("#5C6B7A");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = "Kích hoạt bảo vệ";
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
                    "Kết nối thất bại",
                    "Không thể kết nối đến máy chủ bảo vệ.\n\n" +
                    "• Kiểm tra địa chỉ Server trong Cài đặt\n" +
                    "• Đảm bảo server đang chạy\n" +
                    "• Kiểm tra kết nối mạng",
                    "Thử lại", "Cài đặt"
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
                    if (riskScore >= AppConstants.HIGH_RISK_THRESHOLD)
                        _stats.SeriousThreats++;
                    UpdateStatsDisplay();

                    if (riskScore >= AppConstants.HIGH_RISK_THRESHOLD)
                        await HandleHighRiskAlert(e.Alert, riskScore);
                    else
                        await HandleLowRiskAlert(e.Alert, riskScore);
                }
                catch { }
            });
        }

        private void OnErrorOccurred(object sender, Services.ErrorEventArgs e) { }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e) { }

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
#endif

            await DisplayAlert(
                "⚠️ NGUY HIỂM CAO",
                $"Phát hiện dấu hiệu lừa đảo!\n\n" +
                $"Loại: {alert.AlertType}\n" +
                $"Mức độ rủi ro: {riskScore:F0}%\n" +
                $"Nội dung: {alert.Transcript}\n\n" +
                $"Hãy cân nhắc kết thúc cuộc gọi ngay.",
                "Đã hiểu"
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

            AlertTypeLabel.Text = isHighRisk ? "Phát hiện rủi ro cao" : alert.AlertType;
            AlertMessageLabel.Text = string.IsNullOrEmpty(alert.Transcript)
                ? "Phát hiện hoạt động đáng ngờ"
                : alert.Transcript;
            AlertConfidenceLabel.Text = $"Mức độ rủi ro: {riskScore:F0}%";
        }

        #endregion

        #region Animations

        private async Task AnimateToActiveState()
        {
            await Task.WhenAll(
                ShieldBorder.ScaleTo(0.95, 150, Easing.CubicOut)
            );

            ShieldBorder.Stroke = Color.FromArgb("#14B8A6");
            
            await ShieldBorder.ScaleTo(1, 200, Easing.SpringOut);
        }

        private async Task AnimateToDangerState()
        {
            await ShieldBorder.ScaleTo(0.95, 100);
            
            ShieldBorder.Stroke = AppConstants.DangerColor;
            StatusLabel.Text = "⚠️ PHÁT HIỆN MỐI ĐE DỌA";
            StatusLabel.TextColor = Color.FromArgb("#FCA5A5");

            await ShieldBorder.ScaleTo(1.05, 150, Easing.SpringOut);
            await ShieldBorder.ScaleTo(1, 100);
        }

        private async Task PulseAnimation(CancellationToken ct)
        {
            if (_pulseAnimationRunning) return;
            _pulseAnimationRunning = true;
            try
            {
                while (_isProtectionActive && !ct.IsCancellationRequested)
                {
                    await ShieldBorder.ScaleTo(1.05, 1000, Easing.SinInOut);
                    if (ct.IsCancellationRequested) break;
                    await ShieldBorder.ScaleTo(1.0, 1000, Easing.SinInOut);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                _pulseAnimationRunning = false;
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
                System.Diagnostics.Debug.WriteLine($"[MainPage] Vibration Error: {ex.Message}");
            }
        }

        #endregion

        #region Lifecycle

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Cancel any stale animations
            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();
            
            // Reattach to shared service
            var sharedService = App.GetAudioService();
            if (sharedService != null)
            {
                _audioService = sharedService;
                _isProtectionActive = _audioService.IsStreaming;
                
                // Re-attach event handlers
                _audioService.AlertReceived -= OnAlertReceived;
                _audioService.ErrorOccurred -= OnErrorOccurred;
                _audioService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                
                _audioService.AlertReceived += OnAlertReceived;
                _audioService.ErrorOccurred += OnErrorOccurred;
                _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;
                
                // Update UI to reflect current state
                if (_isProtectionActive)
                {
                    var ct = _animationCts.Token;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdateProtectionUI(true);
                        if (!_pulseAnimationRunning)
                        {
                            _ = PulseAnimation(ct);
                        }
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdateProtectionUI(false);
                    });
                }
            }
            
            // Refresh stats
            UpdateStatsDisplay();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Cancel animations
            _animationCts?.Cancel();
            
            // Only detach event handlers
            if (_audioService != null)
            {
                _audioService.AlertReceived -= OnAlertReceived;
                _audioService.ErrorOccurred -= OnErrorOccurred;
                _audioService.ConnectionStatusChanged -= OnConnectionStatusChanged;
            }
        }

        #endregion
    }
}
