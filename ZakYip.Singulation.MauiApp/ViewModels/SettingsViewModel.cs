using Prism.Commands;
using Prism.Mvvm;
using ZakYip.Singulation.MauiApp.Services;
using System.Collections.ObjectModel;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 设置页面视图模型
/// </summary>
public class SettingsViewModel : BindableBase, IDisposable
{
    private readonly UdpDiscoveryClient _discoveryClient;

    private string _apiBaseUrl = "http://localhost:5005";
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

    private bool _isDiscovering = false;
    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    private DiscoveredService? _selectedService;
    public DiscoveredService? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value) && value != null)
            {
                // 自动填充地址
                ApiBaseUrl = value.HttpBaseUrl;
                StatusMessage = $"已选择服务: {value.ServiceName}";
            }
        }
    }

    public ObservableCollection<DiscoveredService> DiscoveredServices => _discoveryClient.DiscoveredServices;

    public DelegateCommand SaveSettingsCommand { get; }
    public DelegateCommand ToggleDiscoveryCommand { get; }
    public DelegateCommand<DiscoveredService> ConnectToServiceCommand { get; }

    public SettingsViewModel(UdpDiscoveryClient discoveryClient)
    {
        _discoveryClient = discoveryClient;

        // 从本地存储加载设置
        LoadSettings();

        SaveSettingsCommand = new DelegateCommand(async () => await SaveSettingsAsync());
        ToggleDiscoveryCommand = new DelegateCommand(async () => await ToggleDiscoveryAsync());
        ConnectToServiceCommand = new DelegateCommand<DiscoveredService>(async (service) => await ConnectToServiceAsync(service));

        // 订阅服务发现事件
        _discoveryClient.ServiceDiscovered += OnServiceDiscovered;
        _discoveryClient.ServiceLost += OnServiceLost;
    }

    /// <summary>
    /// 切换服务发现
    /// </summary>
    private async Task ToggleDiscoveryAsync()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (IsDiscovering)
            {
                _discoveryClient.StopListening();
                IsDiscovering = false;
                StatusMessage = "服务发现已停止";
            }
            else
            {
                await _discoveryClient.StartListeningAsync();
                IsDiscovering = true;
                StatusMessage = "正在搜索服务...";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 服务发现失败: {ex.Message}";
            IsDiscovering = false;
        }
    }

    /// <summary>
    /// 连接到发现的服务
    /// </summary>
    private async Task ConnectToServiceAsync(DiscoveredService? service)
    {
        if (service == null) return;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            ApiBaseUrl = service.HttpBaseUrl;
            await SaveSettingsAsync();
            
            StatusMessage = $"✅ 已连接到 {service.ServiceName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 连接失败: {ex.Message}";
        }
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
        ApiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5005");
        TimeoutSeconds = Preferences.Get("TimeoutSeconds", "30");
    }

    private void OnServiceDiscovered(object? sender, DiscoveredService service)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"🔍 发现服务: {service.ServiceName}";
        });
    }

    private void OnServiceLost(object? sender, DiscoveredService service)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"❌ 服务失联: {service.ServiceName}";
        });
    }

    public void Dispose()
    {
        _discoveryClient.ServiceDiscovered -= OnServiceDiscovered;
        _discoveryClient.ServiceLost -= OnServiceLost;
        _discoveryClient.Dispose();
    }
}
