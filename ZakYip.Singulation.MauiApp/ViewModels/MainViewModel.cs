using Prism.Commands;
using Prism.Mvvm;
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

    private ObservableCollection<ControllerInfo> _controllers = new();
    public ObservableCollection<ControllerInfo> Controllers
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

    private ControllerInfo? _selectedController;
    public ControllerInfo? SelectedController
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

    public MainViewModel(ApiClient apiClient, SignalRClientFactory signalRFactory)
    {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;

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
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
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
            }
            else
            {
                StatusMessage = $"Error: {response.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
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
            StatusMessage = _signalRFactory.IsConnected 
                ? "SignalR connected" 
                : "SignalR connection failed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SignalR error: {ex.Message}";
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
            StatusMessage = response.Success 
                ? "All axes enabled successfully" 
                : $"Error: {response.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
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
            StatusMessage = response.Success 
                ? "All axes disabled successfully" 
                : $"Error: {response.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
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
            StatusMessage = response.Success 
                ? $"Speed set to {TargetSpeed} mm/s successfully" 
                : $"Error: {response.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Exception: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
