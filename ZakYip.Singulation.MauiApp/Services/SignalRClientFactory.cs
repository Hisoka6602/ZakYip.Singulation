using Microsoft.AspNetCore.SignalR.Client;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// SignalR客户端工厂，用于创建和管理实时连接
/// 支持自动重连、事件订阅和实时通知
/// </summary>
public class SignalRClientFactory
{
    private readonly string _baseUrl;
    private HubConnection? _hubConnection;
    
    // 事件订阅
    public event EventHandler<string>? MessageReceived;
    public event EventHandler<SpeedChangedEventArgs>? SpeedChanged;
    public event EventHandler<SafetyEventArgs>? SafetyEventOccurred;
    public event EventHandler<HubConnectionState>? ConnectionStateChanged;

    public SignalRClientFactory(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// 创建或获取Hub连接，使用指数退避重连策略
    /// </summary>
    public async Task<HubConnection> GetOrCreateHubConnectionAsync(string hubPath = "/hubs/events")
    {
        if (_hubConnection == null)
        {
            // 使用指数退避重连策略：0s, 2s, 10s, 30s, 然后每60s重试
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}{hubPath}")
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero, 
                    TimeSpan.FromSeconds(2), 
                    TimeSpan.FromSeconds(10), 
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(1)
                })
                .Build();

            // 注册通用消息处理器
            _hubConnection.On<string>("ReceiveMessage", (message) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Message: {message}");
                MessageReceived?.Invoke(this, message);
            });

            // 注册通用事件处理器
            _hubConnection.On<string, object>("ReceiveEvent", (eventName, data) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Event: {eventName}, Data: {data}");
            });

            // 注册轴速度变化事件
            _hubConnection.On<int, double>("AxisSpeedChanged", (axisId, speed) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Axis {axisId} speed changed to {speed} mm/s");
                SpeedChanged?.Invoke(this, new SpeedChangedEventArgs(axisId, speed));
            });

            // 注册安全事件
            _hubConnection.On<string, string, DateTime>("SafetyEvent", (eventType, message, timestamp) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Safety Event: {eventType} - {message} at {timestamp}");
                SafetyEventOccurred?.Invoke(this, new SafetyEventArgs(eventType, message, timestamp));
            });

            // 连接状态变化事件
            _hubConnection.Reconnecting += error =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Reconnecting... Error: {error?.Message}");
                ConnectionStateChanged?.Invoke(this, HubConnectionState.Reconnecting);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Reconnected. Connection ID: {connectionId}");
                ConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Connection closed. Error: {error?.Message}");
                ConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
                return Task.CompletedTask;
            };
        }

        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                System.Diagnostics.Debug.WriteLine("[SignalR] Connected successfully");
                ConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Failed to connect: {ex.Message}");
                throw;
            }
        }

        return _hubConnection;
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    
    /// <summary>
    /// 获取当前连接状态
    /// </summary>
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;
}

/// <summary>
/// 速度变化事件参数
/// </summary>
public class SpeedChangedEventArgs : EventArgs
{
    public int AxisId { get; }
    public double Speed { get; }
    
    public SpeedChangedEventArgs(int axisId, double speed)
    {
        AxisId = axisId;
        Speed = speed;
    }
}

/// <summary>
/// 安全事件参数
/// </summary>
public class SafetyEventArgs : EventArgs
{
    public string EventType { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }
    
    public SafetyEventArgs(string eventType, string message, DateTime timestamp)
    {
        EventType = eventType;
        Message = message;
        Timestamp = timestamp;
    }
}
