using Microsoft.Extensions.Logging;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp {
    public static class MauiProgram {
        public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp() {
            var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // 注册 HttpClient 和 ApiClient
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                // 从本地存储读取API地址
                var apiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5000");
                var timeoutSeconds = Preferences.Get("TimeoutSeconds", "30");
                
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(int.Parse(timeoutSeconds));
            });

            // 注册 SignalR 客户端工厂
            builder.Services.AddSingleton(sp => 
            {
                var apiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5000");
                return new SignalRClientFactory(apiBaseUrl);
            });

            // 注册 ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // 注册 Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Views.SettingsPage>();

            return builder.Build();
        }
    }
}
