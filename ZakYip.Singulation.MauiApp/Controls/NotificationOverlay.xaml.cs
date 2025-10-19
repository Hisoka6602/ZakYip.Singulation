using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.Controls;

public partial class NotificationOverlay : ContentView
{
    public NotificationOverlay()
    {
        InitializeComponent();
        BindingContext = NotificationService.Instance;
    }
}
