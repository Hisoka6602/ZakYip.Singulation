using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp {
    public partial class MainPage : ContentPage {
        public MainPage(MainViewModel viewModel) {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
