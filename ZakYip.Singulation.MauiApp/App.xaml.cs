using Microsoft.Maui.Controls;
using ZakYip.Singulation.MauiApp.Views;

namespace ZakYip.Singulation.MauiApp;

public partial class App : Application {
    public App(MainPage page) {
        InitializeComponent();
        MainPage = new NavigationPage(page);
    }
}
