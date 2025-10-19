using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.ViewModels;
using ZakYip.Singulation.MauiApp.Views;

namespace ZakYip.Singulation.MauiApp;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<SignalRClientFactory>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
