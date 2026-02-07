using FraudGuardAI.Services;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Auth;

namespace FraudGuardAI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MauiProgram] Starting CreateMauiApp...");
            
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

            // Register core services
            System.Diagnostics.Debug.WriteLine("[MauiProgram] Registering services...");
            builder.Services.AddSingleton<SecureStorageService>();
            builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthService>();

            System.Diagnostics.Debug.WriteLine("[MauiProgram] Building app...");
            var app = builder.Build();
            System.Diagnostics.Debug.WriteLine("[MauiProgram] App built successfully");
            
            return app;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] CRITICAL ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Register Firebase services with error handling
    /// </summary>
    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MauiProgram] Registering Firebase services...");
#if ANDROID
            // Register Firebase Auth as lazy singleton to avoid early initialization
            builder.Services.AddSingleton<IFirebaseAuth>(_ => 
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Lazy initializing Firebase Auth...");
                    // CrossFirebaseAuth.Current will be initialized after MainActivity.OnCreate()
                    var auth = CrossFirebaseAuth.Current;
                    if (auth == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[MauiProgram] WARNING: CrossFirebaseAuth.Current is null");
                    }
                    return auth;
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Firebase Auth registration error: {ex.Message}");
                    // Return null to allow app to continue without Firebase
                    return null!;
                }
            });
#elif IOS
            builder.Services.AddSingleton<IFirebaseAuth>(_ => CrossFirebaseAuth.Current);
#endif
            System.Diagnostics.Debug.WriteLine("[MauiProgram] Firebase services registered");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] RegisterFirebaseServices error: {ex.Message}");
            // Let it continue - Firebase might not be critical for basic app functionality
        }
        return builder;
    }
}
