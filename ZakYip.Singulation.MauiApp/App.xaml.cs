namespace ZakYip.Singulation.MauiApp {
    public partial class App : Application {
        public App() {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            
            // 订阅窗口生命周期事件
            window.Deactivated += OnWindowDeactivated;
            
            return window;
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            // 当应用失去焦点时（进入后台），可以在这里清理资源
            System.Diagnostics.Debug.WriteLine("应用进入后台");
        }
    }
}
