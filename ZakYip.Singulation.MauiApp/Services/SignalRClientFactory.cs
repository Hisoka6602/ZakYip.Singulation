using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace ZakYip.Singulation.MauiApp.Services;

public sealed class SignalRClientFactory : IAsyncDisposable {
    private HubConnection? _connection;

    public async Task<HubConnection> EnsureConnectionAsync(string baseAddress, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting }) {
            return _connection;
        }

        if (_connection is not null) {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        var hubUrl = $"{Normalize(baseAddress)}/hubs/realtime";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // 暂不启动连接，留给调用方在需要时调用 StartAsync。
        return _connection;
    }

    private static string Normalize(string baseAddress)
        => baseAddress.EndsWith('/') ? baseAddress.TrimEnd('/') : baseAddress;

    public async ValueTask DisposeAsync() {
        if (_connection is not null) {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
