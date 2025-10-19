using System.Net.NetworkInformation;

namespace ZakYip.Singulation.MauiApp.Helpers;

/// <summary>
/// 网络诊断工具，检测网络状态
/// </summary>
public static class NetworkDiagnostics
{
    /// <summary>
    /// 检查网络连接状态
    /// </summary>
    public static NetworkStatus CheckNetworkStatus()
    {
        var result = new NetworkStatus
        {
            IsConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet,
            ConnectionType = GetConnectionTypeName()
        };
        
        // 检查是否在本地网络
        result.IsOnLocalNetwork = IsOnLocalNetwork();
        
        // 生成诊断消息
        result.DiagnosticMessage = GenerateDiagnosticMessage(result);
        
        return result;
    }
    
    /// <summary>
    /// 获取连接类型名称
    /// </summary>
    private static string GetConnectionTypeName()
    {
        var profiles = Connectivity.Current.ConnectionProfiles;
        
        if (profiles.Contains(ConnectionProfile.WiFi))
            return "WiFi";
        
        if (profiles.Contains(ConnectionProfile.Cellular))
            return "移动数据";
        
        if (profiles.Contains(ConnectionProfile.Ethernet))
            return "以太网";
        
        if (profiles.Contains(ConnectionProfile.Bluetooth))
            return "蓝牙";
        
        return "未知";
    }
    
    /// <summary>
    /// 检查是否在本地网络（简单检测）
    /// </summary>
    private static bool IsOnLocalNetwork()
    {
        try
        {
            // 检查是否有WiFi或以太网连接
            var profiles = Connectivity.Current.ConnectionProfiles;
            return profiles.Contains(ConnectionProfile.WiFi) || 
                   profiles.Contains(ConnectionProfile.Ethernet);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 生成诊断消息
    /// </summary>
    private static string GenerateDiagnosticMessage(NetworkStatus status)
    {
        if (!status.IsConnected)
            return "网络未连接。请检查设备的网络设置。";
        
        if (!status.IsOnLocalNetwork)
            return $"当前使用 {status.ConnectionType} 连接。建议连接到与服务器相同的 WiFi 网络以获得更好的性能。";
        
        return $"网络正常（{status.ConnectionType}）";
    }
    
    /// <summary>
    /// 检测是否支持服务发现（简单检查WiFi连接）
    /// </summary>
    public static DiscoveryAvailability CheckDiscoveryAvailability()
    {
        var result = new DiscoveryAvailability();
        
        var networkStatus = CheckNetworkStatus();
        result.IsAvailable = networkStatus.IsConnected && networkStatus.IsOnLocalNetwork;
        
        if (!networkStatus.IsConnected)
        {
            result.Message = "网络未连接。请连接到网络后重试。";
            result.Suggestion = "1. 打开设置\n2. 连接到 WiFi 网络\n3. 返回应用重试";
        }
        else if (!networkStatus.IsOnLocalNetwork)
        {
            result.Message = $"当前使用 {networkStatus.ConnectionType} 连接。";
            result.Suggestion = "建议：\n1. 连接到与服务器相同的 WiFi 网络\n2. 或手动输入服务器地址";
        }
        else
        {
            result.Message = "网络连接正常，可以使用自动发现功能。";
            result.Suggestion = "";
        }
        
        return result;
    }
}

/// <summary>
/// 网络状态信息
/// </summary>
public class NetworkStatus
{
    public bool IsConnected { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    public bool IsOnLocalNetwork { get; set; }
    public string DiagnosticMessage { get; set; } = string.Empty;
}

/// <summary>
/// 服务发现可用性信息
/// </summary>
public class DiscoveryAvailability
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}
