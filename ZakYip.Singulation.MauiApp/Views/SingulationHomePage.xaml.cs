using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

public partial class SingulationHomePage : ContentPage
{
    public SingulationHomePage(SingulationHomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
