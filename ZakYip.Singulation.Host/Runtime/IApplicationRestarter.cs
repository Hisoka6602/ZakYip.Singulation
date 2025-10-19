using System.Threading;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Host.Runtime {

    /// <summary>
    /// 应用重启调度器：隔离可能导致进程退出的逻辑，便于测试与扩展。
    /// </summary>
    public interface IApplicationRestarter {

        /// <summary>
        /// 调度一次重启流程（允许去重）。
        /// </summary>
        /// <param name="reason">触发原因描述。</param>
        /// <param name="ct">取消令牌。</param>
        Task RestartAsync(string reason, CancellationToken ct = default);
    }
}
