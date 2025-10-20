using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.Helpers;
using ZakYip.Singulation.MauiApp.Icons;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 控制器详情页面视图模型
/// </summary>
public class ControllerDetailsViewModel : BindableBase, INavigationAware
{
    private readonly ApiClient _apiClient;
    private readonly SignalRClientFactory _signalRFactory;
    private readonly NotificationService _notificationService;
    
    private AxisInfo _axisInfo = new();
    public AxisInfo AxisInfo
    {
        get => _axisInfo;
        set => SetProperty(ref _axisInfo, value);
    }
    
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    private string _newSpeed = "100";
    public string NewSpeed
    {
        get => _newSpeed;
        set => SetProperty(ref _newSpeed, value);
    }
    
    // 计算属性
    public bool HasError => AxisInfo.LastErrorCode.HasValue && AxisInfo.LastErrorCode.Value != 0;
    
    public string EnabledText => AxisInfo.Enabled == true ? "已使能" : "未使能";
    
    public string TargetSpeedText => AxisInfo.TargetLinearMmps.HasValue 
        ? $"{AxisInfo.TargetLinearMmps.Value:F2} mm/s" 
        : "N/A";
    
    public string FeedbackSpeedText => AxisInfo.FeedbackLinearMmps.HasValue 
        ? $"{AxisInfo.FeedbackLinearMmps.Value:F2} mm/s" 
        : "N/A";
    
    public string CurrentSpeedText => $"{AxisInfo.CurrentSpeed:F2} mm/s";
    
    // Commands
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand EnableCommand { get; }
    public DelegateCommand DisableCommand { get; }
    public DelegateCommand SetSpeedCommand { get; }
    
    // 图标 Glyphs（用于绑定）
    public string RefreshGlyph => AppIcon.Refresh.ToGlyph();
    public string PlayGlyph => AppIcon.Play.ToGlyph();
    public string StopGlyph => AppIcon.Stop.ToGlyph();
    public string SpeedGlyph => AppIcon.Speed.ToGlyph();
    public string InfoGlyph => AppIcon.Info.ToGlyph();
    public string ErrorGlyph => AppIcon.Error.ToGlyph();
    public string ControllerGlyph => AppIcon.Controller.ToGlyph();
    public string SettingsGlyph => AppIcon.Settings.ToGlyph();
    
    public ControllerDetailsViewModel(ApiClient apiClient, SignalRClientFactory signalRFactory)
    {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;
        _notificationService = NotificationService.Instance;
        
        RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        EnableCommand = new DelegateCommand(async () => await EnableAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        DisableCommand = new DelegateCommand(async () => await DisableAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        SetSpeedCommand = new DelegateCommand(async () => await SetSpeedAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        
        // 订阅SignalR速度变化事件
        _signalRFactory.SpeedChanged += OnSpeedChanged;
    }
    
    public void OnNavigatedTo(INavigationParameters parameters)
    {
        // 从导航参数获取轴信息
        if (parameters.TryGetValue<AxisInfo>("axis", out var axis))
        {
            AxisInfo = axis;
            RaisePropertyChanged(nameof(HasError));
            RaisePropertyChanged(nameof(EnabledText));
            RaisePropertyChanged(nameof(TargetSpeedText));
            RaisePropertyChanged(nameof(FeedbackSpeedText));
            RaisePropertyChanged(nameof(CurrentSpeedText));
            
            // 自动刷新
            _ = Task.Run(async () => await RefreshAsync());
        }
        else if (parameters.TryGetValue<string>("axisId", out var axisId))
        {
            // 如果只有ID，则加载详情
            AxisInfo = new AxisInfo { AxisId = axisId };
            _ = Task.Run(async () => await RefreshAsync());
        }
    }
    
    public void OnNavigatedFrom(INavigationParameters parameters)
    {
        // 清理工作
    }
    
    private async Task RefreshAsync()
    {
        await SafeExecutor.ExecuteAsync(
            async () =>
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                
                IsLoading = true;
                StatusMessage = "刷新中...";
                
                // 获取最新的轴信息
                var response = await _apiClient.GetControllersAsync();
                if (response.Success && response.Data != null)
                {
                    var axis = response.Data.FirstOrDefault(a => a.AxisId == AxisInfo.AxisId);
                    if (axis != null)
                    {
                        AxisInfo = axis;
                        RaisePropertyChanged(nameof(HasError));
                        RaisePropertyChanged(nameof(EnabledText));
                        RaisePropertyChanged(nameof(TargetSpeedText));
                        RaisePropertyChanged(nameof(FeedbackSpeedText));
                        RaisePropertyChanged(nameof(CurrentSpeedText));
                        
                        StatusMessage = "刷新成功";
                        _notificationService.ShowSuccess("刷新成功");
                    }
                    else
                    {
                        StatusMessage = "未找到该轴";
                        _notificationService.ShowError("未找到该轴");
                    }
                }
                else
                {
                    StatusMessage = $"刷新失败: {response.Message}";
                    _notificationService.ShowError($"刷新失败: {response.Message}");
                }
            },
            ex =>
            {
                var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
                StatusMessage = friendlyMessage;
                _notificationService.ShowError(friendlyMessage);
            },
            "RefreshAxisDetails",
            timeout: 10000
        );
        
        IsLoading = false;
    }
    
    private async Task EnableAsync()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            IsLoading = true;
            StatusMessage = "使能中...";
            
            // Parse AxisId to int array for API call
            int axisIdInt = int.TryParse(AxisInfo.AxisId.Replace("axis", ""), out var id) ? id : 0;
            var response = await _apiClient.EnableAxesAsync(new[] { axisIdInt });
            if (response.Success)
            {
                StatusMessage = "使能成功";
                _notificationService.ShowSuccess("使能成功");
                await RefreshAsync();
            }
            else
            {
                StatusMessage = $"使能失败: {response.Message}";
                _notificationService.ShowError($"使能失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = friendlyMessage;
            _notificationService.ShowError(friendlyMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task DisableAsync()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            IsLoading = true;
            StatusMessage = "禁用中...";
            
            // Parse AxisId to int array for API call
            int axisIdInt = int.TryParse(AxisInfo.AxisId.Replace("axis", ""), out var id) ? id : 0;
            var response = await _apiClient.DisableAxesAsync(new[] { axisIdInt });
            if (response.Success)
            {
                StatusMessage = "禁用成功";
                _notificationService.ShowSuccess("禁用成功");
                await RefreshAsync();
            }
            else
            {
                StatusMessage = $"禁用失败: {response.Message}";
                _notificationService.ShowError($"禁用失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = friendlyMessage;
            _notificationService.ShowError(friendlyMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task SetSpeedAsync()
    {
        try
        {
            if (!double.TryParse(NewSpeed, out var speed))
            {
                StatusMessage = "速度格式错误";
                _notificationService.ShowError("速度格式错误");
                return;
            }
            
            if (speed < 0 || speed > 2000)
            {
                StatusMessage = "速度范围应在 0-2000 mm/s";
                _notificationService.ShowError("速度范围应在 0-2000 mm/s");
                return;
            }
            
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            IsLoading = true;
            StatusMessage = $"设置速度为 {speed} mm/s...";
            
            // Parse AxisId to int array for API call
            int axisIdInt = int.TryParse(AxisInfo.AxisId.Replace("axis", ""), out var id) ? id : 0;
            var response = await _apiClient.SetAxesSpeedAsync(speed, new[] { axisIdInt });
            if (response.Success)
            {
                StatusMessage = $"速度设置为 {speed} mm/s 成功";
                _notificationService.ShowSuccess($"速度设置为 {speed} mm/s");
                await RefreshAsync();
            }
            else
            {
                StatusMessage = $"设置失败: {response.Message}";
                _notificationService.ShowError($"设置失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            var friendlyMessage = ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message);
            StatusMessage = friendlyMessage;
            _notificationService.ShowError(friendlyMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void OnSpeedChanged(object? sender, SpeedChangedEventArgs e)
    {
        // 只更新当前轴的速度
        if (e.AxisId.ToString() == AxisInfo.AxisId || e.AxisId == int.Parse(AxisInfo.AxisId.Replace("axis", "")))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AxisInfo.CurrentSpeed = e.Speed;
                RaisePropertyChanged(nameof(CurrentSpeedText));
            });
        }
    }
}
