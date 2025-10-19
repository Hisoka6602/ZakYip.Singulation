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
    private string _safetyCommandType = "Emergency";

    [ObservableProperty]
    private string _safetyReason = string.Empty;

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

            var request = new SafetyCommandRequest
            {
                CommandType = SafetyCommandType,
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
}
