using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace CalorieTracker.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                var color = Android.Graphics.Color.ParseColor("#0F0F13"); 
                #pragma warning disable CA1422
                Window?.SetStatusBarColor(color);
                Window?.SetNavigationBarColor(color);
            }
        }
    }
}