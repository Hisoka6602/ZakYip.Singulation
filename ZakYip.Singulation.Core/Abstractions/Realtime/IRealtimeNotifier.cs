using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Abstractions.Realtime {

    /// <summary>
    /// 跨层实时通知抽象：领域/驱动层只依赖这个接口，不关心底层是否用 SignalR。
    /// </summary>
    public interface IRealtimeNotifier {

        /// <summary>广播事件到指定通道（例如 group/topic）。非阻塞，内部自行排队。</summary>
        ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default);

        /// <summary>简化版：广播系统级事件（如“/sys”）。</summary>
        ValueTask PublishAsync(object payload, CancellationToken ct = default)
            => PublishAsync("/sys", payload, ct);

        /// <summary>
        /// 广播设备相关事件（默认频道：/device/{id}）。
        /// </summary>
        ValueTask PublishDeviceAsync(object payload, CancellationToken ct = default)
            => PublishAsync($"/device", payload, ct);

        /// <summary>
        /// 广播视觉相关事件（默认频道：/vision）。
        /// </summary>
        ValueTask PublishVisionAsync(object payload, CancellationToken ct = default)
            => PublishAsync("/vision", payload, ct);

        /// <summary>
        /// 广播异常相关事件（默认频道：/errors）。
        /// </summary>
        ValueTask PublishErrorAsync(object payload, CancellationToken ct = default)
            => PublishAsync("/errors", payload, ct);
    }
}