namespace ZakYip.Singulation.MauiApp {
    public partial class AppShell : Shell {
        public AppShell() {
            InitializeComponent();

            // 配置页面过渡动画以获得极致性能和流畅体验
            Navigating += OnNavigating;
            Navigated += OnNavigated;
        }

        private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            // 添加平滑的页面切换过渡动画
            if (CurrentPage != null && e.Current?.Location?.OriginalString != e.Target?.Location?.OriginalString)
            {
                // 使用淡出动画实现流畅过渡
                await CurrentPage.FadeTo(0, 100, Easing.CubicOut);
            }
        }

        private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            // 淡入新页面
            if (CurrentPage != null)
            {
                CurrentPage.Opacity = 0;
                await CurrentPage.FadeTo(1, 150, Easing.CubicIn);
            }
        }
    }
}
