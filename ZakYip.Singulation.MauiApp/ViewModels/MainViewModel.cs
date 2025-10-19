using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject {
    private readonly ApiClient _apiClient;
    private readonly SignalRClientFactory _signalRFactory;

    public MainViewModel(ApiClient apiClient, SignalRClientFactory signalRFactory) {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;
        _baseAddress = _apiClient.BaseAddress;
        SafetyCommands = new ObservableCollection<SafetyCommand>((SafetyCommand[])Enum.GetValues(typeof(SafetyCommand)));
        _selectedSafetyCommand = SafetyCommand.Stop;
        RefreshControllerCommand = new AsyncRelayCommand(RefreshControllerAsync);
        SendSafetyCommand = new AsyncRelayCommand(SendSafetyCommandAsync);
    }

    [ObservableProperty]
    private string _baseAddress;

    [ObservableProperty]
    private string _controllerStatus = "尚未请求";

    [ObservableProperty]
    private string _lastOperation = string.Empty;

    [ObservableProperty]
    private SafetyCommand _selectedSafetyCommand;

    [ObservableProperty]
    private string? _safetyReason;

    [ObservableProperty]
    private string _signalRStatus = "未连接";

    public ObservableCollection<SafetyCommand> SafetyCommands { get; }

    public IAsyncRelayCommand RefreshControllerCommand { get; }

    public IAsyncRelayCommand SendSafetyCommand { get; }

    private async Task RefreshControllerAsync() {
        try {
            _apiClient.BaseAddress = BaseAddress;
            var response = await _apiClient.GetControllerAsync().ConfigureAwait(false);
            if (response is { Result: true, Data: not null }) {
                var data = response.Data;
                ControllerStatus = $"轴数: {data.AxisCount}, 错误码: {data.ErrorCode}, 初始化: {data.Initialized}";
                LastOperation = "控制器状态已刷新";
            }
            else {
                ControllerStatus = response?.Msg ?? "无法获取控制器状态";
                LastOperation = "控制器状态刷新失败";
            }

            await EnsureSignalRPreviewAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            ControllerStatus = $"请求失败: {ex.Message}";
        }
    }

    private async Task SendSafetyCommandAsync() {
        try {
            _apiClient.BaseAddress = BaseAddress;
            var envelope = await _apiClient.SendSafetyCommandAsync(SelectedSafetyCommand, SafetyReason).ConfigureAwait(false);
            LastOperation = envelope?.Msg ?? "安全命令已发送";
        }
        catch (Exception ex) {
            LastOperation = $"安全命令失败: {ex.Message}";
        }
    }

    private async Task EnsureSignalRPreviewAsync() {
        try {
            var connection = await _signalRFactory.EnsureConnectionAsync(BaseAddress).ConfigureAwait(false);
            SignalRStatus = $"准备连接: {connection.State}";
        }
        catch (Exception ex) {
            SignalRStatus = $"SignalR 准备失败: {ex.Message}";
        }
    }
}
