using Newtonsoft.Json;
using System.Text;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// 控制器相关 API 服务
/// </summary>
public class ControllerApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public ControllerApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 获取控制器状态
    /// </summary>
    public async Task<ApiResponse<ControllerStatus>> GetControllerStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/axes/controller");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<ControllerStatus>>(content, _jsonSettings);
            return result ?? new ApiResponse<ControllerStatus> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ControllerStatus> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 获取控制器选项
    /// </summary>
    public async Task<ApiResponse<ControllerOptions>> GetControllerOptionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/axes/controller/options");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<ControllerOptions>>(content, _jsonSettings);
            return result ?? new ApiResponse<ControllerOptions> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ControllerOptions> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 更新控制器选项
    /// </summary>
    public async Task<ApiResponse<object>> PutControllerOptionsAsync(ControllerOptions options)
    {
        try
        {
            var json = JsonConvert.SerializeObject(options, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/axes/controller/options", content);
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
    /// 复位控制器
    /// </summary>
    public async Task<ApiResponse<object>> ResetControllerAsync(string resetType = "soft")
    {
        try
        {
            var data = new { type = resetType };
            var json = JsonConvert.SerializeObject(data, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/axes/controller/reset", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Controller reset" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 获取控制器错误
    /// </summary>
    public async Task<ApiResponse<object>> GetControllerErrorsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/axes/controller/errors");
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
    /// 清除控制器错误
    /// </summary>
    public async Task<ApiResponse<object>> ClearControllerErrorsAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/axes/controller/errors");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Errors cleared" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }
}
