using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using ZakYip.Singulation.MauiApp.Services;
using ZakYip.Singulation.MauiApp.Helpers;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 模块网格视图模型
/// </summary>
public class ModuleGridViewModel : BindableBase
{
    private readonly ApiClient _apiClient;
    private readonly SignalRClientFactory _signalRFactory;
    private readonly NotificationService _notificationService;
    private readonly SafeExecutor.CircuitBreaker _apiCircuitBreaker;

    private ObservableCollection<SingulationModule> _modules = new();
    public ObservableCollection<SingulationModule> Modules
    {
        get => _modules;
        set => SetProperty(ref _modules, value);
    }

    private SingulationModule? _selectedModule;
    public SingulationModule? SelectedModule
    {
        get => _selectedModule;
        set => SetProperty(ref _selectedModule, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private int _gridRows = 4;
    public int GridRows
    {
        get => _gridRows;
        set
        {
            if (SetProperty(ref _gridRows, value))
            {
                InitializeDefaultModules();
            }
        }
    }

    private int _gridColumns = 6;
    public int GridColumns
    {
        get => _gridColumns;
        set
        {
            if (SetProperty(ref _gridColumns, value))
            {
                InitializeDefaultModules();
            }
        }
    }

    // 统计属性
    public int TotalCount => Modules.Count;
    public int RunningCount => Modules.Count(m => m.Status == ModuleStatus.Running);
    public int IdleCount => Modules.Count(m => m.Status == ModuleStatus.Idle);
    public int ErrorCount => Modules.Count(m => m.Status == ModuleStatus.Error);

    public DelegateCommand RefreshModulesCommand { get; }
    public DelegateCommand ModuleSelectedCommand { get; }

    public ModuleGridViewModel(
        ApiClient apiClient,
        SignalRClientFactory signalRFactory)
    {
        _apiClient = apiClient;
        _signalRFactory = signalRFactory;
        _notificationService = NotificationService.Instance;
        _apiCircuitBreaker = new SafeExecutor.CircuitBreaker(failureThreshold: 3, resetTimeoutSeconds: 60);

        RefreshModulesCommand = new DelegateCommand(async () => await RefreshModulesAsync(), () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        ModuleSelectedCommand = new DelegateCommand(OnModuleSelected);

        // 订阅SignalR速度更新事件
        _signalRFactory.SpeedChanged += OnSpeedChanged;

        // 初始化默认4x6布局
        InitializeDefaultModules();

        // 自动刷新模块数据
        _ = Task.Run(async () => await AutoRefreshAsync());
    }

    /// <summary>
    /// 初始化默认模块布局
    /// </summary>
    private void InitializeDefaultModules()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Modules.Clear();
            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    var module = new SingulationModule
                    {
                        ModuleId = $"M{row}_{col}",
                        Row = row,
                        Column = col,
                        Speed = 0,
                        Status = ModuleStatus.Idle,
                        AxisId = $"axis{row * GridColumns + col + 1}"
                    };
                    Modules.Add(module);
                }
            }
            UpdateStatistics();
        });
    }

    /// <summary>
    /// 自动刷新模块数据
    /// </summary>
    private async Task AutoRefreshAsync()
    {
        await Task.Delay(1500); // 等待页面加载完成
        await RefreshModulesAsync();
    }

    /// <summary>
    /// 刷新模块数据
    /// </summary>
    private async Task RefreshModulesAsync()
    {
        // 使用断路器模式保护API调用
        await _apiCircuitBreaker.ExecuteAsync(
            async () => await RefreshModulesInternalAsync(),
            ex => _notificationService.ShowError($"刷新失败: {ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message)}"),
            "RefreshModules"
        );
    }

    /// <summary>
    /// 刷新模块数据的内部实现
    /// </summary>
    private async Task RefreshModulesInternalAsync()
    {
        IsLoading = true;

        try
        {
            // 使用SafeExecutor保护API调用
            var response = await SafeExecutor.ExecuteAsync(
                async () => await _apiClient.GetControllersAsync(),
                new ApiResponse<List<AxisInfo>> { Result = false, Msg = "Request failed" },
                ex => System.Diagnostics.Debug.WriteLine($"[ModuleGridViewModel] API Error: {ex.Message}"),
                "GetControllers",
                timeout: 10000
            );

            if (response.Success && response.Data != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // 更新模块状态和速度
                    foreach (var axis in response.Data)
                    {
                        UpdateModuleFromAxis(axis);
                    }

                    UpdateStatistics();
                    _notificationService.ShowSuccess($"已更新 {response.Data.Count} 个模块");
                });
            }
            else
            {
                _notificationService.ShowWarning($"刷新警告: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"刷新异常: {ErrorMessageHelper.GetFriendlyErrorMessage(ex.Message)}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 从轴信息更新模块
    /// </summary>
    private void UpdateModuleFromAxis(AxisInfo axis)
    {
        // 尝试从AxisId解析行列位置
        if (TryParseAxisId(axis.AxisId, out int axisIndex))
        {
            // 假设轴ID从1开始，按行优先顺序排列
            int row = (axisIndex - 1) / GridColumns;
            int col = (axisIndex - 1) % GridColumns;

            var module = Modules.FirstOrDefault(m => m.Row == row && m.Column == col);
            if (module != null)
            {
                module.AxisId = axis.AxisId;
                module.Speed = axis.CurrentSpeed > 0 ? axis.CurrentSpeed : (axis.FeedbackLinearMmps ?? 0);
                module.Status = axis.Status switch
                {
                    0 => ModuleStatus.Offline,  // 离线
                    1 => ModuleStatus.Idle,      // 初始化中
                    2 => ModuleStatus.Idle,      // 就绪
                    3 => ModuleStatus.Running,   // 运行中
                    4 => ModuleStatus.Error,     // 故障
                    _ => ModuleStatus.Idle
                };
            }
        }
    }

    /// <summary>
    /// 解析轴ID获取索引
    /// </summary>
    private bool TryParseAxisId(string axisId, out int index)
    {
        index = 0;
        if (string.IsNullOrEmpty(axisId))
            return false;

        // 尝试从 "axis1" 或 "1" 格式解析
        var numericPart = new string(axisId.Where(char.IsDigit).ToArray());
        return int.TryParse(numericPart, out index);
    }

    /// <summary>
    /// 处理速度变化事件
    /// </summary>
    private void OnSpeedChanged(object? sender, SpeedChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 根据轴ID更新对应模块的速度
            if (TryParseAxisId($"axis{e.AxisId}", out int axisIndex))
            {
                int row = (axisIndex - 1) / GridColumns;
                int col = (axisIndex - 1) % GridColumns;

                var module = Modules.FirstOrDefault(m => m.Row == row && m.Column == col);
                if (module != null)
                {
                    module.Speed = e.Speed;
                    if (e.Speed > 0)
                    {
                        module.Status = ModuleStatus.Running;
                    }
                    UpdateStatistics();
                }
            }
        });
    }

    /// <summary>
    /// 模块选中事件
    /// </summary>
    private void OnModuleSelected()
    {
        if (SelectedModule != null)
        {
            SafeExecutor.Execute(
                () =>
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    _notificationService.ShowInfo($"已选择模块 {SelectedModule.DisplayName}");
                },
                ex => System.Diagnostics.Debug.WriteLine($"[ModuleGridViewModel] Haptic feedback error: {ex.Message}"),
                "ModuleSelected"
            );
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(RunningCount));
        RaisePropertyChanged(nameof(IdleCount));
        RaisePropertyChanged(nameof(ErrorCount));
    }
}
