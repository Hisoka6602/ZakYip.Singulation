using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

/// <summary>
/// 控制器详情页面，显示控制器的详细信息和状态。
/// </summary>
public partial class ControllerDetailsPage : ContentPage
{
    /// <summary>
    /// 初始化 <see cref="ControllerDetailsPage"/> 类的新实例。
    /// </summary>
    /// <param name="viewModel">控制器详情视图模型。</param>
    public ControllerDetailsPage(ControllerDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
