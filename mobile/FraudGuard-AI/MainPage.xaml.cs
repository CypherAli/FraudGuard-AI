using System;
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
            LoadDashboardStats();
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Init Error: {ex.Message}");
            }
        }

        private void LoadDashboardStats()
        {
            // Load stats from storage or API
            // For now, use default values
            UpdateStatsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BlockedTotalLabel.Text = _stats.BlockedTotalDisplay;
                BlockedTodayLabel.Text = _stats.BlockedTodayDisplay;
                ThreatsLabel.Text = _stats.ThreatsDisplay;
                EfficiencyLabel.Text = _stats.EfficiencyDisplay;
                WeeklyChangeLabel.Text = $"↑ {_stats.WeeklyChangeDisplay}";
                EfficiencyChangeLabel.Text = $"↑ {_stats.EfficiencyChangeDisplay}";
                BlockRateLabel.Text = $"Tỷ lệ chặn: {_stats.EfficiencyDisplay}";
            });
        }

        #endregion

        #region Button Event Handlers

        private async void OnReportButtonClicked(object sender, EventArgs e)
        {
            // Navigate to report page or show dialog
            string result = await DisplayPromptAsync(
                "Báo cáo số mới",
                "Nhập số điện thoại lừa đảo:",
                "Báo cáo",
                "Hủy",
                placeholder: "0912345678",
                keyboard: Keyboard.Telephone
            );

            if (!string.IsNullOrEmpty(result))
            {
                await DisplayAlert("Thành công", $"Đã báo cáo số {result}", "OK");
                // TODO: Send report to server
            }
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
                // Add timeout for connection attempt
                var connectionTask = _audioService.StartStreamingAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);
                
                bool success = false;
                if (completedTask == connectionTask)
                {
                    success = await connectionTask;
                }

                if (success)
                {
                    _isProtectionActive = true;
                    
                    // Create new animation token for this session
                    _animationCts?.Cancel();
                    _animationCts = new CancellationTokenSource();
                    var ct = _animationCts.Token;

                    // Start foreground service (Android)
#if ANDROID
                    ServiceHelper.StartProtectionService();
#endif

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await AnimateToActiveState();
                        UpdateProtectionUI(true);
                        _ = PulseAnimation(ct);
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
                // Cancel animations immediately
                _animationCts?.Cancel();
                _isProtectionActive = false;
                
                // Stop streaming with timeout
                var stopTask = _audioService.StopStreamingAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                await Task.WhenAny(stopTask, timeoutTask);

                // Stop foreground service (Android)
#if ANDROID
                ServiceHelper.StopProtectionService();
#endif

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateProtectionUI(false);
                    AlertBanner.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] StopProtectionAsync error: {ex.Message}");
            }
        }

        private void UpdateProtectionUI(bool isActive, bool connecting = false)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (connecting)
                {
                    StatusLabel.Text = "Đang kết nối...";
                    ProtectionStatusLabel.Text = "Đang kết nối";
                    ShieldBorder.Stroke = Color.FromArgb("#FBBF24");
                }
                else if (isActive)
                {
                    StatusLabel.Text = "Bảo vệ đang hoạt động";
                    ProtectionStatusLabel.Text = "Đang bảo vệ";
                    ShieldBorder.Stroke = Color.FromArgb("#14B8A6");
                }
                else
                {
                    StatusLabel.Text = "Chưa kích hoạt";
                    ProtectionStatusLabel.Text = "Đã tắt";
                    ShieldBorder.Stroke = Color.FromArgb("#5C6B7A");
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
                    "Thử lại",
                    "Cài đặt"
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
            System.Diagnostics.Debug.WriteLine($"[MainPage] Alert received: {e?.Alert?.AlertType}");
            
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var alert = e.Alert;
                    if (alert == null) return;
                    
                    double riskScore = alert.Confidence * 100;
                    
                    // Update stats
                    _stats.BlockedTotal++;
                    _stats.BlockedToday++;
                    if (riskScore >= AppConstants.HIGH_RISK_THRESHOLD)
                    {
                        _stats.SeriousThreats++;
                    }
                    UpdateStatsDisplay();

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
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Alert handling error: {ex.Message}");
                }
            });
        }

        private void OnErrorOccurred(object sender, Services.ErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error: {e.Message}");
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Connection: {e.Message}");
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
