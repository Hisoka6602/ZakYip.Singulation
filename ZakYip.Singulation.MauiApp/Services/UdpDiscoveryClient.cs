using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// UDP 服务发现客户端
/// 监听 UDP 广播并发现可用的 Singulation 服务
/// </summary>
public class UdpDiscoveryClient : IDisposable
{
    private readonly int _listenPort;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private bool _isListening;

    public event EventHandler<DiscoveredService>? ServiceDiscovered;
    public event EventHandler<DiscoveredService>? ServiceLost;

    private readonly Dictionary<string, DiscoveredService> _discoveredServices = new();
    private readonly Dictionary<string, DateTime> _lastSeen = new();
    private readonly TimeSpan _serviceTimeout = TimeSpan.FromSeconds(10);

    public ObservableCollection<DiscoveredService> DiscoveredServices { get; } = new();

    public UdpDiscoveryClient(int listenPort = 18888)
    {
        _listenPort = listenPort;
    }

    /// <summary>
    /// 开始监听 UDP 广播
    /// </summary>
    public async Task StartListeningAsync()
    {
        if (_isListening)
            return;

        _isListening = true;
        _cts = new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient(_listenPort);
            _udpClient.EnableBroadcast = true;

            // 启动接收任务
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

            // 启动超时检查任务
            _ = Task.Run(() => TimeoutCheckLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动 UDP 监听失败: {ex.Message}");
            _isListening = false;
            throw;
        }
    }

    /// <summary>
    /// 停止监听 UDP 广播
    /// </summary>
    public void StopListening()
    {
        if (!_isListening)
            return;

        _isListening = false;
        _cts?.Cancel();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                var json = Encoding.UTF8.GetString(result.Buffer);
                
                var serviceInfo = JsonSerializer.Deserialize<ServiceDiscoveryInfo>(json);
                if (serviceInfo != null)
                {
                    await ProcessDiscoveredServiceAsync(serviceInfo, result.RemoteEndPoint.Address.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"接收 UDP 数据失败: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task TimeoutCheckLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                CheckForLostServices();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private Task ProcessDiscoveredServiceAsync(ServiceDiscoveryInfo info, string ipAddress)
    {
        var key = $"{ipAddress}:{info.HttpPort}";
        var now = DateTime.UtcNow;

        lock (_discoveredServices)
        {
            _lastSeen[key] = now;

            if (!_discoveredServices.ContainsKey(key))
            {
                var service = new DiscoveredService
                {
                    ServiceName = info.ServiceName,
                    ServiceType = info.ServiceType,
                    Version = info.Version,
                    IpAddress = ipAddress,
                    HttpPort = info.HttpPort,
                    HttpsPort = info.HttpsPort,
                    SignalRPath = info.SignalRPath,
                    DiscoveredAt = now,
                    LastSeen = now
                };

                _discoveredServices[key] = service;

                // 在主线程更新 ObservableCollection
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DiscoveredServices.Add(service);
                });

                ServiceDiscovered?.Invoke(this, service);
                System.Diagnostics.Debug.WriteLine($"发现新服务: {service.ServiceName} @ {service.HttpBaseUrl}");
            }
            else
            {
                _discoveredServices[key].LastSeen = now;
            }
        }

        return Task.CompletedTask;
    }

    private void CheckForLostServices()
    {
        var now = DateTime.UtcNow;
        var lostKeys = new List<string>();

        lock (_discoveredServices)
        {
            foreach (var kvp in _lastSeen)
            {
                if (now - kvp.Value > _serviceTimeout)
                {
                    lostKeys.Add(kvp.Key);
                }
            }

            foreach (var key in lostKeys)
            {
                if (_discoveredServices.TryGetValue(key, out var service))
                {
                    _discoveredServices.Remove(key);
                    _lastSeen.Remove(key);

                    // 在主线程更新 ObservableCollection
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DiscoveredServices.Remove(service);
                    });

                    ServiceLost?.Invoke(this, service);
                    System.Diagnostics.Debug.WriteLine($"服务失联: {service.ServiceName} @ {service.HttpBaseUrl}");
                }
            }
        }
    }

    public void Dispose()
    {
        StopListening();
        _cts?.Dispose();
    }
}

/// <summary>
/// 发现的服务信息
/// </summary>
public class DiscoveredService
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public string SignalRPath { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; }
    public DateTime LastSeen { get; set; }

    public string HttpBaseUrl => $"http://{IpAddress}:{HttpPort}";
    public string HttpsBaseUrl => $"https://{IpAddress}:{HttpsPort}";
    public string SignalRUrl => $"{HttpBaseUrl}{SignalRPath}";
    
    public string DisplayName => $"{ServiceName} ({IpAddress})";
    public string DisplayInfo => $"HTTP: {HttpPort}, HTTPS: {HttpsPort}, 版本: {Version}";
}

/// <summary>
/// 服务发现信息 DTO（与 Host 端保持一致）
/// </summary>
internal class ServiceDiscoveryInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public string SignalRPath { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
