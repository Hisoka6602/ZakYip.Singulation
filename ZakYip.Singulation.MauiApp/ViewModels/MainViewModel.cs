using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 主页面视图模型，实现MVVM架构
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly SignalRClientFactory _signalRFactory;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private ObservableCollection<ControllerInfo> _controllers = new();

    [ObservableProperty]
    private string _safetyCommandType = "Start";

    [ObservableProperty]
    private string _safetyReason = string.Empty;

    [ObservableProperty]
    private ControllerInfo? _selectedController;

    [ObservableProperty]
    private double _targetSpeed = 100.0;

    public MainViewModel(ApiClient apiClient, SignalRClientFactory signalRFactory)
    {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;
    }

    /// <summary>
    /// 刷新控制器列表
    /// </summary>
    [RelayCommand]
    private async Task RefreshControllersAsync()
    {
        try
        {
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
    [RelayCommand]
    private async Task SendSafetyCommandAsync()
    {
        try
        {
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
    [RelayCommand]
    private async Task ConnectSignalRAsync()
    {
        try
        {
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
    [RelayCommand]
    private async Task EnableAllAxesAsync()
    {
        try
        {
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
    [RelayCommand]
    private async Task DisableAllAxesAsync()
    {
        try
        {
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
    [RelayCommand]
    private async Task SetAllAxesSpeedAsync()
    {
        try
        {
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
