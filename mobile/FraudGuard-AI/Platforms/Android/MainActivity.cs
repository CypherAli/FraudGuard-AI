using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Auth;

namespace FraudGuardAI
{
    /// <summary>
    /// Main Activity for FraudGuard AI Android App
    /// Handles Firebase Phone Authentication callbacks
    /// </summary>
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | 
                               ConfigChanges.Orientation | 
                               ConfigChanges.UiMode | 
                               ConfigChanges.ScreenLayout | 
                               ConfigChanges.SmallestScreenSize | 
                               ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        /// <summary>
        /// Called when activity is created
        /// Firebase Phone Auth requires Activity reference for reCAPTCHA
        /// </summary>
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Plugin.Firebase will automatically handle:
            // 1. Play Integrity API verification
            // 2. reCAPTCHA fallback if Play Integrity fails
            // 3. SMS auto-retrieval on supported devices
            
            System.Diagnostics.Debug.WriteLine("[MainActivity] Activity created - Firebase Phone Auth ready");
        }

        /// <summary>
        /// Handle activity result for Phone Auth
        /// This is required for reCAPTCHA verification flow
        /// </summary>
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            
            // Plugin.Firebase handles this automatically
            // No manual intervention needed
        }
    }
}
