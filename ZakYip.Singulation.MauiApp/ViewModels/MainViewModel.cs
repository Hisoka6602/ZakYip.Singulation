using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.Helpers;
using ZakYip.Singulation.MauiApp.Icons;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 主页面视图模型，实现MVVM架构
/// </summary>
public class MainViewModel : BindableBase
{
    private static readonly int[] DefaultAxisRpms =
    {
        1000, 2000, 2000, 1600, 2000,
        2500, 3000, 2000, 2000, 3000,
        1600, 1800, 1000, 1800, 1000,
        1000, 1000, 1800, 1800, 1200
    };

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

    private string _signalRStatus = "未连接";
    public string SignalRStatus
    {
        get => _signalRStatus;
        set => SetProperty(ref _signalRStatus, value);
    }
    
    private int _signalRLatency = 0;
    public int SignalRLatency
    {
        get => _signalRLatency;
        set => SetProperty(ref _signalRLatency, value);
    }
    
    private string _signalRLatencyText = "";
    public string SignalRLatencyText
    {
        get => _signalRLatencyText;
        set => SetProperty(ref _signalRLatencyText, value);
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

    private bool _isAutoRefreshEnabled;
    public bool IsAutoRefreshEnabled
    {
        get => _isAutoRefreshEnabled;
        set
        {
            if (IsLoading)
            {
                return;
            }

            if (SetProperty(ref _isAutoRefreshEnabled, value))
            {
                RaisePropertyChanged(nameof(AutoRefreshStatusText));
                OnAutoRefreshToggled(value);
            }
        }
    }

    private bool _areAllAxesEnabled;
    public bool AreAllAxesEnabled
    {
        get => _areAllAxesEnabled;
        set
        {
            if (IsLoading)
            {
                return;
            }

            if (SetProperty(ref _areAllAxesEnabled, value))
            {
                RaisePropertyChanged(nameof(AllAxesStatusText));
                OnGlobalEnableToggled(value);
            }
        }
    }

    private string _machineSerial = "DJ1957AAKO025";
    public string MachineSerial
    {
        get => _machineSerial;
        set
        {
            if (SetProperty(ref _machineSerial, value))
            {
                RaisePropertyChanged(nameof(MachineSerialDisplay));
            }
        }
    }

    public string MachineSerialDisplay => $"自泵: {MachineSerial}";

    private bool _isSafetyPanelVisible;
    public bool IsSafetyPanelVisible
    {
        get => _isSafetyPanelVisible;
        set => SetProperty(ref _isSafetyPanelVisible, value);
    }

    private bool _isSpeedPanelVisible;
    public bool IsSpeedPanelVisible
    {
        get => _isSpeedPanelVisible;
        set => SetProperty(ref _isSpeedPanelVisible, value);
    }

    private bool _hasLiveAxisData;
    public bool HasLiveAxisData
    {
        get => _hasLiveAxisData;
        set => SetProperty(ref _hasLiveAxisData, value);
    }

    public string AutoRefreshStatusText => IsAutoRefreshEnabled ? "自动刷新开启" : "手动刷新";

    public string AllAxesStatusText => AreAllAxesEnabled ? "全部已使能" : "全部未使能";

    public DelegateCommand RefreshControllersCommand { get; }
    public DelegateCommand SendSafetyCommandCommand { get; }
    public DelegateCommand ConnectSignalRCommand { get; }
    public DelegateCommand EnableAllAxesCommand { get; }
    public DelegateCommand DisableAllAxesCommand { get; }
    public DelegateCommand SetAllAxesSpeedCommand { get; }
    public DelegateCommand<AxisInfo> ViewDetailsCommand { get; }
    public DelegateCommand ToggleSafetyPanelCommand { get; }
    public DelegateCommand ToggleSpeedPanelCommand { get; }
    public DelegateCommand ToggleAutoRefreshCommand { get; }
    public DelegateCommand ToggleAllAxesCommand { get; }

    // 图标 Glyphs（用于绑定）
    public string HomeGlyph => AppIcon.Home.ToGlyph();
    public string RefreshGlyph => AppIcon.Refresh.ToGlyph();
    public string SettingsGlyph => AppIcon.Settings.ToGlyph();
    public string PlayGlyph => AppIcon.Play.ToGlyph();
    public string StopGlyph => AppIcon.Stop.ToGlyph();
    public string SendGlyph => AppIcon.Send.ToGlyph();
    public string SpeedGlyph => AppIcon.Speed.ToGlyph();
    public string LinkGlyph => AppIcon.Link.ToGlyph();
    public string SafetyGlyph => AppIcon.Safety.ToGlyph();
    public string ControllerGlyph => AppIcon.Controller.ToGlyph();

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
        ToggleSafetyPanelCommand = new DelegateCommand(() => IsSafetyPanelVisible = !IsSafetyPanelVisible);
        ToggleSpeedPanelCommand = new DelegateCommand(() => IsSpeedPanelVisible = !IsSpeedPanelVisible);
        ToggleAutoRefreshCommand = new DelegateCommand(() => IsAutoRefreshEnabled = !IsAutoRefreshEnabled);
        ToggleAllAxesCommand = new DelegateCommand(() => AreAllAxesEnabled = !AreAllAxesEnabled);

        // 订阅SignalR事件
        SubscribeToSignalREvents();

        InitializeDefaultControllers();

        // 自动连接SignalR
        _ = Task.Run(async () => await AutoConnectSignalRAsync());
    }

    private void InitializeDefaultControllers(bool updateStatusMessage = true)
    {
        Controllers.Clear();

        for (var index = 0; index < DefaultAxisRpms.Length; index++)
        {
            var rpm = DefaultAxisRpms[index];
            var axisId = $"M{index + 1:D2}";

            Controllers.Add(new AxisInfo
            {
                AxisId = axisId,
                Status = 2,
                Enabled = true,
                TargetLinearMmps = rpm,
                FeedbackLinearMmps = rpm,
                CurrentSpeed = rpm,
                CurrentRpm = rpm,
                IsPlaceholder = true
            });
        }

        HasLiveAxisData = false;
        if (updateStatusMessage)
        {
            StatusMessage = "使用默认轴布局";
        }
    }

    private static double ResolveRpm(AxisInfo controller)
    {
        if (controller.CurrentRpm > 0)
        {
            return controller.CurrentRpm;
        }

        if (controller.FeedbackLinearMmps.HasValue && controller.FeedbackLinearMmps.Value > 0)
        {
            return controller.FeedbackLinearMmps.Value;
        }

        if (controller.TargetLinearMmps.HasValue && controller.TargetLinearMmps.Value > 0)
        {
            return controller.TargetLinearMmps.Value;
        }

        if (controller.CurrentSpeed > 0)
        {
            return controller.CurrentSpeed;
        }

        return 0;
    }

    private void ApplyControllers(IEnumerable<AxisInfo> controllers)
    {
        Controllers.Clear();

        foreach (var controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.CurrentRpm = ResolveRpm(controller);

            if (controller.CurrentSpeed <= 0 && controller.CurrentRpm > 0)
            {
                controller.CurrentSpeed = controller.CurrentRpm;
            }

            controller.IsPlaceholder = false;
            Controllers.Add(controller);
        }

        HasLiveAxisData = Controllers.Count > 0;

        if (!HasLiveAxisData)
        {
            InitializeDefaultControllers(false);
        }
    }

    private void OnAutoRefreshToggled(bool isEnabled)
    {
        if (!isEnabled)
        {
            return;
        }

        if (RefreshControllersCommand.CanExecute())
        {
            RefreshControllersCommand.Execute();
        }
    }

    private void OnGlobalEnableToggled(bool isEnabled)
    {
        if (IsLoading)
        {
            return;
        }

        if (isEnabled)
        {
            if (EnableAllAxesCommand.CanExecute())
            {
                EnableAllAxesCommand.Execute();
            }
        }
        else
        {
            if (DisableAllAxesCommand.CanExecute())
            {
                DisableAllAxesCommand.Execute();
            }
        }
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
        _signalRFactory.LatencyUpdated += OnLatencyUpdated;
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
            var axis = Controllers.FirstOrDefault(a => a.AxisId == e.AxisId.ToString() || a.AxisId == $"axis{e.AxisId}");
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
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected => "已连接",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connecting => "连接中...",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Reconnecting => "重新连接中...",
                Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected => "未连接",
                _ => "未知状态"
            };
            
            UpdateLatencyText();
        });
    }
    
    /// <summary>
    /// 处理延迟更新
    /// </summary>
    private void OnLatencyUpdated(object? sender, int latencyMs)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SignalRLatency = latencyMs;
            UpdateLatencyText();
        });
    }
    
    /// <summary>
    /// 更新延迟显示文本
    /// </summary>
    private void UpdateLatencyText()
    {
        if (SignalRStatus == "已连接" && SignalRLatency > 0)
        {
            SignalRLatencyText = $"延迟: {SignalRLatency}ms";
        }
        else
        {
            SignalRLatencyText = "";
        }
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
        await SafeExecutor.ExecuteAsync(
            async () =>
            {
                // Haptic feedback
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                IsLoading = true;
                StatusMessage = "Refreshing controllers...";

                var response = await _apiClient.GetControllersAsync();
                if (response.Success && response.Data != null)
                {
                    ApplyControllers(response.Data);
                    StatusMessage = HasLiveAxisData
                        ? $"已加载 {Controllers.Count} 个控制器"
                        : "未获取到实时轴数据，已加载默认布局";

                    if (HasLiveAxisData)
                    {
                        _notificationService.ShowSuccess($"已加载 {Controllers.Count} 个控制器");
                    }
                    else
                    {
                        _notificationService.ShowWarning("未获取到实时轴数据，展示默认布局");
                    }
                }
                else
                {
                    InitializeDefaultControllers(false);
                    StatusMessage = response.Success
                        ? "未返回实时轴数据，已展示默认布局"
                        : $"Error: {response.Message}";

                    if (!response.Success)
                    {
                        _notificationService.ShowError($"加载失败: {response.Message}");
                    }
                    else
                    {
                        _notificationService.ShowWarning("未返回轴数据，展示默认布局");
                    }
                }
            },
            ex =>
            {
                StatusMessage = $"Exception: {ex.Message}";
                _notificationService.ShowError($"异常: {ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message)}");
                InitializeDefaultControllers(false);
            },
            "RefreshControllers",
            timeout: 15000
        );

        IsLoading = false;
    }

    /// <summary>
    /// 发送安全命令
    /// </summary>
    private async Task SendSafetyCommandAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
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
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "SendSafetyCommand",
            timeout: 10000
        );

        IsLoading = false;
    }

    /// <summary>
    /// 连接到SignalR Hub
    /// </summary>
    private async Task ConnectSignalRAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
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
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "ConnectSignalR",
            timeout: 15000
        );

        IsLoading = false;
    }

    /// <summary>
    /// 使能所有轴
    /// </summary>
    private async Task EnableAllAxesAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
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
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "EnableAllAxes",
            timeout: 10000
        );

        IsLoading = false;
    }

    /// <summary>
    /// 禁用所有轴
    /// </summary>
    private async Task DisableAllAxesAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
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
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "DisableAllAxes",
            timeout: 10000
        );

        IsLoading = false;
    }

    /// <summary>
    /// 设置所有轴速度
    /// </summary>
    private async Task SetAllAxesSpeedAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
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
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "SetAllAxesSpeed",
            timeout: 10000
        );

        IsLoading = false;
    }
}
