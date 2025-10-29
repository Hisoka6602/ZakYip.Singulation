using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.Singulation.Infrastructure.Services;

/// <summary>
/// UDP 服务发现广播服务
/// 定期通过 UDP 广播发送服务信息，让 MauiApp 可以自动发现服务
/// </summary>
public class UdpDiscoveryService : BackgroundService
{
    private readonly ILogger<UdpDiscoveryService> _logger;
    private readonly UdpDiscoveryOptions _options;
    private UdpClient? _udpClient;

    public UdpDiscoveryService(
        ILogger<UdpDiscoveryService> logger,
        IOptions<UdpDiscoveryOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("UDP 服务发现已禁用");
            return;
        }

        _logger.LogInformation("UDP 服务发现服务启动，端口: {Port}, 间隔: {Interval}秒",
            _options.BroadcastPort, _options.BroadcastIntervalSeconds);

        try
        {
            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _options.BroadcastPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var serviceInfo = new ServiceDiscoveryInfo
                    {
                        ServiceName = _options.ServiceName,
                        ServiceType = "ZakYip.Singulation.Host",
                        Version = "1.0",
                        HttpPort = _options.HttpPort,
                        HttpsPort = _options.HttpsPort,
                        SignalRPath = "/hubs/events",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    var json = JsonSerializer.Serialize(serviceInfo);
                    var data = Encoding.UTF8.GetBytes(json);

                    await _udpClient.SendAsync(data, data.Length, broadcastEndpoint);

                    //_logger.LogDebug("已发送 UDP 广播: {Json}", json);

                    await Task.Delay(TimeSpan.FromSeconds(_options.BroadcastIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 正常停止（包括 TaskCanceledException），不记录日志
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送 UDP 广播时发生错误");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP 服务发现服务异常");
        }
        finally
        {
            _udpClient?.Dispose();
            _logger.LogInformation("UDP 服务发现服务已停止");
        }
    }

    public override void Dispose()
    {
        _udpClient?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// UDP 服务发现配置选项
/// </summary>
public class UdpDiscoveryOptions
{
    /// <summary>是否启用 UDP 服务发现</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>UDP 广播端口</summary>
    public int BroadcastPort { get; set; } = 18888;

    /// <summary>广播间隔（秒）</summary>
    public int BroadcastIntervalSeconds { get; set; } = 3;

    /// <summary>服务名称</summary>
    public string ServiceName { get; set; } = "Singulation Service";

    /// <summary>HTTP 端口</summary>
    public int HttpPort { get; set; } = 5000;

    /// <summary>HTTPS 端口</summary>
    public int HttpsPort { get; set; } = 5001;
}

/// <summary>
/// 服务发现信息（通过 UDP 广播的数据结构）
/// </summary>
public class ServiceDiscoveryInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public string SignalRPath { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
