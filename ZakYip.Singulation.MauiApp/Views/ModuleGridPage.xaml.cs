using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

public partial class ModuleGridPage : ContentPage
{
    public ModuleGridPage(ModuleGridViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
