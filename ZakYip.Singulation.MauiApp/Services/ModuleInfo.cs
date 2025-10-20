using Prism.Mvvm;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// 单件分离模块信息
/// </summary>
public class SingulationModule : BindableBase
{
    private string _moduleId = string.Empty;
    public string ModuleId
    {
        get => _moduleId;
        set => SetProperty(ref _moduleId, value);
    }

    private int _row;
    public int Row
    {
        get => _row;
        set => SetProperty(ref _row, value);
    }

    private int _column;
    public int Column
    {
        get => _column;
        set => SetProperty(ref _column, value);
    }

    private double _speed;
    public double Speed
    {
        get => _speed;
        set
        {
            if (SetProperty(ref _speed, value))
            {
                RaisePropertyChanged(nameof(SpeedText));
                RaisePropertyChanged(nameof(SpeedColor));
            }
        }
    }

    private ModuleStatus _status = ModuleStatus.Idle;
    public ModuleStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusColor));
            }
        }
    }

    private string _axisId = string.Empty;
    public string AxisId
    {
        get => _axisId;
        set => SetProperty(ref _axisId, value);
    }

    // 显示属性
    public string DisplayName => $"M{Row}x{Column}";
    public string SpeedText => $"{Speed:F1} mm/s";
    public string StatusText => Status switch
    {
        ModuleStatus.Idle => "空闲",
        ModuleStatus.Running => "运行中",
        ModuleStatus.Error => "错误",
        ModuleStatus.Offline => "离线",
        _ => "未知"
    };

    public Color StatusColor => Status switch
    {
        ModuleStatus.Idle => Colors.LightGray,
        ModuleStatus.Running => Colors.LightGreen,
        ModuleStatus.Error => Colors.LightCoral,
        ModuleStatus.Offline => Colors.LightSlateGray,
        _ => Colors.White
    };

    public Color SpeedColor => Speed > 0 ? Colors.Green : Colors.Gray;
}

/// <summary>
/// 模块状态枚举
/// </summary>
public enum ModuleStatus
{
    Idle = 0,
    Running = 1,
    Error = 2,
    Offline = 3
}
