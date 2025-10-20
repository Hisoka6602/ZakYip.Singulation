namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// 控制器选项 DTO
/// </summary>
public class ControllerOptions
{
    public string Vendor { get; set; } = string.Empty;
    public string ControllerIp { get; set; } = string.Empty;
    public ControllerTemplate Template { get; set; } = new();
}
