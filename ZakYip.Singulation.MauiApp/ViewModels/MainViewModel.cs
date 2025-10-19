using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System.Collections.ObjectModel;
using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 主页面视图模型，实现MVVM架构
/// </summary>
public class MainViewModel : BindableBase
{
    private readonly ApiClient _apiClient;
    private readonly SignalRClientFactory _signalRFactory;
    private readonly NotificationService _notificationService;
    private readonly INavigationService _navigationService;

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _isLoading = false;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _signalRStatus = "Disconnected";
    public string SignalRStatus
    {
        get => _signalRStatus;
        set => SetProperty(ref _signalRStatus, value);
    }

    private ObservableCollection<string> _realtimeEvents = new();
    public ObservableCollection<string> RealtimeEvents
    {
        get => _realtimeEvents;
        set => SetProperty(ref _realtimeEvents, value);
    }

    private ObservableCollection<AxisInfo> _controllers = new();
    public ObservableCollection<AxisInfo> Controllers
    {
        get => _controllers;
        set => SetProperty(ref _controllers, value);
    }

    private string _safetyCommandType = "Start";
    public string SafetyCommandType
    {
        get => _safetyCommandType;
        set => SetProperty(ref _safetyCommandType, value);
    }

    private string _safetyReason = string.Empty;
    public string SafetyReason
    {
        get => _safetyReason;
        set => SetProperty(ref _safetyReason, value);
    }

    private AxisInfo? _selectedController;
    public AxisInfo? SelectedController
    {
        get => _selectedController;
        set => SetProperty(ref _selectedController, value);
    }

    private double _targetSpeed = 100.0;
    public double TargetSpeed
    {
        get => _targetSpeed;
        set => SetProperty(ref _targetSpeed, value);
    }

    public DelegateCommand RefreshControllersCommand { get; }
    public DelegateCommand SendSafetyCommandCommand { get; }
    public DelegateCommand ConnectSignalRCommand { get; }
    public DelegateCommand EnableAllAxesCommand { get; }
    public DelegateCommand DisableAllAxesCommand { get; }
    public DelegateCommand SetAllAxesSpeedCommand { get; }
    public DelegateCommand<AxisInfo> ViewDetailsCommand { get; }

    public MainViewModel(ApiClient apiClient, SignalRClientFactory signalRFactory, INavigationService navigationService)
    {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;
        _notificationService = NotificationService.Instance;
        _navigationService = navigationService;

        RefreshControllersCommand = new DelegateCommand(async () => await RefreshControllersAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        SendSafetyCommandCommand = new DelegateCommand(async () => await SendSafetyCommandAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        ConnectSignalRCommand = new DelegateCommand(async () => await ConnectSignalRAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        EnableAllAxesCommand = new DelegateCommand(async () => await EnableAllAxesAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        DisableAllAxesCommand = new DelegateCommand(async () => await DisableAllAxesAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        SetAllAxesSpeedCommand = new DelegateCommand(async () => await SetAllAxesSpeedAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        ViewDetailsCommand = new DelegateCommand<AxisInfo>(async (axis) => await ViewDetailsAsync(axis));

        // 订阅SignalR事件
        SubscribeToSignalREvents();
        
        // 自动连接SignalR
        _ = Task.Run(async () => await AutoConnectSignalRAsync());
    }

    /// <summary>
    /// 订阅SignalR事件
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        _signalRFactory.SpeedChanged += OnSpeedChanged;
        _signalRFactory.SafetyEventOccurred += OnSafetyEventOccurred;
        _signalRFactory.ConnectionStateChanged += OnConnectionStateChanged;
        _signalRFactory.MessageReceived += OnMessageReceived;
    }

    /// <summary>
    /// 自动连接SignalR
    /// </summary>
    private async Task AutoConnectSignalRAsync()
    {
        try
        {
            await Task.Delay(1000); // 等待初始化完成
            await ConnectSignalRAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Auto-connect failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理速度变化事件
    /// </summary>
    private void OnSpeedChanged(object? sender, SpeedChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var message = $"⚡ Axis {e.AxisId} speed: {e.Speed:F2} mm/s";
            AddRealtimeEvent(message);
            
            // 更新对应轴的速度显示
            var axis = Controllers.FirstOrDefault(a => a.Id == e.AxisId);
            if (axis != null)
            {
                axis.CurrentSpeed = e.Speed;
            }
        });
    }

    /// <summary>
    /// 处理安全事件
    /// </summary>
    private void OnSafetyEventOccurred(object? sender, SafetyEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var message = $"🛡️ {e.EventType}: {e.Message}";
            AddRealtimeEvent(message);
            _notificationService.ShowWarning(message);
        });
    }

    /// <summary>
    /// 处理连接状态变化
    /// </summary>
    private void OnConnectionStateChanged(object? sender, Microsoft.AspNetCore.SignalR.Client.HubConnectionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SignalRStatus = state switch
            {
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected => "Connected",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connecting => "Connecting...",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Reconnecting => "Reconnecting...",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected => "Disconnected",
                _ => "Unknown"
            };
        });
    }

    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private void OnMessageReceived(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddRealtimeEvent($"📨 {message}");
        });
    }

    /// <summary>
    /// 添加实时事件到列表（保持最近50条）
    /// </summary>
    private void AddRealtimeEvent(string message)
    {
        var timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
        RealtimeEvents.Insert(0, timestamped);
        
        // 保持最近50条记录
        while (RealtimeEvents.Count > 50)
        {
            RealtimeEvents.RemoveAt(RealtimeEvents.Count - 1);
        }
    }

    /// <summary>
    /// 查看轴详情
    /// </summary>
    private async Task ViewDetailsAsync(AxisInfo? axis)
    {
        if (axis == null) return;
        
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            var parameters = new NavigationParameters
            {
                { "axis", axis }
            };
            
            await _navigationService.NavigateAsync("ControllerDetailsPage", parameters);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新控制器列表
    /// </summary>
    private async Task RefreshControllersAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = "Refreshing controllers...";

            var response = await _apiClient.GetControllersAsync();
            if (response.Success && response.Data != null)
            {
                Controllers.Clear();
                foreach (var controller in response.Data)
                {
                    Controllers.Add(controller);
                }
                StatusMessage = $"Loaded {Controllers.Count} controllers";
                _notificationService.ShowSuccess($"已加载 {Controllers.Count} 个控制器");
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
                _notificationService.ShowError($"加载失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
            _notificationService.ShowError($"异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 发送安全命令
    /// </summary>
    private async Task SendSafetyCommandAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = "Sending safety command...";

            // 将字符串命令类型转换为枚举值
            int commandValue = SafetyCommandType switch
            {
                "Start" => 1,
                "Stop" => 2,
                "Reset" => 3,
                _ => 0
            };

            var request = new SafetyCommandRequest
            {
                Command = commandValue,
                Reason = SafetyReason
            };

            var response = await _apiClient.SendSafetyCommandAsync(request);
            if (response.Success)
            {
                StatusMessage = "Safety command sent successfully";
                SafetyReason = string.Empty;
                _notificationService.ShowSuccess($"安全命令 {SafetyCommandType} 发送成功");
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
                _notificationService.ShowError($"发送失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
            _notificationService.ShowError($"异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 连接到SignalR Hub
    /// </summary>
    private async Task ConnectSignalRAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = "Connecting to SignalR...";

            await _signalRFactory.GetOrCreateHubConnectionAsync();
            if (_signalRFactory.IsConnected)
            {
                StatusMessage = "SignalR connected";
                _notificationService.ShowSuccess("SignalR 连接成功");
            }
            else
            {
                StatusMessage = "SignalR connection failed";
                _notificationService.ShowError("SignalR 连接失败");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"SignalR error: {ex.Message}";
            _notificationService.ShowError($"SignalR 错误: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 使能所有轴
    /// </summary>
    private async Task EnableAllAxesAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = "Enabling all axes...";

            var response = await _apiClient.EnableAxesAsync();
            if (response.Success)
            {
                StatusMessage = "All axes enabled successfully";
                _notificationService.ShowSuccess("所有轴已成功使能");
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
                _notificationService.ShowError($"使能失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
            _notificationService.ShowError($"异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 禁用所有轴
    /// </summary>
    private async Task DisableAllAxesAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = "Disabling all axes...";

            var response = await _apiClient.DisableAxesAsync();
            if (response.Success)
            {
                StatusMessage = "All axes disabled successfully";
                _notificationService.ShowSuccess("所有轴已成功禁用");
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
                _notificationService.ShowError($"禁用失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
            _notificationService.ShowError($"异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 设置所有轴速度
    /// </summary>
    private async Task SetAllAxesSpeedAsync()
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            IsLoading = true;
            StatusMessage = $"Setting speed to {TargetSpeed} mm/s...";

            var response = await _apiClient.SetAxesSpeedAsync(TargetSpeed);
            if (response.Success)
            {
                StatusMessage = $"Speed set to {TargetSpeed} mm/s successfully";
                _notificationService.ShowSuccess($"速度已设置为 {TargetSpeed} mm/s");
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
                _notificationService.ShowError($"设置失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
            _notificationService.ShowError($"异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
