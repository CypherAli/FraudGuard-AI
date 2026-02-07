using FraudGuardAI.Services;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Auth;
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace FraudGuardAI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // TEMP FIX: Comment out custom fonts to avoid crash - will use default fonts
            //.ConfigureFonts(fonts =>
            //{
            //    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            //    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            //})
            .RegisterFirebaseServices();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Firebase Authentication Service
        builder.Services.AddSingleton<SecureStorageService>();
        builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthService>();

        return builder.Build();
    }

    /// <summary>
    /// Register Firebase services
    /// </summary>
    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
#elif IOS
        builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
#endif
        return builder;
    }
}
