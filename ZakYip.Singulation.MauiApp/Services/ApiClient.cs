using Newtonsoft.Json;
using System.Text;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// API客户端，用于与 ZakYip.Singulation.Host REST API 通信
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 获取所有控制器信息
    /// </summary>
    public async Task<ApiResponse<List<ControllerInfo>>> GetControllersAsync()
    {
        try
        {
            var responseMessage = await _httpClient.GetAsync("/api/axes/axes");
            var content = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ApiResponse<List<ControllerInfo>>>(content, _jsonSettings);
            return response ?? new ApiResponse<List<ControllerInfo>> 
            { 
                Result = false, 
                Msg = "Response is null" 
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ControllerInfo>>
            {
                Result = false,
                Msg = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取控制器状态
    /// </summary>
    public async Task<ApiResponse<ControllerStatus>> GetControllerStatusAsync()
    {
        try
        {
            var responseMessage = await _httpClient.GetAsync("/api/axes/controller");
            var content = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ApiResponse<ControllerStatus>>(content, _jsonSettings);
            return response ?? new ApiResponse<ControllerStatus> 
            { 
                Result = false, 
                Msg = "Response is null" 
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ControllerStatus>
            {
                Result = false,
                Msg = ex.Message
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
            var json = JsonConvert.SerializeObject(request, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/safety/commands", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> 
                { 
                    Result = true, 
                    Msg = "Command sent successfully" 
                };
            }
            return new ApiResponse<object>
            {
                Result = false,
                Msg = $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object>
            {
                Result = false,
                Msg = ex.Message
            };
        }
    }

    /// <summary>
    /// 使能指定轴
    /// </summary>
    public async Task<ApiResponse<object>> EnableAxesAsync(int[]? axisIds = null)
    {
        try
        {
            var query = axisIds != null && axisIds.Length > 0 
                ? $"?{string.Join("&", axisIds.Select(id => $"axisIds={id}"))}"
                : "";
            var response = await _httpClient.PostAsync($"/api/axes/axes/enable{query}", null);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Axes enabled" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 禁用指定轴
    /// </summary>
    public async Task<ApiResponse<object>> DisableAxesAsync(int[]? axisIds = null)
    {
        try
        {
            var query = axisIds != null && axisIds.Length > 0 
                ? $"?{string.Join("&", axisIds.Select(id => $"axisIds={id}"))}"
                : "";
            var response = await _httpClient.PostAsync($"/api/axes/axes/disable{query}", null);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Axes disabled" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 设置轴速度
    /// </summary>
    public async Task<ApiResponse<object>> SetAxesSpeedAsync(double speedMmps, int[]? axisIds = null)
    {
        try
        {
            var query = axisIds != null && axisIds.Length > 0 
                ? $"?{string.Join("&", axisIds.Select(id => $"axisIds={id}"))}"
                : "";
            var requestData = new { LinearMmps = speedMmps };
            var json = JsonConvert.SerializeObject(requestData, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/axes/axes/speed{query}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Speed set" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }
}

/// <summary>
/// API响应包装器 - 与服务器端 ApiResponse 保持一致
/// </summary>
public class ApiResponse<T>
{
    public bool Result { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T? Data { get; set; }
    
    // 便捷属性，与 Result 保持一致
    public bool Success => Result;
    public string Message => Msg;
}

/// <summary>
/// 轴信息 - 与服务器端 AxisResponseDto 保持一致
/// </summary>
public class ControllerInfo
{
    public string AxisId { get; set; } = string.Empty;
    public int Status { get; set; }  // DriverStatus 枚举
    public double? TargetLinearMmps { get; set; }
    public double? FeedbackLinearMmps { get; set; }
    public bool? Enabled { get; set; }
    public int? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    
    // 便捷显示属性
    public string Id => AxisId;
    public string Name => $"Axis {AxisId}";
    public string StatusText => Status switch
    {
        0 => "Offline",
        1 => "Initializing", 
        2 => "Ready",
        3 => "Running",
        4 => "Faulted",
        _ => "Unknown"
    };
}

/// <summary>
/// 安全命令请求 - 与服务器端 SafetyCommandRequestDto 保持一致
/// </summary>
public class SafetyCommandRequest
{
    public int Command { get; set; }  // SafetyCommand 枚举值: Start=1, Stop=2, Reset=3
    public string? Reason { get; set; }
}

/// <summary>
/// 控制器状态 - 与服务器端 ControllerResponseDto 保持一致
/// </summary>
public class ControllerStatus
{
    public int AxisCount { get; set; }
    public int ErrorCode { get; set; }
    public bool Initialized { get; set; }
}
