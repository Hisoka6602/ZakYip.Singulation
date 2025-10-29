using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

/// <summary>
/// 模块网格页面，以网格形式展示所有模块的状态。
/// </summary>
public partial class ModuleGridPage : ContentPage
{
    /// <summary>
    /// 初始化 <see cref="ModuleGridPage"/> 类的新实例。
    /// </summary>
    /// <param name="viewModel">模块网格视图模型。</param>
    public ModuleGridPage(ModuleGridViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
