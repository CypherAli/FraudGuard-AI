using Android.App;
using Android.Runtime;
using Plugin.Firebase.Core.Platforms.Android;

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
        
        // Initialize Firebase with google-services.json
        CrossFirebase.Initialize(this);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
