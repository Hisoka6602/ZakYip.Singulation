using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Leadshine;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Services;

/// <summary>
/// 连接健康检查服务
/// 提供雷赛控制器和上游连接的健康状态检查功能
/// </summary>
public sealed class ConnectionHealthCheckService
{
    private readonly ILogger<ConnectionHealthCheckService> _logger;
    private readonly IAxisController? _axisController;
    private readonly IByteTransport? _upstreamTransport;
    private readonly ISystemClock _clock;
    private readonly string? _leadshineIp;
    private readonly string? _upstreamIp;
    private readonly int _upstreamPort;

    public ConnectionHealthCheckService(
        ILogger<ConnectionHealthCheckService> logger,
        ISystemClock clock,
        IAxisController? axisController = null,
        IByteTransport? upstreamTransport = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _axisController = axisController;
        _upstreamTransport = upstreamTransport;
        
        // 尝试从传输层获取上游IP和端口
        if (_upstreamTransport != null)
        {
            _upstreamIp = _upstreamTransport.RemoteIp;
            _upstreamPort = _upstreamTransport.RemotePort;
        }
        
        // 尝试从控制器获取雷赛IP
        if (_axisController != null)
        {
            try
            {
                var busAdapter = _axisController.Bus as LeadshineLtdmcBusAdapter;
                _leadshineIp = busAdapter?.ControllerIp;
            }
            catch (Exception ex) // Intentional: Graceful initialization - bus adapter access may fail
            {
                _logger.LogWarning(ex, "无法从AxisController获取雷赛IP地址");
            }
        }
    }
    
    /// <summary>
    /// 检查所有连接的健康状态
    /// </summary>
    public async Task<ConnectionHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("开始连接健康检查");
        
        var result = new ConnectionHealthCheckResult
        {
            CheckedAt = _clock.UtcNow,
            LeadshineConnection = await CheckLeadshineConnectionAsync(ct),
            UpstreamConnection = _upstreamTransport != null && !string.IsNullOrEmpty(_upstreamIp) 
                ? await CheckUpstreamConnectionAsync(ct) 
                : null
        };
        
        _logger.LogInformation("连接健康检查完成 - 雷赛: {LeadshineConnected}, 上游: {UpstreamConnected}",
            result.LeadshineConnection.IsConnected,
            result.UpstreamConnection?.IsConnected ?? true);
        
        return result;
    }
    
    /// <summary>
    /// 检查雷赛控制器连接状态
    /// </summary>
    private async Task<LeadshineConnectionHealth> CheckLeadshineConnectionAsync(CancellationToken ct)
    {
        var health = new LeadshineConnectionHealth
        {
            IpAddress = _leadshineIp
        };
        
        var diagnostics = new List<string>();
        
        try
        {
            // 1. 检查IP是否可达（Ping）
            if (!string.IsNullOrEmpty(_leadshineIp))
            {
                var pingResult = await PingHostAsync(_leadshineIp, 1000, ct);
                health.IsPingable = pingResult.Success;
                health.PingTimeMs = pingResult.RoundtripTime;
                
                if (pingResult.Success)
                {
                    diagnostics.Add($"✓ IP {_leadshineIp} 可达 (Ping: {pingResult.RoundtripTime}ms)");
                }
                else
                {
                    diagnostics.Add($"✗ IP {_leadshineIp} 不可达: {pingResult.ErrorMessage}");
                    health.ErrorMessage = $"无法Ping通雷赛控制器IP: {pingResult.ErrorMessage}";
                }
            }
            else
            {
                diagnostics.Add("⚠ 未配置雷赛控制器IP地址（可能使用本地PCI模式）");
                health.IsPingable = true; // 本地模式假设可用
            }
            
            // 2. 检查控制器是否初始化
            if (_axisController != null)
            {
                try
                {
                    var drives = _axisController.Drives;
                    health.IsInitialized = drives != null && drives.Any();
                    
                    if (health.IsInitialized && drives != null)
                    {
                        diagnostics.Add($"✓ 控制器已初始化 ({drives.Count} 个轴)");
                    }
                    else
                    {
                        diagnostics.Add("✗ 控制器未初始化或无可用轴");
                        if (string.IsNullOrEmpty(_leadshineIp))
                        {
                            diagnostics.Add("  提示: 未配置控制器IP地址");
                            diagnostics.Add("  建议: 如果使用以太网模式，请配置IP地址；如果使用本地PCI模式，请检查硬件连接");
                        }
                        else if (health.IsPingable)
                        {
                            diagnostics.Add($"  提示: IP {_leadshineIp} 可达，但控制器初始化失败");
                            diagnostics.Add("  建议: 检查控制器配置和硬件连接");
                        }
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"✗ 检查控制器初始化状态时出错: {ex.Message}");
                    health.ErrorMessage = $"检查控制器状态失败: {ex.Message}";
                }
            }
            else
            {
                diagnostics.Add("⚠ AxisController未注入，无法检查初始化状态");
            }
        }
        catch (Exception ex) // Intentional: Health check should not fail - collect diagnostics instead
        {
            _logger.LogError(ex, "检查雷赛连接时发生异常");
            diagnostics.Add($"✗ 检查失败: {ex.Message}");
            health.ErrorMessage = $"检查雷赛连接时发生异常: {ex.Message}";
        }
        
        health.DiagnosticMessages = diagnostics;
        return health;
    }
    
    /// <summary>
    /// 检查上游连接状态
    /// </summary>
    private async Task<UpstreamConnectionHealth> CheckUpstreamConnectionAsync(CancellationToken ct)
    {
        var health = new UpstreamConnectionHealth
        {
            IpAddress = _upstreamIp,
            Port = _upstreamPort
        };
        
        var diagnostics = new List<string>();
        
        try
        {
            // 1. 检查IP是否可达（Ping）
            if (!string.IsNullOrEmpty(_upstreamIp))
            {
                var pingResult = await PingHostAsync(_upstreamIp, 1000, ct);
                health.IsPingable = pingResult.Success;
                health.PingTimeMs = pingResult.RoundtripTime;
                
                if (pingResult.Success)
                {
                    diagnostics.Add($"✓ 上游IP {_upstreamIp} 可达 (Ping: {pingResult.RoundtripTime}ms)");
                }
                else
                {
                    diagnostics.Add($"✗ 上游IP {_upstreamIp} 不可达: {pingResult.ErrorMessage}");
                    health.ErrorMessage = $"无法Ping通上游IP: {pingResult.ErrorMessage}";
                }
            }
            
            // 2. 检查传输层连接状态
            if (_upstreamTransport != null)
            {
                var transportStatus = _upstreamTransport.Status;
                health.IsTransportConnected = transportStatus == Core.Enums.TransportConnectionState.Connected;
                health.TransportState = transportStatus.ToString();
                
                if (health.IsTransportConnected)
                {
                    diagnostics.Add($"✓ 上游传输层已连接 (状态: {transportStatus})");
                }
                else
                {
                    diagnostics.Add($"✗ 上游传输层未连接 (状态: {transportStatus})");
                    if (health.IsPingable)
                    {
                        diagnostics.Add($"  提示: IP {_upstreamIp} 可达，但TCP连接未建立");
                        diagnostics.Add($"  建议: 检查上游服务是否在端口 {_upstreamPort} 监听");
                    }
                }
            }
            else
            {
                diagnostics.Add("⚠ 上游传输层未配置");
            }
        }
        catch (Exception ex) // Intentional: Health check should not fail - collect diagnostics instead
        {
            _logger.LogError(ex, "检查上游连接时发生异常");
            diagnostics.Add($"✗ 检查失败: {ex.Message}");
            health.ErrorMessage = $"检查上游连接时发生异常: {ex.Message}";
        }
        
        health.DiagnosticMessages = diagnostics;
        return health;
    }
    
    /// <summary>
    /// Ping指定主机
    /// </summary>
    private async Task<PingResult> PingHostAsync(string host, int timeoutMs, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, timeoutMs);
            
            return new PingResult
            {
                Success = reply.Status == IPStatus.Success,
                RoundtripTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : null,
                ErrorMessage = reply.Status != IPStatus.Success ? reply.Status.ToString() : null
            };
        }
        catch (PingException ex)
        {
            return new PingResult
            {
                Success = false,
                ErrorMessage = $"Ping失败: {ex.Message}"
            };
        }
        catch (Exception ex) // Intentional: Network operation - various exceptions possible (timeout, unreachable, etc)
        {
            return new PingResult
            {
                Success = false,
                ErrorMessage = $"Ping异常: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Ping结果内部类
    /// </summary>
    private class PingResult
    {
        public bool Success { get; set; }
        public long? RoundtripTime { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

/// <summary>
/// 连接健康检查结果
/// </summary>
public class ConnectionHealthCheckResult
{
    public LeadshineConnectionHealth LeadshineConnection { get; set; } = new();
    public UpstreamConnectionHealth? UpstreamConnection { get; set; }
    public bool IsHealthy => LeadshineConnection.IsConnected && 
                            (UpstreamConnection == null || UpstreamConnection.IsConnected);
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// 雷赛连接健康状态
/// </summary>
public class LeadshineConnectionHealth
{
    public string? IpAddress { get; set; }
    public bool IsPingable { get; set; }
    public long? PingTimeMs { get; set; }
    public bool IsInitialized { get; set; }
    public bool IsConnected => IsPingable && IsInitialized;
    public string? ErrorMessage { get; set; }
    public List<string> DiagnosticMessages { get; set; } = new();
}

/// <summary>
/// 上游连接健康状态
/// </summary>
public class UpstreamConnectionHealth
{
    public string? IpAddress { get; set; }
    public int Port { get; set; }
    public bool IsPingable { get; set; }
    public long? PingTimeMs { get; set; }
    public bool IsTransportConnected { get; set; }
    public string TransportState { get; set; } = "Unknown";
    public bool IsConnected => IsPingable && IsTransportConnected;
    public string? ErrorMessage { get; set; }
    public List<string> DiagnosticMessages { get; set; } = new();
}
