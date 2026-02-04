using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Auth;

namespace FraudGuardAI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Initialize Firebase
            Plugin.Firebase.CrossFirebase.Initialize(this, savedInstanceState, CreateCrossFirebaseSettings());
        }

        private static CrossFirebaseSettings CreateCrossFirebaseSettings()
        {
            return new CrossFirebaseSettings(
                isAuthEnabled: true,
                isCloudMessagingEnabled: false,
                isDynamicLinksEnabled: false,
                isFirestoreEnabled: false,
                isFunctionsEnabled: false,
                isRemoteConfigEnabled: false,
                isStorageEnabled: false,
                facebookId: string.Empty,
                facebookAppName: string.Empty,
                googleRequestIdToken: string.Empty
            );
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            // Handle Firebase Auth activity results
            CrossFirebaseAuth.Current.HandleActivityResultAsync(requestCode, resultCode, data);
        }
    }
}
