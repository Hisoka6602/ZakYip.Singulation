using Foundation;

namespace ZakYip.Singulation.MauiApp {
    /// <summary>
    /// MacCatalyst 平台的应用程序委托。
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate {
        /// <inheritdoc />
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
