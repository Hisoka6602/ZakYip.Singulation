using System;
using System.Net.Http.Json;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.MauiApp.Services;

public sealed class ApiClient : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private string _baseAddress = "https://localhost:5001/";

    public ApiClient() {
        _httpClient = new HttpClient();
    }

    public string BaseAddress {
        get => _baseAddress;
        set => _baseAddress = NormalizeBaseAddress(value);
    }

    public async Task<ApiEnvelope<ControllerSummary>?> GetControllerAsync(CancellationToken ct = default) {
        var uri = BuildUri("api/axes/controller");
        return await _httpClient.GetFromJsonAsync<ApiEnvelope<ControllerSummary>>(uri, _serializerOptions, ct).ConfigureAwait(false);
    }

    public async Task<ApiEnvelope<object>?> SendSafetyCommandAsync(SafetyCommand command, string? reason, CancellationToken ct = default) {
        var uri = BuildUri("api/safety/commands");
        var payload = new { command, reason };
        var response = await _httpClient.PostAsJsonAsync(uri, payload, _serializerOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(_serializerOptions, ct).ConfigureAwait(false);
    }

    public async Task<ApiEnvelope<object>?> DeleteSessionAsync(CancellationToken ct = default) {
        var uri = BuildUri("api/system/session");
        var response = await _httpClient.DeleteAsync(uri, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(_serializerOptions, ct).ConfigureAwait(false);
    }

    private Uri BuildUri(string relative)
        => new($"{BaseAddress.TrimEnd('/')}/{relative.TrimStart('/')}", UriKind.Absolute);

    private static string NormalizeBaseAddress(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "https://localhost:5001/";
        }
        return value.EndsWith('/') ? value : value + "/";
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}

public sealed record ApiEnvelope<T>(bool Result, string Msg, T? Data);

public sealed record ControllerSummary(int AxisCount, int ErrorCode, bool Initialized);
