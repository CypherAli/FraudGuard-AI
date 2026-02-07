using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Core.Platforms.Android;

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
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] OnCreate started");
                base.OnCreate(savedInstanceState);
                System.Diagnostics.Debug.WriteLine("[MainActivity] base.OnCreate completed");
                
                // Initialize Firebase with Activity context (OPTIONAL)
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Initializing Firebase...");
                    CrossFirebase.Initialize(this);
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Firebase initialized successfully");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] Firebase init failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] Stack: {ex.StackTrace}");
                    // App will work without Firebase - just won't have phone auth
                }
                
                System.Diagnostics.Debug.WriteLine("[MainActivity] OnCreate completed");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] FATAL OnCreate error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Stack: {ex.StackTrace}");
                // Re-throw to show Android crash dialog with details
                throw;
            }
            
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
