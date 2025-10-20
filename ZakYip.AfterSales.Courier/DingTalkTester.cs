using ZakYip.AfterSales.Courier.Models;
using ZakYip.AfterSales.Courier.Services;

namespace ZakYip.AfterSales.Courier;

/// <summary>
/// 钉钉功能测试器
/// </summary>
public class DingTalkTester
{
    private readonly DingTalkService _dingTalkService;

    public DingTalkTester()
    {
        _dingTalkService = new DingTalkService(new HttpClient());
    }

    public DingTalkTester(DingTalkService dingTalkService)
    {
        _dingTalkService = dingTalkService;
    }

    /// <summary>
    /// 测试获取成员列表（无部门）
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>测试结果</returns>
    public async Task<TestResult> TestGetMemberWithoutDeptAsync(string accessToken)
    {
        try
        {
            Console.WriteLine("=== 测试: 获取成员列表（无部门） ===");
            Console.WriteLine($"访问令牌: {MaskToken(accessToken)}");

            var request = new GetMemberRequestDto
            {
                AccessToken = accessToken,
                DeptId = null, // 不指定部门ID
                Offset = 0,
                Size = 10
            };

            var response = await _dingTalkService.GetMemberListAsync(request);

            if (response.ErrCode == 0)
            {
                Console.WriteLine($"✓ 成功获取 {response.UserList.Count} 个成员");
                Console.WriteLine($"是否有更多数据: {response.HasMore}");
                
                foreach (var member in response.UserList)
                {
                    Console.WriteLine($"  - {member.Name} ({member.UserId}) - {member.Position}");
                }

                return new TestResult
                {
                    Success = true,
                    Message = $"成功获取 {response.UserList.Count} 个成员",
                    Data = response
                };
            }
            else
            {
                Console.WriteLine($"✗ 请求失败: {response.ErrMsg} (Code: {response.ErrCode})");
                return new TestResult
                {
                    Success = false,
                    Message = $"错误: {response.ErrMsg}",
                    ErrorCode = response.ErrCode
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 异常: {ex.Message}");
            return new TestResult
            {
                Success = false,
                Message = $"异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 测试获取指定部门的成员列表
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="deptId">部门ID</param>
    /// <returns>测试结果</returns>
    public async Task<TestResult> TestGetMemberWithDeptAsync(string accessToken, long deptId)
    {
        try
        {
            Console.WriteLine($"=== 测试: 获取部门成员列表（部门ID: {deptId}） ===");
            Console.WriteLine($"访问令牌: {MaskToken(accessToken)}");

            var request = new GetMemberRequestDto
            {
                AccessToken = accessToken,
                DeptId = deptId,
                Offset = 0,
                Size = 10
            };

            var response = await _dingTalkService.GetMemberListAsync(request);

            if (response.ErrCode == 0)
            {
                Console.WriteLine($"✓ 成功获取 {response.UserList.Count} 个成员");
                Console.WriteLine($"是否有更多数据: {response.HasMore}");
                
                foreach (var member in response.UserList)
                {
                    Console.WriteLine($"  - {member.Name} ({member.UserId}) - {member.Position}");
                }

                return new TestResult
                {
                    Success = true,
                    Message = $"成功获取 {response.UserList.Count} 个成员",
                    Data = response
                };
            }
            else
            {
                Console.WriteLine($"✗ 请求失败: {response.ErrMsg} (Code: {response.ErrCode})");
                return new TestResult
                {
                    Success = false,
                    Message = $"错误: {response.ErrMsg}",
                    ErrorCode = response.ErrCode
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 异常: {ex.Message}");
            return new TestResult
            {
                Success = false,
                Message = $"异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 测试获取所有成员（自动分页）
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>测试结果</returns>
    public async Task<TestResult> TestGetAllMembersAsync(string accessToken)
    {
        try
        {
            Console.WriteLine("=== 测试: 获取所有成员（自动分页） ===");
            Console.WriteLine($"访问令牌: {MaskToken(accessToken)}");

            var members = await _dingTalkService.GetAllMembersAsync(accessToken);

            Console.WriteLine($"✓ 成功获取总共 {members.Count} 个成员");
            
            if (members.Count > 0)
            {
                Console.WriteLine("前10个成员:");
                foreach (var member in members.Take(10))
                {
                    Console.WriteLine($"  - {member.Name} ({member.UserId}) - {member.Position}");
                }
            }

            return new TestResult
            {
                Success = true,
                Message = $"成功获取总共 {members.Count} 个成员",
                Data = members
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 异常: {ex.Message}");
            return new TestResult
            {
                Success = false,
                Message = $"异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="testDeptId">测试用的部门ID（可选）</param>
    public async Task RunAllTestsAsync(string accessToken, long? testDeptId = null)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("钉钉 API 测试套件");
        Console.WriteLine("========================================\n");

        // 测试1: 获取成员列表（无部门）
        var result1 = await TestGetMemberWithoutDeptAsync(accessToken);
        Console.WriteLine();

        // 测试2: 获取指定部门的成员列表（如果提供了部门ID）
        if (testDeptId.HasValue)
        {
            var result2 = await TestGetMemberWithDeptAsync(accessToken, testDeptId.Value);
            Console.WriteLine();
        }

        // 测试3: 获取所有成员
        var result3 = await TestGetAllMembersAsync(accessToken);
        Console.WriteLine();

        Console.WriteLine("========================================");
        Console.WriteLine("测试完成");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 屏蔽令牌信息（安全考虑）
    /// </summary>
    private string MaskToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 10)
        {
            return "***";
        }
        return $"{token.Substring(0, 5)}...{token.Substring(token.Length - 5)}";
    }
}
