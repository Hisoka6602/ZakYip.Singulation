using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

/// <summary>
/// 设置页面，允许用户配置应用程序设置。
/// </summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>
    /// 初始化 <see cref="SettingsPage"/> 类的新实例。
    /// </summary>
    /// <param name="viewModel">设置视图模型。</param>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
