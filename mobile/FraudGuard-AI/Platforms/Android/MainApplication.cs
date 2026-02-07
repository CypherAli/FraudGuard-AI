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
        
        // Firebase Auth initialization happens in MainActivity
        // No need to initialize here as Application context is different from Activity
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
