using FraudGuardAI.Services;
using FraudGuardAI.Pages;
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
        builder.Services.AddSingleton<SecureStorageService>();
        builder.Services.AddSingleton<BrevoEmailService>();
        builder.Services.AddSingleton<IAuthenticationService, EmailOtpAuthService>();

        // ── Domain Services ──────────────────────────────────────────────────
        builder.Services.AddSingleton<IHistoryService, HistoryService>();

        // ── Pages (registered so Shell DataTemplate + GoToAsync use DI) ──────
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HistoryDetailPage>();

        return builder.Build();
    }
}
