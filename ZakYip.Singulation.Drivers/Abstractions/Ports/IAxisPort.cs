using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Abstractions.Ports {

    public interface IAxisPort : IAsyncDisposable {

        /// <summary>
        /// 发送单向指令（无返回），常用于设定速度/使能等
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default);

        /// <summary>
        /// 请求-应答（有返回），常用于读取状态/报警码等
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseBuffer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<int> RequestAsync(
            ReadOnlyMemory<byte> request,
            Memory<byte> responseBuffer,
            CancellationToken ct = default);
    }
}