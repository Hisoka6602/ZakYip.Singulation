using System.Threading.Channels;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// 轴指令队列（内部辅助类）：
    /// <para>
    /// - 单工作线程按序执行指令，确保设备侧的有序性；<br/>
    /// - 高频 <c>WriteSpeed</c> 做 <b>最后值胜出</b> 的合并（不挤爆队列）；<br/>
    /// - <c>Stop</c> 等紧急指令走优先通道，尽快到达；<br/>
    /// - 支持有界队列与背压/丢弃策略；<br/>
    /// - 仅调度与合并，不关心协议细节，真正下发由外部委托完成。
    /// </para>
    /// </summary>
    internal sealed class AxisCommandQueue : IAsyncDisposable {

        // —— 对外执行委托（由具体驱动提供，真正与设备通信） ——
        private readonly Func<AxisRpm, CancellationToken, ValueTask> _onWriteSpeed;

        private readonly Func<CancellationToken, ValueTask> _onStop;

        // —— 两级通道：紧急（Stop）优先，普通（WriteSpeed）次之 ——
        private readonly Channel<AxisCommand> _urgent;  // 小容量，优先消费

        private readonly Channel<AxisCommand> _normal;  // 有界容量

        // —— 写速合并槽：只保留“最后一次”的 RPM ——
        private AxisRpm _coalescedRpm;       // 最新待下发的 RPM

        private int _speedDirty;             // 0=无未送达的写速标记；1=有（用于控制是否写入一个占位命令）

        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;

        /// <summary>
        /// 创建轴指令队列。
        /// </summary>
        /// <param name="onWriteSpeed">真正执行写速的回调。</param>
        /// <param name="onStop">真正执行停止的回调。</param>
        /// <param name="normalCapacity">普通队列容量（写速占位命令所用）。</param>
        /// <param name="urgentCapacity">紧急队列容量（Stop 用）。</param>
        public AxisCommandQueue(
            Func<AxisRpm, CancellationToken, ValueTask> onWriteSpeed,
            Func<CancellationToken, ValueTask> onStop,
            int normalCapacity = 32,
            int urgentCapacity = 8) {
            _onWriteSpeed = onWriteSpeed ?? throw new ArgumentNullException(nameof(onWriteSpeed));
            _onStop = onStop ?? throw new ArgumentNullException(nameof(onStop));

            // 1) 建立有界通道，单读多写，降低锁竞争
            _normal = Channel.CreateBounded<AxisCommand>(new BoundedChannelOptions(normalCapacity) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest // 防止极端情况下堆积（仅对普通通道生效）
            });

            _urgent = Channel.CreateBounded<AxisCommand>(new BoundedChannelOptions(urgentCapacity) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            // 2) 启动单消费者工作循环
            _worker = Task.Run(WorkerLoopAsync);
        }

        /// <summary>
        /// 入队“写速”命令（合并高频写入：最后值胜出）。
        /// <para>
        /// 执行步骤：<br/>
        /// 1) 更新合并槽中的最新 RPM；<br/>
        /// 2) 若当前没有待处理的写速占位，则设置标记并投递一个占位命令；<br/>
        /// 3) 若已有占位在队列中，则仅更新 RPM，不再重复投递，避免队列风暴。
        /// </para>
        /// </summary>
        public void EnqueueWriteSpeed(AxisRpm rpm) {
            // 1) 写入最新 RPM（最新覆盖旧值）
            _coalescedRpm = rpm;

            // 2) 若此前无占位（0→1），投递一个占位命令（Kind=WriteSpeed）
            if (Interlocked.Exchange(ref _speedDirty, 1) == 0) {
                _ = _normal.Writer.TryWrite(AxisCommand.WriteSpeed());
                // 若满了也无妨；DropOldest 会丢弃更旧的普通命令，写速只要保证“至少一个占位在路上”即可
            }
        }

        /// <summary>
        /// 入队“停止”命令（紧急通道）。
        /// <para>可与当前写速占位并存，消费时优先级更高。</para>
        /// </summary>
        public void EnqueueStop() {
            // 停止是高优先级：尽量写入紧急通道
            _ = _urgent.Writer.TryWrite(AxisCommand.Stop());
        }

        /// <summary>
        /// 等待队列清空（普通与紧急都无待处理，且写速标记已清）。
        /// </summary>
        public async ValueTask FlushAsync(CancellationToken ct = default) {
            // 轮询等待：两通道皆空且无待写速标记
            while ((_urgent.Reader.Count > 0) ||
                   (_normal.Reader.Count > 0) ||
                   (Volatile.Read(ref _speedDirty) == 1)) {
                await Task.Delay(5, ct);
            }
        }

        /// <summary>
        /// 优雅关闭：停止工作循环并释放资源。
        /// </summary>
        public async ValueTask DisposeAsync() {
            try {
                _cts.Cancel();
                // 关闭写端以唤醒读方
                _urgent.Writer.TryComplete();
                _normal.Writer.TryComplete();
                await Task.WhenAny(_worker, Task.Delay(200)); // 给工作线程一个短窗口收尾
            }
            finally {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// 单消费者工作循环。
        /// <para>
        /// 取数顺序：先尝试紧急 → 再普通；<br/>
        /// <b>WriteSpeed</b>：读取最新合并的 RPM，清理脏标记，再执行回调；<br/>
        /// <b>Stop</b>：立即执行回调。
        /// </para>
        /// </summary>
        private async Task WorkerLoopAsync() {
            var ct = _cts.Token;

            try {
                while (!ct.IsCancellationRequested) {
                    // 1) 先处理紧急命令（尽量 non-blocking 提速）
                    if (_urgent.Reader.TryRead(out var urgentCmd)) {
                        if (urgentCmd.Kind == AxisCommandKind.Stop)
                            await _onStop(ct); // 执行停止
                        continue;
                    }

                    // 2) 再处理普通命令（若无可读则阻塞等待读）
                    AxisCommand cmd;
                    try {
                        cmd = await _normal.Reader.ReadAsync(ct);
                    }
                    catch (ChannelClosedException) {
                        break;
                    }

                    if (cmd.Kind == AxisCommandKind.WriteSpeed) {
                        // 3) 写速占位命令：抓取“最新 RPM”，并清理标记
                        //    执行步骤：
                        //    (1) 先清标记（允许生产者再次投递新的占位）；
                        //    (2) 读取当前最新 RPM，并下发；
                        Interlocked.Exchange(ref _speedDirty, 0);
                        var rpm = _coalescedRpm;
                        await _onWriteSpeed(rpm, ct);
                    }
                }
            }
            catch (OperationCanceledException) {
                // 正常关闭
            }
            catch (Exception) {
                // 这里可加日志：队列异常退出
                // TODO: 日志/告警
            }
        }

        /// <summary>命令种类。</summary>
        private enum AxisCommandKind : byte { WriteSpeed = 1, Stop = 2 }

        /// <summary>
        /// 队列中的轻量命令占位（无负载；写速负载由合并槽提供）。
        /// </summary>
        private readonly struct AxisCommand {
            public AxisCommandKind Kind { get; }

            private AxisCommand(AxisCommandKind kind) => Kind = kind;

            public static AxisCommand WriteSpeed() => new(AxisCommandKind.WriteSpeed);

            public static AxisCommand Stop() => new(AxisCommandKind.Stop);
        }
    }
}