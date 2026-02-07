using Android.App;
using Android.Runtime;

namespace FraudGuardAI;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        // Bắt tất cả unhandled exceptions
        AndroidEnvironment.UnhandledExceptionRaiser += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnTaskException;
    }

    private void OnUnhandledException(object? sender, RaiseThrowableEventArgs e)
    {
        LogCrash("AndroidEnvironment", e.Exception);
        e.Handled = true; // Ngăn app crash
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        LogCrash("AppDomain", e.ExceptionObject as Exception);
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogCrash("TaskScheduler", e.Exception);
        e.SetObserved(); // Ngăn app crash
    }

    private void LogCrash(string source, Exception? ex)
    {
        try
        {
            var msg = $"[CRASH] {source}: {ex?.Message}\n{ex?.StackTrace}";
            System.Diagnostics.Debug.WriteLine(msg);
            
            // Lưu vào file để xem sau
            var path = System.IO.Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
            var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n\n";
            System.IO.File.AppendAllText(path, content);
        }
        catch { /* ignore logging errors */ }
    }

    public override void OnCreate()
    {
        base.OnCreate();
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
            LogCrash("CreateMauiApp", ex);
            throw;
        }
    }
}
