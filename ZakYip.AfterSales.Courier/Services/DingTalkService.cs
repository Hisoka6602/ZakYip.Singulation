using System.Text;
using System.Text.Json;
using ZakYip.AfterSales.Courier.Models;

namespace ZakYip.AfterSales.Courier.Services;

/// <summary>
/// 钉钉 API 服务
/// </summary>
public class DingTalkService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://oapi.dingtalk.com";

    public DingTalkService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <summary>
    /// 获取组织成员列表（支持无部门场景）
    /// API Code: qyapi_get_member
    /// </summary>
    /// <param name="request">请求参数</param>
    /// <returns>成员列表响应</returns>
    public async Task<GetMemberResponseDto> GetMemberListAsync(GetMemberRequestDto request)
    {
        try
        {
            // 构建请求URL
            var url = $"/topapi/v2/user/list?access_token={request.AccessToken}";
            
            // 构建请求体
            var requestBody = new Dictionary<string, object>
            {
                { "cursor", request.Offset },
                { "size", request.Size }
            };

            // 如果指定了部门ID，则添加到请求中
            if (request.DeptId.HasValue)
            {
                requestBody["dept_id"] = request.DeptId.Value;
            }

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // 发送请求
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 解析响应
            var result = JsonSerializer.Deserialize<GetMemberResponseDto>(responseContent, 
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

            return result ?? new GetMemberResponseDto
            {
                ErrCode = -1,
                ErrMsg = "响应解析失败"
            };
        }
        catch (Exception ex)
        {
            return new GetMemberResponseDto
            {
                ErrCode = -1,
                ErrMsg = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取所有组织成员（自动分页获取）
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="deptId">部门ID，可选</param>
    /// <returns>所有成员列表</returns>
    public async Task<List<DingTalkMemberDto>> GetAllMembersAsync(string accessToken, long? deptId = null)
    {
        var allMembers = new List<DingTalkMemberDto>();
        var offset = 0;
        var size = 100;
        bool hasMore = true;

        while (hasMore)
        {
            var request = new GetMemberRequestDto
            {
                AccessToken = accessToken,
                DeptId = deptId,
                Offset = offset,
                Size = size
            };

            var response = await GetMemberListAsync(request);

            if (response.ErrCode == 0)
            {
                allMembers.AddRange(response.UserList);
                hasMore = response.HasMore;
                offset += size;
            }
            else
            {
                // 如果出错，终止循环
                break;
            }
        }

        return allMembers;
    }
}
