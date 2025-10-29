using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.Controls;

/// <summary>
/// 通知叠加层控件，用于显示应用程序通知。
/// </summary>
public partial class NotificationOverlay : ContentView
{
    /// <summary>
    /// 初始化 <see cref="NotificationOverlay"/> 类的新实例。
    /// </summary>
    public NotificationOverlay()
    {
        InitializeComponent();
        BindingContext = NotificationService.Instance;
    }
}
