using Prism.Commands;
using Prism.Mvvm;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 设置页面视图模型
/// </summary>
public class SettingsViewModel : BindableBase
{
    private string _apiBaseUrl = "http://localhost:5000";
    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    private string _timeoutSeconds = "30";
    public string TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => SetProperty(ref _timeoutSeconds, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public DelegateCommand SaveSettingsCommand { get; }

    public SettingsViewModel()
    {
        // 从本地存储加载设置
        LoadSettings();

        SaveSettingsCommand = new DelegateCommand(async () => await SaveSettingsAsync());
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            // 验证 URL 格式
            if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri))
            {
                StatusMessage = "❌ 无效的 URL 格式";
                return;
            }

            // 验证超时值
            if (!int.TryParse(TimeoutSeconds, out var timeout) || timeout <= 0)
            {
                StatusMessage = "❌ 无效的超时时间";
                return;
            }

            // 保存到本地存储
            Preferences.Set("ApiBaseUrl", ApiBaseUrl);
            Preferences.Set("TimeoutSeconds", TimeoutSeconds);

            StatusMessage = "✅ 设置已保存";
            
            // 提示用户需要重启应用
            await Task.Delay(1500);
            StatusMessage = "ℹ️ 请重启应用以应用新设置";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        ApiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5000");
        TimeoutSeconds = Preferences.Get("TimeoutSeconds", "30");
    }
}
