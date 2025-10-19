namespace ZakYip.Singulation.MauiApp {
    public partial class App : PrismApplication {
        public App() {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnInitialized()
        {
            // Prism initialization is handled in MauiProgram.cs
        }
    }
}
