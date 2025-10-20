using Microsoft.Extensions.Logging;
using Prism.Ioc;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.ViewModels;
using ZakYip.Singulation.MauiApp.Views;

namespace ZakYip.Singulation.MauiApp {
    public static class MauiProgram {
        public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp() {
            var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UsePrism(prism => prism
                    .RegisterTypes(RegisterTypes)
                    .CreateWindow((container, nav) => nav.NavigateAsync("NavigationPage/MainPage")))
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册 HttpClient 工厂
            containerRegistry.Register<HttpClient>(provider =>
            {
                // 从本地存储读取API地址
                var apiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5005");
                var timeoutSeconds = Preferences.Get("TimeoutSeconds", "30");
                
                // 安全解析超时值，失败时使用默认值
                if (!int.TryParse(timeoutSeconds, out var timeout) || timeout <= 0)
                {
                    timeout = 30;
                }
                
                var client = new HttpClient
                {
                    BaseAddress = new Uri(apiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(timeout)
                };
                return client;
            });

            // 注册 UDP 服务发现客户端
            containerRegistry.RegisterSingleton<UdpDiscoveryClient>();

            // 注册所有 API 服务（按功能划分）
            containerRegistry.Register<ApiClient>();
            containerRegistry.Register<AxisApiService>();
            containerRegistry.Register<ControllerApiService>();
            containerRegistry.Register<DecoderApiService>();
            containerRegistry.Register<UpstreamApiService>();
            containerRegistry.Register<SystemApiService>();
            containerRegistry.Register<SafetyApiService>();

            // 注册 SignalR 客户端工厂
            containerRegistry.RegisterSingleton<SignalRClientFactory>(provider => 
            {
                var apiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5005");
                return new SignalRClientFactory(apiBaseUrl);
            });

            // 注册 Pages 和 ViewModels (Prism 会自动关联)
            containerRegistry.RegisterForNavigation<MainPage, MainViewModel>();
            containerRegistry.RegisterForNavigation<ModuleGridPage, ModuleGridViewModel>();
            containerRegistry.RegisterForNavigation<SettingsPage, SettingsViewModel>();
            containerRegistry.RegisterForNavigation<ControllerDetailsPage, ControllerDetailsViewModel>();
            containerRegistry.RegisterForNavigation<SingulationHomePage, SingulationHomeViewModel>();
        }
    }
}
