using System;
using System.Linq;
using System.Net.Http;
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

        private AudioStreamingServiceLowLevel _audioService = null!;
        private readonly IAppSettings _settings;
        private readonly IHistoryService _historyService;
        private bool _isProtectionActive = false;
        private bool _isConnecting = false;
        private bool _voipCaptureActive = false;
        private bool _isHandlingExpiry = false;          // prevent duplicate expired-session dialogs
        private DateTime _lastTokenCheckTime = DateTime.MinValue; // throttle OnAppearing token checks
        private CancellationTokenSource? _animationCts;
        private DashboardStats _stats = new();
        // Tracks which banner alert started the auto-dismiss timer (prevents race condition)
        private string _lastBannerAlertId = string.Empty;
        // Stored handler reference so we can unsubscribe in OnDisappearing (prevents memory leak)
        private System.ComponentModel.PropertyChangedEventHandler _locChangeHandler;
        private Action<byte[], int>? _pcmDataHandler;

        // ── UI Animation fields ──
        private RadarDrawable _radarDrawable = new();
        private WaveformDrawable _waveformDrawable = new();
        private IDispatcherTimer? _radarTimer;
        private IDispatcherTimer? _waveformTimer;
        private IDispatcherTimer? _statusDotTimer;
        private bool _statusDotVisible = true;

        // ── Report bottom sheet state ──
        private string _selectedThreatLevel = "Medium";

        // ── Token validation ──
        private static readonly HttpClient _validationClient = new() { Timeout = TimeSpan.FromSeconds(8) };

        #endregion

        #region Constructor

        public MainPage(IAppSettings settings, IHistoryService historyService)
        {
            _settings = settings;
            _historyService = historyService;

            InitializeComponent();
            InitializeRadarAndWaveform();
            InitializeAudioService();

            // Store handler reference so we can unsubscribe in OnDisappearing
            _locChangeHandler = (_, __) =>
            {
                UpdateProtectionUI(_isProtectionActive, _isConnecting);
                UpdateStatsDisplay();
            };

            // Load dashboard stats asynchronously
            _ = LoadDashboardStatsAsync();

            // Auto-start protection if enabled in settings
            _ = AutoStartProtectionIfEnabledAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Subscribe to service events (paired with unsubscribe in OnDisappearing)
            if (_audioService != null)
            {
                _audioService.AlertReceived += OnAlertReceived;
                _audioService.ErrorOccurred += OnErrorOccurred;
                _audioService.ConnectionStatusChanged += OnConnectionStatusChanged;
                _audioService.SessionExpired += OnSessionExpiredFromService;
            }
            LocalizationResourceManager.Instance.PropertyChanged += _locChangeHandler;

            // Resume animation timers that were paused in OnDisappearing
            _radarTimer?.Start();
            _waveformTimer?.Start();
            _statusDotTimer?.Start();

            // Refresh dashboard stats each time user navigates back to MainPage
            _ = LoadDashboardStatsAsync();

            // Background token validation — runs silently, redirects to login if expired
            _ = CheckTokenOnResumeAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Unsubscribe to prevent memory leaks when page goes to background
            if (_audioService != null)
            {
                _audioService.AlertReceived -= OnAlertReceived;
                _audioService.ErrorOccurred -= OnErrorOccurred;
                _audioService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _audioService.SessionExpired -= OnSessionExpiredFromService;
                if (_pcmDataHandler != null)
                    _audioService.PcmDataAvailable -= _pcmDataHandler;
            }
            LocalizationResourceManager.Instance.PropertyChanged -= _locChangeHandler;

            // Pause animation timers while page is not visible — saves ~10% CPU/battery.
            // Timers are NOT destroyed; they resume in OnAppearing().
            _radarTimer?.Stop();
            _waveformTimer?.Stop();
            _statusDotTimer?.Stop();
        }

        private async Task AutoStartProtectionIfEnabledAsync()
        {
            try
            {
                // Wait a bit for UI to initialize
                await Task.Delay(1000);
                
                // Check if auto protection is enabled and not already active
                if (_settings.IsAutoProtectionEnabled() && !_isProtectionActive && !_isConnecting)
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

                // Wire PCM data to the waveform visualiser (fire-and-forget; no UI thread needed)
                _pcmDataHandler = (buf, len) => _waveformDrawable.UpdateFromPcm(buf, len);
                _audioService.PcmDataAvailable += _pcmDataHandler;

                // NOTE: event subscriptions moved to OnAppearing() to prevent memory leaks
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
                var baseUrl = _settings.GetApiBaseUrl();

                // ── 1. Blacklist count (public endpoint, no auth needed) ─────────
                // Shows total numbers blocked regardless of auth status.
                try
                {
                    using var publicHttp = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                    var blResp = await publicHttp.GetAsync($"{baseUrl}/api/blacklist");
                    if (blResp.IsSuccessStatusCode)
                    {
                        var blJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                            await blResp.Content.ReadAsStringAsync());
                        if (blJson.TryGetProperty("count", out var countEl))
                            _stats.BlockedTotal = countEl.GetInt32();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Blacklist fetch failed: {ex.Message}");
                }

                // ── 2. Call history (requires auth — handle 401 gracefully) ──────
                List<Models.CallLog> allCalls;
                try
                {
                    var deviceId = _settings.GetDeviceId();
                    allCalls = await _historyService.GetHistoryAsync(deviceId, limit: 1000);
                }
                catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401") || ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] History 401 — token expired or missing, skipping call stats");
                    allCalls = new List<Models.CallLog>();
                }

                // ── Single-pass aggregation — tránh lặp qua list nhiều lần ──────
                var sevenDaysAgo    = DateTime.Today.AddDays(-7);
                var fourteenDaysAgo = DateTime.Today.AddDays(-14);
                var today           = DateTime.Today;

                int fraudToday      = 0;
                int seriousThreats  = 0;
                int totalFraud      = 0;
                int thisWeekFraud   = 0;
                int lastWeekFraud   = 0;
                int thisWeekAll     = 0;
                int lastWeekAll     = 0;

                foreach (var c in allCalls)
                {
                    var callDate = c.Timestamp.ToLocalTime().Date;
                    bool isFraud = c.IsFraud;

                    if (isFraud)
                    {
                        totalFraud++;
                        if (callDate == today)                              fraudToday++;
                        if (c.Confidence >= AppConstants.CRITICAL_RISK_THRESHOLD) seriousThreats++;
                        if (callDate >= sevenDaysAgo)                       thisWeekFraud++;
                        else if (callDate >= fourteenDaysAgo)               lastWeekFraud++;
                    }

                    if (callDate >= sevenDaysAgo)                           thisWeekAll++;
                    else if (callDate >= fourteenDaysAgo)                   lastWeekAll++;
                }

                _stats.BlockedToday      = fraudToday;
                _stats.SeriousThreats    = seriousThreats;
                _stats.ProtectionEfficiency = allCalls.Count > 0
                    ? Math.Min(100, (totalFraud / (double)allCalls.Count) * 100) : 0;
                _stats.WeeklyChange = thisWeekFraud;

                double thisWeekEff = thisWeekAll > 0
                    ? Math.Min(100, (thisWeekFraud / (double)thisWeekAll) * 100) : 0;
                double lastWeekEff = lastWeekAll > 0
                    ? Math.Min(100, (lastWeekFraud / (double)lastWeekAll) * 100) : 0;

                // Only show positive change (improvement)
                _stats.EfficiencyChange = Math.Max(0, thisWeekEff - lastWeekEff);
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
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] OnToggleProtectionClicked error: {ex.Message}");
                    _isConnecting = false;
                    UpdateProtectionUI(false);
                }
            }
        }

        // ── Open bottom sheet ────────────────────────────────────
        private async void OnReportButtonClicked(object? sender, EventArgs e)
        {
            // Reset sheet state
            if (ReportPhoneEntry != null) ReportPhoneEntry.Text = string.Empty;
            if (ReportLabelEntry != null) ReportLabelEntry.Text = string.Empty;
            _selectedThreatLevel = "Medium";
            UpdateThreatLevelUI();
            if (SubmitReportButton != null)
            {
                SubmitReportButton.Text = "Report number"; // Reset text from previous "Submitting..." state
                SubmitReportButton.IsEnabled = false;
                SubmitReportButton.Opacity = 0.4;
            }

            // Show overlay + animate panel from below
            if (ReportSheetOverlay != null)
            {
                ReportSheetOverlay.IsVisible = true;
                ReportSheetOverlay.Opacity = 0;
                await ReportSheetOverlay.FadeTo(1, 200, Easing.CubicOut);
            }
            if (ReportSheetPanel != null)
            {
                ReportSheetPanel.TranslationY = 400;
                await ReportSheetPanel.TranslateTo(0, 0, 280, Easing.CubicOut);
            }
        }

        // ── Close bottom sheet ───────────────────────────────────
        private async void OnReportBackdropTapped(object? sender, TappedEventArgs e)
            => await HideReportSheet();

        private async void OnCloseReportSheetTapped(object? sender, TappedEventArgs e)
            => await HideReportSheet();

        private async Task HideReportSheet()
        {
            if (ReportSheetPanel != null)
                await ReportSheetPanel.TranslateTo(0, 400, 200, Easing.CubicIn);
            if (ReportSheetOverlay != null)
            {
                await ReportSheetOverlay.FadeTo(0, 150, Easing.Linear);
                ReportSheetOverlay.IsVisible = false;
            }
        }

        // ── Threat level buttons ─────────────────────────────────
        private void OnThreatLevelTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is string level)
            {
                _selectedThreatLevel = level;
                UpdateThreatLevelUI();
            }
        }

        private void UpdateThreatLevelUI()
        {
            // Reset all to unselected
            void SetUnselected(Border? border, Label? label)
            {
                if (border == null || label == null) return;
                border.BackgroundColor = Color.FromArgb("#1B2838");
                border.Stroke = new SolidColorBrush(Color.FromArgb("#2A3F54"));
                label.TextColor = Color.FromArgb("#6B7C8D");
            }
            void SetSelected(Border? border, Label? label)
            {
                if (border == null || label == null) return;
                border.BackgroundColor = Color.FromArgb("#F97316");
                border.Stroke = new SolidColorBrush(Color.FromArgb("#F97316"));
                label.TextColor = Colors.White;
            }

            SetUnselected(ThreatLowBorder, ThreatLowLabel);
            SetUnselected(ThreatMediumBorder, ThreatMediumLabel);
            SetUnselected(ThreatHighBorder, ThreatHighLabel);

            switch (_selectedThreatLevel)
            {
                case "Low":    SetSelected(ThreatLowBorder,    ThreatLowLabel);    break;
                case "Medium": SetSelected(ThreatMediumBorder, ThreatMediumLabel); break;
                case "High":   SetSelected(ThreatHighBorder,   ThreatHighLabel);   break;
            }
        }

        // ── Enable submit when phone is typed ───────────────────
        private void OnReportPhoneChanged(object? sender, TextChangedEventArgs e)
        {
            if (SubmitReportButton == null) return;
            bool hasPhone = !string.IsNullOrWhiteSpace(e.NewTextValue);
            SubmitReportButton.IsEnabled = hasPhone;
            SubmitReportButton.Opacity = hasPhone ? 1.0 : 0.4;
        }

        // ── Submit report ────────────────────────────────────────
        private async void OnSubmitReportClicked(object? sender, EventArgs e)
        {
            var phoneNumber = ReportPhoneEntry?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phoneNumber)) return;

            var label = ReportLabelEntry?.Text?.Trim() ?? string.Empty;
            var threat = _selectedThreatLevel;

            // Disable button to prevent double-submit and show loading state
            if (SubmitReportButton != null)
            {
                SubmitReportButton.IsEnabled = false;
                SubmitReportButton.Text = T("Main_ReportSubmitting");
                SubmitReportButton.Opacity = 0.7;
            }

            await HideReportSheet();

            try
            {
                var token    = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("auth_token");
                var deviceId = _settings.GetDeviceId();
                var baseUrl  = _settings.GetApiBaseUrl();

                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var req = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Post, $"{baseUrl}/api/report");

                if (!string.IsNullOrEmpty(token))
                    req.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    phone_number = phoneNumber,
                    device_id    = deviceId,
                    reason       = string.IsNullOrEmpty(label) ? "user_report" : label,
                    threat_level = threat.ToLowerInvariant()
                });
                req.Content = new System.Net.Http.StringContent(
                    payload, System.Text.Encoding.UTF8, "application/json");

                var response = await http.SendAsync(req);
                System.Diagnostics.Debug.WriteLine(
                    $"[MainPage] Report {phoneNumber} ({threat}) → HTTP {(int)response.StatusCode}");

                // Always add to local cache
                Services.BlacklistCacheService.Instance.AddToLocalCache(phoneNumber);
                _ = Services.BlacklistCacheService.Instance.SyncFromServerAsync();

                // Refresh stats so BlockedTotal updates immediately after report
                _ = LoadDashboardStatsAsync();

                string suffix = response.IsSuccessStatusCode
                    ? string.Empty
                    : response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? T("Main_ReportLocalSaveUnauth")
                        : T("Main_ReportLocalSaveError");

                await DisplayAlert(
                    T("Main_ReportSuccessTitle"),
                    string.Format(CultureInfo.CurrentCulture,
                        T("Main_ReportSuccessMessage"), phoneNumber) + suffix,
                    T("Common_OK"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Report error: {ex.Message}");
                Services.BlacklistCacheService.Instance.AddToLocalCache(phoneNumber);
                _ = LoadDashboardStatsAsync();
                await DisplayAlert(
                    T("Main_ReportSuccessTitle"),
                    string.Format(CultureInfo.CurrentCulture,
                        T("Main_ReportSuccessMessage"), phoneNumber)
                        + T("Main_ReportLocalSaveOffline"),
                    T("Common_OK"));
            }
        }

        #endregion

        #region Protection Control

        public async Task StartProtectionAsync()
        {
            if (_isConnecting) return;

            _isConnecting = true;
            try
            {
                UpdateProtectionUI(false, connecting: true);
                // Validate token with server BEFORE attempting WebSocket.
                // Catches expired/invalid tokens early (e.g. after Render.com cold-restart).
                bool tokenValid = await ValidateTokenWithServerAsync();
                if (!tokenValid)
                {
                    await HandleExpiredToken();
                    return;
                }

                var connectionTask = _audioService.StartStreamingAsync();
                // Allow up to 25 s — Render.com free tier can take ~15 s to cold-start
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(25));
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

                    // Offer Call-Shield dual-stream after brief delay (fire-and-forget)
                    _ = TryActivateCallShieldAsync();
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
                _voipCaptureActive = false;

                // StopStreamingAsync internally calls StopVoipCaptureAsync
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
            // Update radar/waveform visual state
            UpdateRadarState(connecting ? false : isActive);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (connecting)
                {
                    ProtectionIconLabel.Text = "⏳";
                    StatusLabel.Text = T("Main_ProtectionConnecting");
                    ProtectionStatusLabel.Text = T("Main_ProtectionConnectingShort");
                    ToggleProtectionButton.IsEnabled = false;
                    ToggleProtectionButton.Text = T("Main_ButtonConnecting");
                }
                else if (isActive)
                {
                    ProtectionIconLabel.Text = "✓";
                    StatusLabel.Text = T("Main_ProtectionActive");
                    ProtectionStatusLabel.Text = T("Main_ProtectionProtecting");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = T("Main_DisableProtection");
                }
                else
                {
                    ProtectionIconLabel.Text = "🛡️";
                    StatusLabel.Text = T("Main_ProtectionInactive");
                    ProtectionStatusLabel.Text = T("Main_ProtectionOff");
                    ToggleProtectionButton.IsEnabled = true;
                    ToggleProtectionButton.Text = T("Main_EnableProtection");
                }
            });
        }

        /// <summary>
        /// Calls /auth/validate-token to check if the stored token is still accepted by the server.
        /// Returns true if valid (or if server unreachable — let WebSocket handle it normally).
        /// Returns false only when server explicitly says 401 (token expired/invalid).
        /// </summary>
        private async Task<bool> ValidateTokenWithServerAsync()
        {
            try
            {
                var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                    return false; // No token at all → definitely need login

                var baseUrl = _settings.GetApiBaseUrl();
                var url = $"{baseUrl}/auth/validate-token";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _validationClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] 🔑 Token invalid on server (401) — need re-auth");
                    return false;
                }
                // Any non-401 response (200, timeout exception, network error) → assume valid
                return true;
            }
            catch (TaskCanceledException)
            {
                // Timeout — server might be cold-starting, let WebSocket try anyway
                System.Diagnostics.Debug.WriteLine("[MainPage] ⏱ Token validation timed out — proceeding with WebSocket");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] ⚠ Token validation error: {ex.Message} — proceeding");
                return true;
            }
        }

        /// <summary>
        /// Called when server returns 401 on token validation.
        /// Shows a clear dialog and navigates to login with email pre-filled.
        /// </summary>
        private async Task HandleExpiredToken()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UpdateProtectionUI(false);

                bool relogin = await (Application.Current?.MainPage ?? this).DisplayAlert(
                    "Phiên đăng nhập hết hạn",
                    "Server đã khởi động lại, token cũ không còn hiệu lực.\n\nVui lòng đăng nhập lại — chỉ cần nhập OTP qua email, nhanh thôi!",
                    "Đăng nhập lại",
                    "Để sau"
                );

                if (relogin)
                {
                    // Clear old token so LoginPage detects the expired state
                    try { Microsoft.Maui.Storage.SecureStorage.Default.Remove("auth_token"); } catch { }
                    // Navigate to login (email stays in SecureStorage for pre-fill)
                    Application.Current!.MainPage = new NavigationPage(new Pages.Auth.LoginPage());
                }
            });
        }

        /// <summary>
        /// Called by AudioService.SessionExpired event when WebSocket upgrade returns 401
        /// or server sends PolicyViolation (1008) close frame.
        /// </summary>
        private void OnSessionExpiredFromService(object? sender, EventArgs e)
        {
            // Guard: only show dialog once even if multiple events fire simultaneously
            if (_isHandlingExpiry) return;
            _isHandlingExpiry = true;
            _ = HandleExpiredToken().ContinueWith(_ => _isHandlingExpiry = false);
        }

        /// <summary>
        /// Validates the stored token against the server on app resume.
        /// Throttled to once per 30 minutes to avoid spamming the server.
        /// Silently redirects to login if token is expired — only when protection is NOT active.
        /// </summary>
        private async Task CheckTokenOnResumeAsync()
        {
            try
            {
                // Throttle: skip if checked recently
                if ((DateTime.UtcNow - _lastTokenCheckTime).TotalMinutes < 30) return;

                // Skip if protection is currently active (ValidateTokenWithServerAsync runs before start anyway)
                if (_isProtectionActive || _isConnecting) return;

                var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token)) return; // Not logged in — nothing to check

                _lastTokenCheckTime = DateTime.UtcNow;
                var valid = await ValidateTokenWithServerAsync();
                if (!valid && !_isHandlingExpiry)
                {
                    _isHandlingExpiry = true;
                    await HandleExpiredToken();
                    _isHandlingExpiry = false;
                }
            }
            catch
            {
                // Network error on resume — skip silently, will be caught when user starts protection
            }
        }

        private async Task ShowConnectionFailed()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UpdateProtectionUI(false);

                bool retry = await (Application.Current?.MainPage ?? this).DisplayAlert(
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

#if ANDROID
        /// <summary>
        /// Prompts the user to enable Call-Shield (dual-stream VoIP capture).
        /// Shows the MediaProjection system dialog and starts VoIP capture if granted.
        /// Runs as a fire-and-forget task — does not block protection startup.
        /// </summary>
        private async Task TryActivateCallShieldAsync()
        {
            try
            {
                // Let protection-start animations finish before showing another dialog
                await Task.Delay(900);

                if (!_isProtectionActive) return;

                bool accepted = await MainThread.InvokeOnMainThreadAsync(() =>
                    (Application.Current?.MainPage ?? this).DisplayAlert(
                        T("Main_CallShieldTitle"),
                        T("Main_CallShieldMessage"),
                        T("Main_CallShieldEnable"),
                        T("Main_CallShieldLater")
                    )
                );

                if (!accepted || !_isProtectionActive) return;

                System.Diagnostics.Debug.WriteLine("[MainPage] Call-Shield accepted — requesting MediaProjection...");

                // System dialog: "FraudGuard wants to start recording"
                var projection = await FraudGuardAI.MainActivity.RequestMediaProjectionAsync();

                if (projection == null)
                {
                    // Either user denied OR SecurityException (ForegroundService lacks TypeMediaProjection).
                    // VoIP capture is unavailable.  Only attempt PSTN SCO immediately if there
                    // is already an active phone call (user opened Call-Shield mid-call).
                    // Otherwise, CallStateReceiver will start PSTN SCO automatically on OFFHOOK —
                    // no need to hold AudioMode.InCall 24/7 waiting for a future call.
                    System.Diagnostics.Debug.WriteLine("[MainPage] MediaProjection unavailable — falling back to SCO");
#if ANDROID
                    if (FraudGuardAI.Platforms.Android.Services.CallStateReceiver.IsCallActive)
                        _ = TryPstnScoFallbackAsync();
                    else
                        System.Diagnostics.Debug.WriteLine("[MainPage] No active call — PSTN SCO will start on next OFFHOOK via CallStateReceiver");
#endif
                    return;
                }

                // Subscribe to VoIP status BEFORE starting so we catch PSTN_OR_INIT_FAILED / PSTN_DETECTED
                Action<string>? voipStatusHandler = null;
                voipStatusHandler = (status) =>
                {
                    if (status == "PSTN_DETECTED")
                    {
                        _audioService.VoipStatusChanged -= voipStatusHandler;
                        // VoIP confirmed this is a PSTN call — silently attempt SCO capture
                        _ = TryPstnScoFallbackAsync();
                    }
                };
                _audioService.VoipStatusChanged += voipStatusHandler;

                bool voipStarted = await _audioService.StartVoipCaptureAsync(projection);
                _voipCaptureActive = voipStarted;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (voipStarted)
                    {
                        StatusLabel.Text = T("Main_StatusFullProtectionVoip");
                        StatusLabel.TextColor = Color.FromArgb("#22D3EE");
                    }
                    else
                    {
                        // Immediate init failure (PSTN_OR_INIT_FAILED or Android < 10)
                        _audioService.VoipStatusChanged -= voipStatusHandler; // clean up
                        StatusLabel.Text = T("Main_StatusTryingCallAudio");
                        StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                    }
                });

                // If VoIP init failed immediately, try SCO only during an active call.
                // (Same rule: avoid occupying AudioMode.InCall outside of a real call.)
                if (!voipStarted)
                {
#if ANDROID
                    if (FraudGuardAI.Platforms.Android.Services.CallStateReceiver.IsCallActive)
                        _ = TryPstnScoFallbackAsync();
                    else
                        System.Diagnostics.Debug.WriteLine("[MainPage] VoIP failed, no active call — PSTN SCO deferred to next OFFHOOK");
#endif
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Call-Shield error: {ex.Message}");
            }
        }

        /// <summary>
        /// Fallback: try Virtual BT HFP (SCO) capture when VoIP capture fails/detects PSTN.
        /// Silently tries 4 strategies — only shows dialog if ALL strategies fail.
        /// </summary>
        private async Task TryPstnScoFallbackAsync()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = T("Main_StatusScoStarting");
                    StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                });

                bool scoStarted = await _audioService.StartPstnScoAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (scoStarted)
                    {
                        StatusLabel.Text = T("Main_StatusFullProtectionSco");
                        StatusLabel.TextColor = Color.FromArgb("#22D3EE");
                        System.Diagnostics.Debug.WriteLine("[MainPage] PSTN SCO capture STARTED ✅");
                    }
                    else
                    {
                        // All SCO strategies failed — fall back to speakerphone tip
                        StatusLabel.Text = T("Main_StatusSpeakerphoneTip");
                        StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                        _ = (Application.Current?.MainPage ?? this).DisplayAlert(
                            T("Main_CallShieldScoFailTitle"),
                            T("Main_CallShieldScoFailMessage"),
                            T("Common_OK")
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] TryPstnScoFallbackAsync error: {ex.Message}");
            }
        }
#endif

        #endregion

        #region Audio Service Event Handlers

        private void OnAlertReceived(object? sender, Services.AlertEventArgs e)
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

        private void OnErrorOccurred(object? sender, Services.ErrorEventArgs e)
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

        private void OnConnectionStatusChanged(object? sender, Services.ConnectionStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Connection: {e.IsConnected} - {e.Status}");

                    if (e.IsConnected)
                    {
                        // Only mirror the "active" UI if the FLAG still says we should be protecting.
                        // Without this guard: StopProtectionAsync sets _isProtectionActive=false, then
                        // a stale Connected event from a lingering reconnect fires here and calls
                        // UpdateProtectionUI(true) — button text becomes "Tắt bảo vệ" while the flag
                        // is false → next click starts protection instead of stopping. Bug = 2 clicks.
                        if (_isProtectionActive)
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

        #endregion

        #region UI Animation & Feedback

        private async Task AnimateToActiveState()
        {
            try
            {
                if (ShieldBorder != null)
                    await ShieldBorder.ScaleTo(1.1, 200, Easing.CubicOut);
                if (ShieldBorder != null)
                    await ShieldBorder.ScaleTo(1.0, 150, Easing.CubicIn);
            }
            catch { }
        }

        private async Task AnimateToDangerState()
        {
            try
            {
                if (ShieldBorder != null)
                {
                    await ShieldBorder.ScaleTo(1.3, 150, Easing.CubicOut);
                    await ShieldBorder.ScaleTo(1.0, 100, Easing.CubicIn);
                }
            }
            catch { }
        }

        private async Task PulseAnimation(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (ShieldBorder != null)
                    {
                        await ShieldBorder.ScaleTo(1.05, 1000, Easing.SinInOut);
                        await ShieldBorder.ScaleTo(1.0, 1000, Easing.SinInOut);
                    }
                    await Task.Delay(500, token);
                }
            }
            catch (TaskCanceledException) { }
            catch { }
        }

        private void TriggerVibration()
        {
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Vibration error: {ex.Message}");
            }
        }

        private void ShowAlertBanner(AlertData alert, double riskScore, bool isHighRisk)
        {
            try
            {
                if (AlertBanner == null) return;

                AlertBanner.BackgroundColor = isHighRisk
                    ? Color.FromArgb("#DC2626")
                    : Color.FromArgb("#F59E0B");

                if (AlertTypeLabel != null)
                    AlertTypeLabel.Text = alert.AlertType ?? (isHighRisk ? "HIGH RISK" : "SUSPICIOUS");
                if (AlertConfidenceLabel != null)
                    AlertConfidenceLabel.Text = $"{riskScore:F0}%";
                if (AlertMessageLabel != null)
                    AlertMessageLabel.Text = alert.Transcript ?? alert.Message ?? "";

                AlertBanner.IsVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] ShowAlertBanner error: {ex.Message}");
            }
        }

        #endregion

        #region Localization Helper

        private static string T(string key) => Localization.LocalizationResourceManager.Instance[key];

        #endregion

        #region Radar & Waveform Animation

        private void InitializeRadarAndWaveform()
        {
            try
            {
                // Assign drawables to GraphicsViews
                RadarView.Drawable = _radarDrawable;
                WaveformView.Drawable = _waveformDrawable;

                // Radar timer ~60 fps
                _radarTimer = Dispatcher.CreateTimer();
                _radarTimer.Interval = TimeSpan.FromMilliseconds(16);
                _radarTimer.Tick += (_, _) =>
                {
                    float speed = _radarDrawable.IsActive ? 0.025f : 0.012f;
                    _radarDrawable.Angle = (_radarDrawable.Angle + speed) % (float)(Math.PI * 2);
                    RadarView.Invalidate();
                };
                _radarTimer.Start();

                // Waveform timer ~30 fps
                _waveformTimer = Dispatcher.CreateTimer();
                _waveformTimer.Interval = TimeSpan.FromMilliseconds(33);
                _waveformTimer.Tick += (_, _) =>
                {
                    _waveformDrawable.Advance(0.033f);
                    WaveformView.Invalidate();
                };
                _waveformTimer.Start();

                // Status dot blink timer
                _statusDotTimer = Dispatcher.CreateTimer();
                _statusDotTimer.Interval = TimeSpan.FromMilliseconds(900);
                _statusDotTimer.Tick += (_, _) =>
                {
                    _statusDotVisible = !_statusDotVisible;
                    SystemStatusDot.Opacity = _statusDotVisible ? 1.0 : 0.25;
                };
                _statusDotTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Radar init error: {ex.Message}");
            }
        }

        private void UpdateRadarState(bool isActive)
        {
            _radarDrawable.IsActive = isActive;
            _waveformDrawable.IsActive = isActive;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Update card gradient dynamically (x:Name on brushes not supported in MAUI)
                    if (ShieldBorder != null)
                    {
                        var gradient = new LinearGradientBrush
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(1, 1)
                        };
                        gradient.GradientStops.Add(new GradientStop(
                            isActive ? Color.FromArgb("#1414B8A6") : Color.FromArgb("#14EF4444"), 0));
                        gradient.GradientStops.Add(new GradientStop(Color.FromArgb("#000A0A0A"), 1));
                        ShieldBorder.Background = gradient;
                        ShieldBorder.Stroke = new SolidColorBrush(
                            isActive ? Color.FromArgb("#3314B8A6") : Color.FromArgb("#25EF4444"));
                    }

                    // Update status dot color
                    if (SystemStatusDot != null)
                        SystemStatusDot.Color = isActive
                            ? Color.FromArgb("#2DD4BF")
                            : Color.FromArgb("#EF4444");

                    // Update status text color
                    if (ProtectionStatusLabel != null)
                        ProtectionStatusLabel.TextColor = isActive
                            ? Color.FromArgb("#2DD4BF")
                            : Color.FromArgb("#EF4444");

                    // Update toggle button style
                    if (ToggleProtectionButton != null)
                    {
                        ToggleProtectionButton.BorderColor = isActive
                            ? Color.FromArgb("#EF4444")
                            : Color.FromArgb("#2DD4BF");
                        ToggleProtectionButton.TextColor = isActive
                            ? Color.FromArgb("#EF4444")
                            : Color.FromArgb("#2DD4BF");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] UpdateRadarState error: {ex.Message}");
                }
            });
        }

        #endregion
    }
}
