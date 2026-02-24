using FraudGuardAI.Pages;
using FraudGuardAI.Pages.Auth;
using FraudGuardAI.Services;
using Microsoft.Extensions.Logging;

namespace FraudGuardAI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ── Infrastructure ───────────────────────────────────────────────────
        builder.Services.AddSingleton<IAppSettings, AppSettings>();

        // ── Auth Services ────────────────────────────────────────────────────
        // NOTE: BrevoEmailService is no longer registered — OTP is sent via backend.
        // Brevo API key must be stored in backend env vars only, never in the APK.
        builder.Services.AddSingleton<SecureStorageService>();
        builder.Services.AddSingleton<IAuthenticationService, EmailOtpAuthService>();

        // ── Domain Services ──────────────────────────────────────────────────
        builder.Services.AddSingleton<IHistoryService, HistoryService>();

        // ── Pages ────────────────────────────────────────────────────────────
        // Auth flow pages (created via DI so constructor injection works)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        // Note: OtpVerificationPage takes (verificationId, email) so use 'new' at call site

        // Main app pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HistoryDetailPage>();
        builder.Services.AddTransient<WhitelistPage>();

        return builder.Build();
    }
}
