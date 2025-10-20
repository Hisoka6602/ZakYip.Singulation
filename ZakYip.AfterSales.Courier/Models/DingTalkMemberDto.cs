namespace ZakYip.AfterSales.Courier.Models;

/// <summary>
/// 钉钉成员信息 DTO
/// </summary>
public class DingTalkMemberDto
{
    /// <summary>
    /// 员工唯一标识ID（不可修改）
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 员工在当前企业内的唯一标识
    /// </summary>
    public string UnionId { get; set; } = string.Empty;

    /// <summary>
    /// 员工姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// 员工邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 员工状态：1-试用期，2-正式，3-实习期，5-待离职，-1-无状态
    /// </summary>
    public int StateCode { get; set; }

    /// <summary>
    /// 职位信息
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// 所属部门ID列表
    /// </summary>
    public List<long> DeptIdList { get; set; } = new();
}
