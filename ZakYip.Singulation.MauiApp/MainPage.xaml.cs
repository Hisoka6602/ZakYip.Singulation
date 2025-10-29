using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp {
    /// <summary>
    /// 应用程序的主页面。
    /// </summary>
    public partial class MainPage : ContentPage {
        /// <summary>
        /// 初始化 <see cref="MainPage"/> 类的新实例。
        /// </summary>
        /// <param name="viewModel">主视图模型。</param>
        public MainPage(MainViewModel viewModel) {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
