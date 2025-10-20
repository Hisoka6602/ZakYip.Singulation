namespace ZakYip.AfterSales.Courier.Models;

/// <summary>
/// 钉钉获取成员列表请求 DTO
/// </summary>
public class GetMemberRequestDto : DingTalkRequestDto
{
    /// <summary>
    /// 部门ID，可选参数。如果不传入，则获取全部成员
    /// </summary>
    public long? DeptId { get; set; }

    /// <summary>
    /// 偏移量，从0开始
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// 每页大小，最大100
    /// </summary>
    public int Size { get; set; } = 100;
}
