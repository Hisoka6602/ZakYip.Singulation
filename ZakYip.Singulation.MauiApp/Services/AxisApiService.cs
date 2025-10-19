using Newtonsoft.Json;
using System.Text;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// 轴管理 API 服务
/// </summary>
public class AxisApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _jsonSettings;

    public AxisApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    /// <summary>
    /// 获取所有轴信息
    /// </summary>
    public async Task<ApiResponse<List<AxisInfo>>> GetAllAxesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/axes/axes");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<List<AxisInfo>>>(content, _jsonSettings);
            return result ?? new ApiResponse<List<AxisInfo>> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AxisInfo>> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 获取指定轴信息
    /// </summary>
    public async Task<ApiResponse<AxisInfo>> GetAxisAsync(string axisId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/axes/axes/{axisId}");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<AxisInfo>>(content, _jsonSettings);
            return result ?? new ApiResponse<AxisInfo> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AxisInfo> { Result = false, Msg = ex.Message };
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

    /// <summary>
    /// 获取轴拓扑布局
    /// </summary>
    public async Task<ApiResponse<AxisTopology>> GetTopologyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/axes/topology");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<AxisTopology>>(content, _jsonSettings);
            return result ?? new ApiResponse<AxisTopology> { Result = false, Msg = "Response is null" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AxisTopology> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 更新轴拓扑布局
    /// </summary>
    public async Task<ApiResponse<object>> PutTopologyAsync(AxisTopology topology)
    {
        try
        {
            var json = JsonConvert.SerializeObject(topology, _jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/axes/topology", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Topology updated" };
            }
            return new ApiResponse<object> { Result = false, Msg = $"HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Result = false, Msg = ex.Message };
        }
    }

    /// <summary>
    /// 删除轴拓扑布局
    /// </summary>
    public async Task<ApiResponse<object>> DeleteTopologyAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/axes/topology");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent, _jsonSettings);
                return result ?? new ApiResponse<object> { Result = true, Msg = "Topology deleted" };
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
/// 轴拓扑布局 DTO
/// </summary>
public class AxisTopology
{
    public int Rows { get; set; }
    public int Cols { get; set; }
    public List<AxisPlacement> Placements { get; set; } = new();
}

public class AxisPlacement
{
    public int Row { get; set; }
    public int Col { get; set; }
    public string AxisId { get; set; } = string.Empty;
}
