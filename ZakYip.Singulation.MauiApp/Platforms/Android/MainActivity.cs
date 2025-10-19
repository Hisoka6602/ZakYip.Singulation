using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ZakYip.Singulation.MauiApp {
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity {
        protected override void OnStop()
        {
            base.OnStop();
            // 当应用进入后台时，结束应用以防止后台运行
            FinishAndRemoveTask();
        }
    }
}
