using System.Net.Http.Json;
using System.Text.Json;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// API客户端，用于与 ZakYip.Singulation.Host REST API 通信
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 获取所有控制器信息
    /// </summary>
    public async Task<ApiResponse<List<ControllerInfo>>> GetControllersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ControllerInfo>>>(
                "/api/controllers", _jsonOptions);
            return response ?? new ApiResponse<List<ControllerInfo>> 
            { 
                Success = false, 
                Message = "Response is null" 
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ControllerInfo>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 发送安全命令
    /// </summary>
    public async Task<ApiResponse<object>> SendSafetyCommandAsync(SafetyCommandRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/safety/commands", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                return result ?? new ApiResponse<object> 
                { 
                    Success = true, 
                    Message = "Command sent successfully" 
                };
            }
            return new ApiResponse<object>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}

/// <summary>
/// API响应包装器
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// 控制器信息
/// </summary>
public class ControllerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 安全命令请求
/// </summary>
public class SafetyCommandRequest
{
    public string CommandType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}
