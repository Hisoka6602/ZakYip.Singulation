namespace ZakYip.Singulation.MauiApp {
    public partial class AppShell : Shell {
        public AppShell() {
            InitializeComponent();

            // 配置页面过渡动画以获得极致性能和流畅体验
            Navigating += OnNavigating;
        }

        private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            // 添加平滑的页面切换过渡动画
            if (e.Current?.Location?.OriginalString != e.Target?.Location?.OriginalString)
            {
                // 使用淡入淡出动画实现流畅过渡
                var currentPage = Current?.CurrentItem?.CurrentItem as ShellContent;
                if (currentPage != null)
                {
                    await Task.WhenAll(
                        currentPage.FadeTo(0, 100, Easing.CubicOut)
                    );
                }
            }
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            // 淡入新页面
            var currentPage = Current?.CurrentItem?.CurrentItem as ShellContent;
            if (currentPage != null)
            {
                currentPage.Opacity = 0;
                currentPage.FadeTo(1, 150, Easing.CubicIn);
            }
        }
    }
}
