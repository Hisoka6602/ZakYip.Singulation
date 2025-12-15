namespace ZakYip.Singulation.Host.Dto;

/// <summary>
/// 连接健康检查响应DTO
/// </summary>
public record ConnectionHealthDto
{
    /// <summary>
    /// 雷赛控制器连接状态
    /// </summary>
    public LeadshineConnectionStatus LeadshineConnection { get; init; } = new();
    
    /// <summary>
    /// 上游连接状态（如果配置了）
    /// </summary>
    public UpstreamConnectionStatus? UpstreamConnection { get; init; }
    
    /// <summary>
    /// 整体健康状态。
    /// 当雷赛控制器连接健康（LeadshineConnection.IsConnected 为 true），且上游连接健康（UpstreamConnection.IsConnected 为 true）或未配置（UpstreamConnection 为 null）时，返回 true。
    /// </summary>
    public bool IsHealthy => LeadshineConnection.IsConnected && 
                            (UpstreamConnection == null || UpstreamConnection.IsConnected);
    
    /// <summary>
    /// 检查时间
    /// </summary>
    public required DateTime CheckedAt { get; init; }
}

/// <summary>
/// 雷赛控制器连接状态
/// </summary>
public record LeadshineConnectionStatus
{
    /// <summary>
    /// 控制器IP地址
    /// </summary>
    public string? IpAddress { get; init; }
    
    /// <summary>
    /// IP是否可达（Ping通）
    /// </summary>
    public bool IsPingable { get; init; }
    
    /// <summary>
    /// Ping响应时间（毫秒）
    /// </summary>
    public long? PingTimeMs { get; init; }
    
    /// <summary>
    /// 控制器是否已初始化
    /// </summary>
    public bool IsInitialized { get; init; }
    
    /// <summary>
    /// 控制器是否连接（IP可达且已初始化）
    /// </summary>
    public bool IsConnected => IsPingable && IsInitialized;
    
    /// <summary>
    /// 错误消息（如果有）
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 诊断信息
    /// </summary>
    public List<string> DiagnosticMessages { get; init; } = new();
}

/// <summary>
/// 上游连接状态
/// </summary>
public record UpstreamConnectionStatus
{
    /// <summary>
    /// 上游IP地址
    /// </summary>
    public string? IpAddress { get; init; }
    
    /// <summary>
    /// 上游端口
    /// </summary>
    public int Port { get; init; }
    
    /// <summary>
    /// IP是否可达（Ping通）
    /// </summary>
    public bool IsPingable { get; init; }
    
    /// <summary>
    /// Ping响应时间（毫秒）
    /// </summary>
    public long? PingTimeMs { get; init; }
    
    /// <summary>
    /// 传输层是否连接
    /// </summary>
    public bool IsTransportConnected { get; init; }
    
    /// <summary>
    /// 传输层连接状态
    /// </summary>
    public string TransportState { get; init; } = "Unknown";
    
    /// <summary>
    /// 上游是否连接（IP可达且传输层已连接）
    /// </summary>
    public bool IsConnected => IsPingable && IsTransportConnected;
    
    /// <summary>
    /// 错误消息（如果有）
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 诊断信息
    /// </summary>
    public List<string> DiagnosticMessages { get; init; } = new();
}
