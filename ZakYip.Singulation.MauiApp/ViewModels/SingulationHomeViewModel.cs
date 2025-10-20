using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace ZakYip.Singulation.MauiApp.ViewModels;

/// <summary>
/// 单件分离主页视图模型
/// </summary>
public class SingulationHomeViewModel : BindableBase
{
    private string _batchNumber = "DJ61957AAK00025";
    public string BatchNumber
    {
        get => _batchNumber;
        set => SetProperty(ref _batchNumber, value);
    }

    private string _selectedMode = "Auto";
    public string SelectedMode
    {
        get => _selectedMode;
        set => SetProperty(ref _selectedMode, value);
    }

    private MotorAxisInfo? _selectedMotor;
    public MotorAxisInfo? SelectedMotor
    {
        get => _selectedMotor;
        set => SetProperty(ref _selectedMotor, value);
    }

    private ObservableCollection<MotorAxisInfo> _motorAxes = new();
    public ObservableCollection<MotorAxisInfo> MotorAxes
    {
        get => _motorAxes;
        set => SetProperty(ref _motorAxes, value);
    }

    // Commands
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand SettingsCommand { get; }
    public DelegateCommand RefreshControllerCommand { get; }
    public DelegateCommand SafetyCommandCommand { get; }
    public DelegateCommand EnableAllCommand { get; }
    public DelegateCommand DisableAllCommand { get; }
    public DelegateCommand AxisSpeedSettingCommand { get; }
    public DelegateCommand<string> SelectModeCommand { get; }
    public DelegateCommand SeparateCommand { get; }
    public DelegateCommand<MotorAxisInfo> SelectMotorCommand { get; }

    public SingulationHomeViewModel()
    {
        SearchCommand = new DelegateCommand(OnSearch);
        SettingsCommand = new DelegateCommand(OnSettings);
        RefreshControllerCommand = new DelegateCommand(OnRefreshController);
        SafetyCommandCommand = new DelegateCommand(OnSafetyCommand);
        EnableAllCommand = new DelegateCommand(OnEnableAll);
        DisableAllCommand = new DelegateCommand(OnDisableAll);
        AxisSpeedSettingCommand = new DelegateCommand(OnAxisSpeedSetting);
        SelectModeCommand = new DelegateCommand<string>(OnSelectMode);
        SeparateCommand = new DelegateCommand(OnSeparate);
        SelectMotorCommand = new DelegateCommand<MotorAxisInfo>(OnSelectMotor);

        InitializeMotorAxes();
    }

    /// <summary>
    /// 初始化电机轴数据（M01-M20）
    /// </summary>
    private void InitializeMotorAxes()
    {
        var rpmValues = new int[] { 1000, 2000, 2000, 1600, 2000, 2500, 3000, 2000, 2000, 3000,
                                    1600, 1800, 1000, 1800, 1000, 1000, 1000, 1800, 1800, 1200 };
        
        MotorAxes.Clear();
        for (int i = 1; i <= 20; i++)
        {
            MotorAxes.Add(new MotorAxisInfo
            {
                MotorId = $"M{i:D2}",
                Rpm = rpmValues[i - 1],
                IsSelected = false,
                IsAbnormal = false,
                IsDisabled = false
            });
        }
    }

    private async void OnSearch()
    {
        // Implement search functionality
        await Application.Current?.MainPage?.DisplayAlert(
            "搜索",
            "搜索功能开发中...",
            "确定");
    }

    private void OnSettings()
    {
        // Navigate to settings page
        Shell.Current.GoToAsync("//SettingsPage");
    }

    private async void OnRefreshController()
    {
        // Implement refresh controller logic
        await Application.Current?.MainPage?.DisplayAlert(
            "刷新控制器",
            "控制器已刷新",
            "确定");
    }

    private async void OnSafetyCommand()
    {
        // Show safety command menu with options: 启动, 停止, 重置
        var result = await Application.Current?.MainPage?.DisplayActionSheet(
            "安全指令", 
            "取消", 
            null, 
            "启动", 
            "停止", 
            "重置");
        
        if (result != null && result != "取消")
        {
            // Handle the selected safety command
            await Application.Current?.MainPage?.DisplayAlert(
                "安全指令", 
                $"已执行: {result}", 
                "确定");
        }
    }

    private void OnEnableAll()
    {
        // Enable all motor axes
        foreach (var motor in MotorAxes)
        {
            motor.IsDisabled = false;
        }
    }

    private void OnDisableAll()
    {
        // Disable all motor axes
        foreach (var motor in MotorAxes)
        {
            motor.IsDisabled = true;
        }
    }

    private async void OnAxisSpeedSetting()
    {
        // Show axis speed setting dialog or navigate to speed setting page
        var result = await Application.Current?.MainPage?.DisplayPromptAsync(
            "轴速度设置",
            "请输入目标速度 (r/min):",
            "确定",
            "取消",
            "2000",
            keyboard: Keyboard.Numeric);
        
        if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int speed))
        {
            // Update all motor axes to the new speed
            foreach (var motor in MotorAxes)
            {
                motor.Rpm = speed;
            }
            
            await Application.Current?.MainPage?.DisplayAlert(
                "轴速度设置",
                $"已设置所有轴速度为: {speed} r/min",
                "确定");
        }
    }

    private void OnSelectMode(string mode)
    {
        SelectedMode = mode;
    }

    private void OnSelectMotor(MotorAxisInfo? motor)
    {
        if (motor == null) return;
        
        // Deselect all others
        foreach (var m in MotorAxes)
        {
            m.IsSelected = false;
        }
        
        // Select the tapped motor
        motor.IsSelected = true;
        SelectedMotor = motor;
    }

    private async void OnSeparate()
    {
        // Execute separation operation
        var mode = SelectedMode == "Auto" ? "自动分离" : "手动分离";
        var result = await Application.Current?.MainPage?.DisplayAlert(
            "分离操作",
            $"确认执行{mode}操作吗？\n批次: {BatchNumber}",
            "确定",
            "取消");
        
        if (result)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "分离操作",
                $"{mode}操作已启动",
                "确定");
        }
    }
}

/// <summary>
/// 电机轴信息模型
/// </summary>
public class MotorAxisInfo : BindableBase
{
    private string _motorId = string.Empty;
    public string MotorId
    {
        get => _motorId;
        set => SetProperty(ref _motorId, value);
    }

    private int _rpm;
    public int Rpm
    {
        get => _rpm;
        set => SetProperty(ref _rpm, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                RaisePropertyChanged(nameof(IsNormal));
            }
        }
    }

    private bool _isAbnormal;
    public bool IsAbnormal
    {
        get => _isAbnormal;
        set
        {
            if (SetProperty(ref _isAbnormal, value))
            {
                RaisePropertyChanged(nameof(IsNormal));
            }
        }
    }

    private bool _isDisabled;
    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (SetProperty(ref _isDisabled, value))
            {
                RaisePropertyChanged(nameof(IsNormal));
            }
        }
    }

    public bool IsNormal => !IsSelected && !IsAbnormal && !IsDisabled;
}
