using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Abstractions.Safety {

    /// <summary>
    /// 上电顺序机：定义回零/对位/失败落 SAFE 的实现。
    /// </summary>
    public interface ICommissioningSequence {
        Task PowerOnAsync(CancellationToken ct);
        Task HomeAsync(CancellationToken ct);
        Task AlignAsync(CancellationToken ct);
        Task FailToSafeAsync(string reason, CancellationToken ct);
    }
}
