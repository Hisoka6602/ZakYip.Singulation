using Android.App;
using Android.Runtime;

namespace ZakYip.Singulation.MauiApp {
    /// <summary>
    /// Android 平台的主应用程序类。
    /// </summary>
    [Application]
    public class MainApplication : MauiApplication {
        /// <summary>
        /// 初始化 <see cref="MainApplication"/> 类的新实例。
        /// </summary>
        /// <param name="handle">JNI 句柄。</param>
        /// <param name="ownership">JNI 句柄所有权。</param>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership) {
        }

        /// <inheritdoc />
        protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
