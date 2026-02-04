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

        // Register Firebase Authentication Service
        builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthService>();
        builder.Services.AddSingleton<SecureStorageService>();

        return builder.Build();
    }
}
