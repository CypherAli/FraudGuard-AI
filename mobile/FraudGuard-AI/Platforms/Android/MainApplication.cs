using Android.App;
using Android.Runtime;

namespace FraudGuardAI;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        
        // Firebase will be initialized in MainActivity
        // Application context is not suitable for Firebase.Initialize()
        System.Diagnostics.Debug.WriteLine("[MainApplication] Application created");
    }

    protected override MauiApp CreateMauiApp()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MainApplication] Creating MauiApp...");
            var app = MauiProgram.CreateMauiApp();
            System.Diagnostics.Debug.WriteLine("[MainApplication] MauiApp created successfully");
            return app;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainApplication] CreateMauiApp error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainApplication] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
