using Microsoft.AspNetCore.SignalR.Client;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// SignalR客户端工厂，用于创建和管理实时连接
/// </summary>
public class SignalRClientFactory
{
    private readonly string _baseUrl;
    private HubConnection? _hubConnection;

    public SignalRClientFactory(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// 创建或获取Hub连接
    /// </summary>
    public async Task<HubConnection> GetOrCreateHubConnectionAsync(string hubPath = "/hubs/realtime")
    {
        if (_hubConnection == null)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}{hubPath}")
                .WithAutomaticReconnect()
                .Build();

            // 注册事件处理器
            _hubConnection.On<string>("ReceiveMessage", (message) =>
            {
                System.Diagnostics.Debug.WriteLine($"Received message: {message}");
            });

            _hubConnection.On<string, object>("ReceiveEvent", (eventName, data) =>
            {
                System.Diagnostics.Debug.WriteLine($"Received event: {eventName}, Data: {data}");
            });
        }

        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
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
}
