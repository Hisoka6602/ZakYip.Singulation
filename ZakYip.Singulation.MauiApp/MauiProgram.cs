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
                // 默认API地址，可通过配置文件或环境变量修改
                client.BaseAddress = new Uri("http://localhost:5000");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // 注册 SignalR 客户端工厂
            builder.Services.AddSingleton(sp => 
                new SignalRClientFactory("http://localhost:5000"));

            // 注册 ViewModels
            builder.Services.AddTransient<MainViewModel>();

            // 注册 Pages
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
