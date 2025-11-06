using Newtonsoft.Json;
using System.Text;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// 解码器 API 服务
/// </summary>
public class DecoderApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public DecoderApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 解码器健康检查
    /// </summary>
    public async Task<ApiResponse<object>> GetHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/decoder/health");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<object>>(content, _jsonSettings);
            return result ?? new ApiResponse<object> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 获取解码器配置
    /// </summary>
    public async Task<ApiResponse<DecoderOptions>> GetOptionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/decoder/options");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<DecoderOptions>>(content, _jsonSettings);
            return result ?? new ApiResponse<DecoderOptions> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<DecoderOptions> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 更新解码器配置
    /// </summary>
    public async Task<ApiResponse<object>> PutOptionsAsync(DecoderOptions options)
    {
        try
        {
            var json = JsonConvert.SerializeObject(options, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/decoder/options", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Options updated" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 提交帧数据进行解码
    /// </summary>
    public async Task<ApiResponse<object>> DecodeFrameAsync(string hex)
    {
        try
        {
            var data = new { hex };
            var json = JsonConvert.SerializeObject(data, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/decoder/frames", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Frame decoded" };
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
/// 上游通信 API 服务
/// </summary>
public class UpstreamApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public UpstreamApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 获取上游配置
    /// </summary>
    public async Task<ApiResponse<UpstreamConfiguration>> GetConfigurationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/upstream/configuration");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<UpstreamConfiguration>>(content, _jsonSettings);
            return result ?? new ApiResponse<UpstreamConfiguration> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UpstreamConfiguration> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 更新上游配置
    /// </summary>
    public async Task<ApiResponse<object>> PutConfigurationAsync(UpstreamConfiguration config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/upstream/configuration", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Configuration updated" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 获取上游连接状态
    /// </summary>
    public async Task<ApiResponse<UpstreamStatus>> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/upstream/status");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<UpstreamStatus>>(content, _jsonSettings);
            return result ?? new ApiResponse<UpstreamStatus> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UpstreamStatus> { Result = false, Msg = ex.Message };
        }
    }
}

/// <summary>
/// 系统会话 API 服务
/// </summary>
public class SystemApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public SystemApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 删除当前运行会话（触发宿主进程退出）
    /// </summary>
    public async Task<ApiResponse<object>> DeleteSessionAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/system/session");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Session deleted" };
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
/// 安全管线 API 服务
/// </summary>
public class SafetyApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public SafetyApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 发送安全命令
    /// </summary>
    public async Task<ApiResponse<object>> SendCommandAsync(CabinetCommandRequest request)
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
                return result ?? new ApiResponse<object> { Result = true, Msg = "Command sent" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }
}

// DTO 类

public class DecoderOptions
{
    public int MainCount { get; set; }
    public int EjectCount { get; set; }
}

public class UpstreamConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Enabled { get; set; }
}

public class UpstreamStatus
{
    public bool IsConnected { get; set; }
    public string ConnectionState { get; set; } = string.Empty;
    public DateTime? LastConnected { get; set; }
}
