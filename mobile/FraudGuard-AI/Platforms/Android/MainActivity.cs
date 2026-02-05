using Android.App;
using Android.Content.PM;
using Android.OS;
using Firebase;

namespace FraudGuardAI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Initialize Firebase
            try
            {
                FirebaseApp.InitializeApp(this);
                System.Diagnostics.Debug.WriteLine("[MainActivity] Firebase initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Firebase initialization error: {ex.Message}");
            }
        }
    }
}
