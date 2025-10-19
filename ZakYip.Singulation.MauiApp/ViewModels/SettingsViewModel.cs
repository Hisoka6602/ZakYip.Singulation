using Prism.Commands;
using Prism.Mvvm;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.Helpers;
using System.Collections.ObjectModel;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 设置页面视图模型
/// </summary>
public class SettingsViewModel : BindableBase, IDisposable
{
    private readonly UdpDiscoveryClient _discoveryClient;
    private readonly NotificationService _notificationService;

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
    
    private string _networkStatusMessage = string.Empty;
    public string NetworkStatusMessage
    {
        get => _networkStatusMessage;
        set => SetProperty(ref _networkStatusMessage, value);
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
    
    private ObservableCollection<CachedServiceInfo> _cachedServices = new();
    public ObservableCollection<CachedServiceInfo> CachedServices
    {
        get => _cachedServices;
        set => SetProperty(ref _cachedServices, value);
    }

    public DelegateCommand SaveSettingsCommand { get; }
    public DelegateCommand ToggleDiscoveryCommand { get; }
    public DelegateCommand<DiscoveredService> ConnectToServiceCommand { get; }
    public DelegateCommand CheckNetworkCommand { get; }
    public DelegateCommand<CachedServiceInfo> UseCachedServiceCommand { get; }

    public SettingsViewModel(UdpDiscoveryClient discoveryClient)
    {
        _discoveryClient = discoveryClient;
        _notificationService = NotificationService.Instance;

        // 从本地存储加载设置
        LoadSettings();

        SaveSettingsCommand = new DelegateCommand(async () => await SaveSettingsAsync());
        ToggleDiscoveryCommand = new DelegateCommand(async () => await ToggleDiscoveryAsync());
        ConnectToServiceCommand = new DelegateCommand<DiscoveredService>(async (service) => await ConnectToServiceAsync(service));
        CheckNetworkCommand = new DelegateCommand(CheckNetwork);
        UseCachedServiceCommand = new DelegateCommand<CachedServiceInfo>(async (service) => await UseCachedServiceAsync(service));
        
        // 加载缓存的服务
        LoadCachedServices();
        
        // 检查网络状态
        CheckNetwork();

        // 订阅服务发现事件
        _discoveryClient.ServiceDiscovered += OnServiceDiscovered;
        _discoveryClient.ServiceLost += OnServiceLost;

        // 自动启动服务发现
        _ = AutoStartDiscoveryAsync();
    }

    /// <summary>
    /// 自动启动服务发现
    /// </summary>
    private async Task AutoStartDiscoveryAsync()
    {
        try
        {
            // 检查网络是否可用
            var availability = NetworkDiagnostics.CheckDiscoveryAvailability();
            if (!availability.IsAvailable)
            {
                StatusMessage = $"⚠️ {availability.Message}";
                NetworkStatusMessage = availability.Suggestion;
                return;
            }
            
            await Task.Delay(500); // 延迟启动，确保UI已加载
            await _discoveryClient.StartListeningAsync();
            IsDiscovering = true;
            const string message = "自动搜索服务中...";
            StatusMessage = message;
            _notificationService.ShowInfo(message);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = $"❌ {friendlyMessage}";
            IsDiscovering = false;
            _notificationService.ShowError(friendlyMessage);
        }
    }

    /// <summary>
    /// 检查网络状态
    /// </summary>
    private void CheckNetwork()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            var availability = NetworkDiagnostics.CheckDiscoveryAvailability();
            NetworkStatusMessage = availability.Message;
            
            if (!availability.IsAvailable && !string.IsNullOrEmpty(availability.Suggestion))
            {
                NetworkStatusMessage += $"\n\n{availability.Suggestion}";
            }
            
            StatusMessage = availability.IsAvailable ? "✅ 网络连接正常" : "⚠️ 网络连接异常";
        }
        catch (Exception ex)
        {
            NetworkStatusMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
        }
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
                const string message = "服务发现已停止";
                StatusMessage = message;
                _notificationService.ShowInfo(message);
            }
            else
            {
                // 检查网络状态
                var availability = NetworkDiagnostics.CheckDiscoveryAvailability();
                if (!availability.IsAvailable)
                {
                    StatusMessage = $"⚠️ {availability.Message}";
                    NetworkStatusMessage = availability.Suggestion;
                    _notificationService.ShowWarning(availability.Message);
                    return;
                }
                
                await _discoveryClient.StartListeningAsync();
                IsDiscovering = true;
                const string message = "正在搜索服务...";
                StatusMessage = message;
                _notificationService.ShowInfo(message);
            }
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = $"❌ {friendlyMessage}";
            IsDiscovering = false;
            _notificationService.ShowError(friendlyMessage);
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
            
            // 缓存服务信息
            ServiceCacheHelper.CacheService(service);
            LoadCachedServices(); // 刷新缓存列表
            
            await SaveSettingsAsync();
            
            var message = $"已连接到 {service.ServiceName}";
            StatusMessage = $"✅ {message}";
            _notificationService.ShowSuccess(message);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = $"❌ {friendlyMessage}";
            _notificationService.ShowError(friendlyMessage);
        }
    }
    
    /// <summary>
    /// 使用缓存的服务
    /// </summary>
    private async Task UseCachedServiceAsync(CachedServiceInfo? service)
    {
        if (service == null) return;
        
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            ApiBaseUrl = service.HttpBaseUrl;
            await SaveSettingsAsync();
            
            var message = $"已选择缓存服务: {service.ServiceName}";
            StatusMessage = $"✅ {message}";
            _notificationService.ShowSuccess(message);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = $"❌ {friendlyMessage}";
            _notificationService.ShowError(friendlyMessage);
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
                const string message = "无效的 URL 格式";
                StatusMessage = $"❌ {message}";
                _notificationService.ShowError(message);
                return;
            }

            // 验证超时值
            if (!int.TryParse(TimeoutSeconds, out var timeout) || timeout <= 0)
            {
                const string message = "无效的超时时间";
                StatusMessage = $"❌ {message}";
                _notificationService.ShowError(message);
                return;
            }

            // 保存到本地存储
            Preferences.Set("ApiBaseUrl", ApiBaseUrl);
            Preferences.Set("TimeoutSeconds", TimeoutSeconds);
            
            // 缓存当前API地址
            ServiceCacheHelper.CacheCurrentApiUrl(ApiBaseUrl);
            LoadCachedServices(); // 刷新缓存列表

            const string successMsg = "设置已保存";
            StatusMessage = $"✅ {successMsg}";
            _notificationService.ShowSuccess(successMsg);
            
            // 提示用户需要重启应用
            await Task.Delay(1500);
            const string infoMsg = "请重启应用以应用新设置";
            StatusMessage = $"ℹ️ {infoMsg}";
            _notificationService.ShowInfo(infoMsg);
        }
        catch (Exception ex)
        {
            var message = $"保存失败: {ex.Message}";
            StatusMessage = $"❌ {message}";
            _notificationService.ShowError(message);
        }
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        // 尝试从缓存加载最近使用的服务
        var recentService = ServiceCacheHelper.GetMostRecentService();
        if (recentService != null)
        {
            ApiBaseUrl = Preferences.Get("ApiBaseUrl", recentService.HttpBaseUrl);
        }
        else
        {
            ApiBaseUrl = Preferences.Get("ApiBaseUrl", "http://localhost:5005");
        }
        
        TimeoutSeconds = Preferences.Get("TimeoutSeconds", "30");
    }
    
    /// <summary>
    /// 加载缓存的服务列表
    /// </summary>
    private void LoadCachedServices()
    {
        var cached = ServiceCacheHelper.GetCachedServices();
        CachedServices.Clear();
        foreach (var service in cached)
        {
            CachedServices.Add(service);
        }
    }

    private void OnServiceDiscovered(object? sender, DiscoveredService service)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"🔍 发现服务: {service.ServiceName}";
            _notificationService.ShowInfo($"发现服务: {service.ServiceName}");
        });
    }

    private void OnServiceLost(object? sender, DiscoveredService service)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"❌ 服务失联: {service.ServiceName}";
            _notificationService.ShowWarning($"服务失联: {service.ServiceName}");
        });
    }

    public void Dispose()
    {
        _discoveryClient.ServiceDiscovered -= OnServiceDiscovered;
        _discoveryClient.ServiceLost -= OnServiceLost;
        _discoveryClient.Dispose();
    }
}
