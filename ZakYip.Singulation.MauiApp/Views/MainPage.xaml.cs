using Microsoft.Maui.Controls;
using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

public partial class MainPage : ContentPage {
    public MainPage(MainViewModel viewModel) {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
