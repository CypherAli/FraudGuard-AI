using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using FraudGuardAI;                  // App, AppConstants
using FraudGuardAI.Constants;
using FraudGuardAI.Services;
using FraudGuardAI.Pages.Auth;
using FraudGuardAI.Localization;
using System.Globalization;

namespace FraudGuardAI.Pages
{
    public partial class SettingsPage : ContentPage
    {
        #region Constants

        private const string PREF_SERVER_URL = "ServerURL";  // Changed from ServerIP to support full URLs
        private const string PREF_USB_MODE = "UsbMode";
        private const string PREF_AUTO_PROTECTION = "AutoProtection";  // Enable/Disable auto protection
        private const string PREF_LIGHT_THEME = "LightThemeEnabled";
        private const string PREF_LIGHT_THEME_USER_SET = "LightThemeUserSet";
        private const string PREF_APP_LANGUAGE = "AppLanguage";
        private const string PREF_AVATAR_PATH = "AvatarImagePath";
        private const string DEFAULT_URL = AppConstants.PRODUCTION_SERVER_URL;  // Use production by default
        private const string USB_URL = AppConstants.USB_SERVER_URL; // For emulator
        
        // Legacy support for migration
        private const string LEGACY_PREF_SERVER_IP = "ServerIP";

        private readonly Color SuccessColor = Color.FromArgb("#34D399");
        private readonly IAuthenticationService? _authService;

        #endregion

        #region Constructor

        public SettingsPage(IAuthenticationService? authService = null)
        {
            try
            {
                InitializeComponent();
                _authService = authService;

                if (_authService == null)
                    System.Diagnostics.Debug.WriteLine("[SettingsPage] WARNING: AuthService is null");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Constructor Error: {ex.Message}");
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
                LoadUserInfo();
                CheckServerConnection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] OnAppearing Error: {ex.Message}");
            }
        }
        
        private async void LoadUserInfo()
        {
            try
            {
                // Check if auth service is available
                if (_authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SettingsPage] AuthService is null, skipping user info load");
                    return;
                }

                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    // Preferences takes priority (updated by Edit Profile); fall back to SecureStorage value
                    var prefName = Preferences.Get("DisplayName", null as string);
                    var displayName = !string.IsNullOrEmpty(prefName)
                        ? prefName
                        : user.DisplayName ?? LocalizationResourceManager.Instance["Settings_DefaultUserName"];

                    if (UserNameLabel != null)
                        UserNameLabel.Text = displayName;
                    if (UserEmailLabel != null)
                        UserEmailLabel.Text = user.Email ?? "user@example.com";
                    if (AvatarInitials != null)
                        AvatarInitials.Text = GetInitials(displayName);

                    // ── Phone number: SecureStorage → API profile → auto-detect ──
                    string? storedPhone = await SecureStorage.Default.GetAsync("phone_number");

                    if (string.IsNullOrWhiteSpace(storedPhone))
                        storedPhone = string.IsNullOrEmpty(user.PhoneNumber) ? null : user.PhoneNumber;

                    if (string.IsNullOrWhiteSpace(storedPhone))
                        storedPhone = await TryDetectDevicePhoneNumberAsync();

                    if (PhoneNumberLabel != null)
                        PhoneNumberLabel.Text = !string.IsNullOrWhiteSpace(storedPhone)
                            ? storedPhone
                            : LocalizationResourceManager.Instance["Settings_NotUpdated"];
                }

                LoadAvatarImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error loading user info: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to read the device SIM phone number via TelephonyManager.
        /// Returns the number if found and persists it to SecureStorage for future use.
        /// Returns null when the SIM number is not available (most carriers don't provision it).
        /// </summary>
        private async Task<string?> TryDetectDevicePhoneNumberAsync()
        {
#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;

                // READ_PHONE_NUMBERS (API 33+) or READ_PHONE_STATE (pre-33) required.
                // READ_PHONE_STATE is already declared in AndroidManifest.xml.
                var status = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Phone>();

                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[SettingsPage] Phone permission not granted — cannot auto-detect number");
                    return null;
                }

                var telephony = (Android.Telephony.TelephonyManager?)
                    context.GetSystemService(global::Android.Content.Context.TelephonyService);

                string? line1 = telephony?.Line1Number;

                if (!string.IsNullOrWhiteSpace(line1) &&
                    line1 != "Unknown" && line1 != "+1" && line1.Length >= 7)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsPage] Auto-detected phone: {line1}");
                    await SecureStorage.Default.SetAsync("phone_number", line1);
                    return line1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] TelephonyManager error: {ex.Message}");
            }
#endif
            return null;
        }

        private void LoadAvatarImage()
        {
            try
            {
                string savedPath = Preferences.Get(PREF_AVATAR_PATH, string.Empty);
                if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                {
                    SetAvatarImage(savedPath);
                }
                else
                {
                    if (AvatarImage != null)
                        AvatarImage.IsVisible = false;
                    if (AvatarInitials != null)
                        AvatarInitials.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error loading avatar: {ex.Message}");
            }
        }

        private void SetAvatarImage(string filePath)
        {
            if (AvatarImage == null || AvatarInitials == null)
                return;

            AvatarImage.Source = ImageSource.FromFile(filePath);
            AvatarImage.IsVisible = true;
            AvatarInitials.IsVisible = false;
        }
        
        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "ND";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
        
        private void CheckServerConnection()
        {
            try
            {
                var audioService = App.GetAudioService();
                bool isConnected = audioService?.IsConnected ?? false;
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ServerStatusDot != null)
                        ServerStatusDot.BackgroundColor = isConnected ? SuccessColor : Color.FromArgb("#5C6B7A");
                    if (ServerStatusLabel != null)
                    {
                        ServerStatusLabel.Text = isConnected
                            ? LocalizationResourceManager.Instance["Settings_ServerConnected"]
                            : LocalizationResourceManager.Instance["Settings_ServerDisconnected"];
                        ServerStatusLabel.TextColor = isConnected ? SuccessColor : Color.FromArgb("#8B9CAF");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error checking connection: {ex.Message}");
            }
        }
        
        private void UpdateCurrentConfig()
        {
            try
            {
                bool usbMode = Preferences.Get(PREF_USB_MODE, false);
                string baseUrl = usbMode ? USB_URL : Preferences.Get(PREF_SERVER_URL, DEFAULT_URL);
                UpdateConfigurationDisplay(baseUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                // Load USB Mode preference
                bool usbMode = Preferences.Get(PREF_USB_MODE, false);
                if (UsbModeSwitch != null)
                    UsbModeSwitch.IsToggled = usbMode;

                // Load Auto Protection preference
                bool autoProtection = Preferences.Get(PREF_AUTO_PROTECTION, true);  // Default to enabled
                if (AutoProtectionSwitch != null)
                    AutoProtectionSwitch.IsToggled = autoProtection;

                // Load Auto-Reject preference
                bool autoReject = Preferences.Get("AutoRejectBlacklisted", false);
                if (AutoRejectSwitch != null)
                    AutoRejectSwitch.IsToggled = autoReject;

                // Load SMS Detection preference
                bool smsDetection = Preferences.Get("SmsDetectionEnabled", false);
                if (SmsDetectionSwitch != null)
                    SmsDetectionSwitch.IsToggled = smsDetection;

                bool lightThemeUserSet = Preferences.Get(PREF_LIGHT_THEME_USER_SET, false);
                bool lightThemeEnabled = lightThemeUserSet && Preferences.Get(PREF_LIGHT_THEME, false);
                if (!lightThemeUserSet)
                {
                    Preferences.Set(PREF_LIGHT_THEME, false);
                }
                if (DarkModeSwitch != null)
                    DarkModeSwitch.IsToggled = lightThemeEnabled;
                ApplyTheme(lightThemeEnabled);

                // Get saved URL or use default
                string savedURL = Preferences.Get(PREF_SERVER_URL, "");
                if (string.IsNullOrEmpty(savedURL))
                {
                    // Use production server as default
                    savedURL = DEFAULT_URL;
                    Preferences.Set(PREF_SERVER_URL, savedURL);
                    System.Diagnostics.Debug.WriteLine($"[Settings] No saved URL, using default: {savedURL}");
                }
                
                // Clean up legacy preference if it exists (one-time migration)
                if (Preferences.ContainsKey(LEGACY_PREF_SERVER_IP))
                {
                    Preferences.Remove(LEGACY_PREF_SERVER_IP);
                    System.Diagnostics.Debug.WriteLine($"[Settings] Removed legacy ServerIP preference");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Settings] Loaded URL: {savedURL}");
                if (ServerIPEntry != null)
                    ServerIPEntry.Text = savedURL;
                
                // Update config display
                UpdateConfigurationDisplay(usbMode ? USB_URL : savedURL);
                
                System.Diagnostics.Debug.WriteLine($"[Settings] LoadSettings completed successfully (USB Mode: {usbMode})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
            }
        }

        private async void SaveServerIP()
        {
            try
            {
                string? url = ServerIPEntry?.Text?.Trim();

                if (string.IsNullOrWhiteSpace(url))
                {
                    await DisplayAlert(T("Common_Error"), T("Settings_Test_Error_EmptyUrl"), T("Common_OK"));
                    return;
                }

                // Accept both full URLs and IP addresses
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    // Assume it's an IP, add http:// and port
                    if (IsValidIP(url))
                    {
                        url = $"http://{url}:8080";
                    }
                    else
                    {
                        await DisplayAlert(T("Common_Error"), T("Settings_Save_InvalidUrl"), T("Common_OK"));
                        return;
                    }
                }

                // Auto-upgrade to HTTPS for known production domains
                if (url.StartsWith("http://") &&
                    (url.Contains("onrender.com") || url.Contains("railway.app") || url.Contains("herokuapp.com")))
                {
                    url = "https://" + url.Substring("http://".Length);
                    if (ServerIPEntry != null)
                        ServerIPEntry.Text = url;
                    System.Diagnostics.Debug.WriteLine($"[Settings] Auto-upgraded to HTTPS for production domain");
                }

                Preferences.Set(PREF_SERVER_URL, url);
                UpdateConfigurationDisplay(url);
                System.Diagnostics.Debug.WriteLine($"[Settings] Configuration saved: {url}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Unexpected error: {ex.Message}");
                await DisplayAlert(T("Common_Error"), string.Format(T("Settings_Test_Error_Generic"), ex.Message), T("Common_OK"));
            }
        }

        #endregion

        #region Validation

        private bool IsValidIP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;

            // Accept plain IPv4: 192.168.1.1
            var parts = ip.Split('.');
            if (parts.Length == 4 && parts.All(p => int.TryParse(p, out int n) && n >= 0 && n <= 255))
                return true;

            // Accept hostname / mDNS: my-server.local, raspberrypi, etc.
            // Allow letters, digits, hyphens, dots (no leading/trailing hyphen per segment)
            if (System.Text.RegularExpressions.Regex.IsMatch(ip,
                @"^(?!-)[A-Za-z0-9\-]{1,63}(?<!-)(\.[A-Za-z0-9\-]{1,63}(?<!-))*$"))
                return true;

            // Accept full URLs starting with http:// or https://
            if (Uri.TryCreate(ip, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
                return true;

            return false;
        }

        #endregion

        #region USB Mode Handling

        private void OnUsbModeToggled(object sender, ToggledEventArgs e)
        {
            try
            {
                Preferences.Set(PREF_USB_MODE, e.Value);
                var displayURL = e.Value ? USB_URL : ServerIPEntry?.Text?.Trim() ?? DEFAULT_URL;
                UpdateConfigurationDisplay(displayURL);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region UI Updates

        private void UpdateConfigurationDisplay(string url)
        {
            try
            {
                var cleanUrl = url.Replace("http://", "").Replace("https://", "");
                var protocol = url.StartsWith("https://") ? "wss" : "ws";
                
                if (CurrentConfigLabel != null)
                {
                    CurrentConfigLabel.Text = cleanUrl.Contains(":")
                        ? $"{protocol}://{cleanUrl}/ws"
                        : $"{protocol}://{cleanUrl}:8080/ws";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Connection Testing

        private async Task TestConnectionAsync()
        {
            try
            {
                bool isUsbMode = Preferences.Get(PREF_USB_MODE, false);
                string? serverUrl = isUsbMode ? USB_URL : ServerIPEntry?.Text?.Trim();

                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    await DisplayAlert(T("Common_Error"), T("Settings_Test_Error_EmptyUrl"), T("Common_OK"));
                    return;
                }

                string testUrl = serverUrl;
                if (!testUrl.StartsWith("http://") && !testUrl.StartsWith("https://"))
                {
                    if (IsValidIP(testUrl))
                        testUrl = $"http://{testUrl}:8080";
                    else
                    {
                        await DisplayAlert(T("Common_Error"), T("Settings_Test_Error_InvalidUrl"), T("Common_OK"));
                        return;
                    }
                }

                if (TestButton != null)
                {
                    TestButton.IsEnabled = false;
                    TestButton.Text = T("Settings_Test_Testing");
                }

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var healthUrl = testUrl.TrimEnd('/') + "/health";
                var response = await httpClient.GetAsync(healthUrl);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert(
                        T("Settings_Test_Success_Title"),
                        string.Format(T("Settings_Test_Success_Detail"), testUrl),
                        T("Common_OK"));
                }
                else
                {
                    await DisplayAlert(
                        T("Common_Error"),
                        string.Format(T("Settings_Test_Error_Server"), response.StatusCode),
                        T("Common_OK"));
                }
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert(
                    T("Settings_Test_Error_Connection_Title"),
                    string.Format(T("Settings_Test_Error_Connection_Detail"), ex.Message),
                    T("Common_OK"));
            }
            catch (TaskCanceledException)
            {
                await DisplayAlert(
                    T("Settings_Test_Error_Timeout_Title"),
                    T("Settings_Test_Error_Timeout_Detail"),
                    T("Common_OK"));
            }
            catch (Exception ex)
            {
                await DisplayAlert(T("Common_Error"), string.Format(T("Settings_Test_Error_Generic"), ex.Message), T("Common_OK"));
            }
            finally
            {
                if (TestButton != null)
                {
                    TestButton.IsEnabled = true;
                    TestButton.Text = T("Settings_Test_Done");
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnSaveButtonClicked(object sender, EventArgs e) => SaveServerIP();

        private async void OnTestConnectionClicked(object sender, EventArgs e) => await TestConnectionAsync();

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            try
            {
                string newName = await DisplayPromptAsync(
                    T("Settings_EditProfile_Title"),
                    T("Settings_EditProfile_Prompt"),
                    T("Settings_EditProfile_Save"),
                    T("Settings_EditProfile_Cancel"),
                    placeholder: T("Settings_EditProfile_Placeholder")
                );

                if (!string.IsNullOrEmpty(newName))
                {
                    UserNameLabel.Text = newName;
                    AvatarInitials.Text = GetInitials(newName);
                    Preferences.Set("DisplayName", newName);
                    await DisplayAlert(
                        T("Settings_EditProfile_SuccessTitle"),
                        T("Settings_EditProfile_SuccessMessage"),
                        T("Common_OK")
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Edit profile error: {ex.Message}");
            }
        }

        private async void OnChangeAvatarClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result == null)
                    return;

                string extension = Path.GetExtension(result.FileName);
                string fileName = $"avatar_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                string destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                using (Stream sourceStream = await result.OpenReadAsync())
                using (FileStream destStream = File.OpenWrite(destPath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                string oldPath = Preferences.Get(PREF_AVATAR_PATH, string.Empty);
                Preferences.Set(PREF_AVATAR_PATH, destPath);

                if (!string.IsNullOrWhiteSpace(oldPath) && File.Exists(oldPath) && !string.Equals(oldPath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(oldPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors for old avatar
                    }
                }

                SetAvatarImage(destPath);
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert(T("Settings_Avatar_ErrorTitle"), T("Settings_Avatar_NotSupported"), T("Common_OK"));
            }
            catch (PermissionException)
            {
                await DisplayAlert(T("Settings_Avatar_ErrorTitle"), T("Settings_Avatar_Permission"), T("Common_OK"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Avatar pick error: {ex.Message}");
                await DisplayAlert(T("Settings_Avatar_ErrorTitle"), T("Settings_Avatar_ErrorMessage"), T("Common_OK"));
            }
        }

        private void OnDarkModeToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set(PREF_LIGHT_THEME_USER_SET, true);
            Preferences.Set(PREF_LIGHT_THEME, e.Value);
            ApplyTheme(e.Value);
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Light theme: {e.Value}");
        }

        private void ApplyTheme(bool useLightTheme)
        {
            if (Application.Current == null)
                return;

            Application.Current.UserAppTheme = useLightTheme ? AppTheme.Light : AppTheme.Dark;
            App.ApplyThemeResources(useLightTheme);

            // Update Shell TabBar to match theme
            if (Shell.Current != null)
            {
                var tabBg    = useLightTheme ? Color.FromArgb("#F5F7FB") : Color.FromArgb("#0A0A0A");
                var tabUnsel = useLightTheme ? Color.FromArgb("#8B9CAF") : Color.FromArgb("#606060");
                Shell.SetTabBarBackgroundColor(Shell.Current, tabBg);
                Shell.SetTabBarUnselectedColor(Shell.Current, tabUnsel);
            }
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            string title = LocalizationResourceManager.Instance["Language_SelectTitle"];
            string cancel = LocalizationResourceManager.Instance["Common_Cancel"];
            string optionVi = LocalizationResourceManager.Instance["Language_Vietnamese"];
            string optionEn = LocalizationResourceManager.Instance["Language_English"];

            string action = await DisplayActionSheet(title, cancel, null, optionVi, optionEn);

            if (action == optionVi)
            {
                SetLanguage("vi");
            }
            else if (action == optionEn)
            {
                SetLanguage("en");
            }
        }

        private void SetLanguage(string languageCode)
        {
            Preferences.Set(PREF_APP_LANGUAGE, languageCode);
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(languageCode));
        }

        private async void OnSecurityClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                T("Settings_Security_Title"),
                T("Settings_Security_Message"),
                T("Common_OK")
            );
        }

        private async void OnWhitelistClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("WhitelistPage");
        }

        private async void OnAutoRejectToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("AutoRejectBlacklisted", e.Value);
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Auto-reject blacklisted: {e.Value}");

            if (e.Value)
            {
                // Sync blacklist from server when enabling
                try
                {
                    await BlacklistCacheService.Instance.SyncFromServerAsync();
                    await DisplayAlert(
                        T("Settings_AutoReject"),
                        T("Settings_AutoRejectEnabled"),
                        T("Common_OK"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsPage] Blacklist sync error: {ex.Message}");
                }
            }
        }

        private void OnSmsDetectionToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("SmsDetectionEnabled", e.Value);
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] SMS detection: {e.Value}");
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            // Hiển thị crash log nếu có
            try
            {
                var crashLogPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
                string crashInfo = "";
                
                if (System.IO.File.Exists(crashLogPath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(crashLogPath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        string snippet = content.Substring(0, Math.Min(500, content.Length));
                        crashInfo = string.Format(T("Settings_Help_CrashLogFormat"), snippet);
                    }
                }
                
                bool clearLog = await DisplayAlert(
                    T("Settings_Help_Title"),
                    string.Format(T("Settings_Help_MessageFormat"), crashInfo),
                    T("Settings_Help_ClearLog"),
                    T("Common_Close")
                );
                
                if (clearLog && System.IO.File.Exists(crashLogPath))
                {
                    System.IO.File.Delete(crashLogPath);
                    await DisplayAlert(
                        T("Settings_Help_LogClearedTitle"),
                        T("Settings_Help_LogClearedMessage"),
                        T("Common_OK")
                    );
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    T("Settings_Help_Title"),
                    string.Format(T("Settings_Help_LogErrorFormat"), ex.Message),
                    T("Common_OK")
                );
            }
        }
        
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                // Confirm logout
                bool confirm = await DisplayAlert(
                    T("Settings_Logout_Title"),
                    T("Settings_Logout_ConfirmMessage"),
                    T("Settings_Logout_ConfirmButton"),
                    T("Settings_Logout_CancelButton")
                );

                if (!confirm)
                    return;

                System.Diagnostics.Debug.WriteLine("[SettingsPage] Logging out user");

                // Check if auth service is available
                if (_authService == null)
                {
                    await DisplayAlert(
                        T("Settings_Logout_ErrorTitle"),
                        T("Settings_Logout_ServiceUnavailable"),
                        T("Common_OK")
                    );
                    return;
                }

                // Logout
                await _authService.LogoutAsync();

                // Navigate to login page
                Application.Current!.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                    BarTextColor = Color.FromArgb("#E0E6ED")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Logout error: {ex.Message}");
                await DisplayAlert(
                    T("Settings_Logout_ErrorTitle"),
                    string.Format(T("Settings_Logout_ErrorMessage"), ex.Message),
                    T("Common_OK")
                );
            }
        }

        // ── Connection section collapse/expand ──────────────────────────
        private async void OnConnectionSectionToggled(object sender, EventArgs e)
        {
            try
            {
                bool isCurrentlyExpanded = ConnectionDetailsPanel.IsVisible;
                ConnectionDetailsPanel.IsVisible = !isCurrentlyExpanded;

                // Rotate chevron: 0° = collapsed (›), 90° = expanded (pointing down)
                if (ConnectionChevron != null)
                    await ConnectionChevron.RotateTo(isCurrentlyExpanded ? 0 : 90, 200, Easing.CubicInOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Connection toggle error: {ex.Message}");
            }
        }

        // ── Update phone number ──────────────────────────────────────────
        private async void OnUpdatePhoneClicked(object sender, EventArgs e)
        {
            try
            {
                // Pre-fill with existing value if available
                string current = PhoneNumberLabel?.Text ?? "";
                string notUpdated = LocalizationResourceManager.Instance["Settings_NotUpdated"];
                if (current == notUpdated) current = "";

                string newPhone = await DisplayPromptAsync(
                    "Cập nhật số điện thoại",
                    "Nhập số điện thoại của bạn:",
                    "Lưu",
                    "Hủy",
                    placeholder: "0912345678",
                    initialValue: current,
                    keyboard: Keyboard.Telephone
                );

                if (string.IsNullOrWhiteSpace(newPhone)) return;
                newPhone = newPhone.Trim();

                // Persist to SecureStorage (key matches SecureStorageService.KEY_PHONE_NUMBER)
                await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync("phone_number", newPhone);

                // Update UI label immediately
                if (PhoneNumberLabel != null)
                    PhoneNumberLabel.Text = newPhone;

                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Phone number updated: {newPhone}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Update phone error: {ex.Message}");
            }
        }

        private static string T(string key)
            => LocalizationResourceManager.Instance[key];

        #endregion

    }
}
