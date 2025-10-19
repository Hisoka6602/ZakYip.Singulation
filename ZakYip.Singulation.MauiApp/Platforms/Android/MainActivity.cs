using Android.App;
using Android.Content.PM;

namespace ZakYip.Singulation.MauiApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    MainLauncher = true)]
public class MainActivity : MauiAppCompatActivity {
}
