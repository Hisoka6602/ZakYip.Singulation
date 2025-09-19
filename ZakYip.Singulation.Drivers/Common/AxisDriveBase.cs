using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions.Ports;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// 轴驱动的通用基类（实现 <see cref="IAxisDrive"/>）。
    /// <para>
    /// 负责：命令节流、指数退避重试、状态更新、零拷贝解析的骨架。
    /// 子类只需实现协议相关的帧构建与解析方法（Build*/Parse*）。
    /// </para>
    /// </summary>
    public abstract class AxisDriveBase : IAxisDrive, IAsyncDisposable {

        /// <summary>轴标识（对外通过 <see cref="Axis"/> 暴露）。</summary>
        protected readonly AxisId _axisId;

        /// <summary>底层端口（TCP/SDK 等的抽象）。</summary>
        protected readonly IAxisPort Port;

        /// <summary>驱动运行参数（速率限制、退避策略等）。</summary>
        protected readonly DriverOptions Opts;

        private volatile DriverStatus _status = DriverStatus.Disconnected;
        private long _lastCommandTicks;

        /// <summary>
        /// 构造函数：注入轴ID、端口与驱动参数。
        /// </summary>
        /// <param name="axisId">轴ID。</param>
        /// <param name="port">发送/收包端口。</param>
        /// <param name="opts">驱动选项（为空时使用默认）。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="port"/> 为 null。</exception>
        protected AxisDriveBase(AxisId axisId, IAxisPort port, DriverOptions opts) {
            _axisId = axisId;
            Axis = axisId;                               // 1) 绑定对外只读属性
            Port = port ?? throw new ArgumentNullException(nameof(port)); // 2) 端口注入
            Opts = opts;          // 3) 选项兜底
        }

        /// <summary>轴ID（只读）。</summary>
        public AxisId Axis { get; }

        /// <summary>当前驱动状态（离线/恢复中/在线/已释放等）。</summary>
        public DriverStatus Status => _status;

        /// <summary>
        /// （可选）显式连接/准备；默认仅更新状态为 Connected。
        /// <para>子类可重写以执行上电/清错/握手等步骤。</para>
        /// </summary>
        public virtual async ValueTask ConnectAsync(CancellationToken ct = default) {
            _status = DriverStatus.Connected;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 使能驱动（协议帧由子类提供）。
        /// <list type="number">
        /// <item>构建“Enable”帧</item>
        /// <item>发送并带重试与退避</item>
        /// <item>更新状态：成功置 Connected；重试置 Recovering；失败置 Disconnected</item>
        /// </list>
        /// </summary>
        public virtual async ValueTask EnableAsync(CancellationToken ct = default)
            => await SendGuardedAsync(BuildEnableCommand(), ct);

        /// <summary>
        /// 禁用驱动（协议帧由子类提供）。
        /// 执行步骤同 <see cref="EnableAsync"/>.
        /// </summary>
        public virtual async ValueTask DisableAsync(CancellationToken ct = default)
            => await SendGuardedAsync(BuildDisableCommand(), ct);

        /// <summary>
        /// 设置轴转速（对外契约上的写速接口）。
        /// <list type="number">
        /// <item>检查/准备：必要时可在子类中做同值去重</item>
        /// <item>构建“写RPM”帧</item>
        /// <item>发送（节流 + 重试 + 退避）</item>
        /// </list>
        /// </summary>
        /// <param name="rpm">目标转速。</param>
        public virtual async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default)
            => await SetRpmAsync(rpm, ct);

        public ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            return default;
        }

        /// <summary>
        /// 紧急停止。
        /// <list type="number">
        /// <item>构建“Stop”帧</item>
        /// <item>发送（节流 + 重试 + 退避）</item>
        /// </list>
        /// </summary>
        public virtual async ValueTask StopAsync(CancellationToken ct = default)
            => await SendGuardedAsync(BuildStopCommand(), ct);

        /// <summary>
        /// 存活探测：通过一次读状态往返判断通信是否正常。
        /// <list type="number">
        /// <item>构建“ReadStatus”帧</item>
        /// <item>请求-应答（节流 + 重试 + 退避）</item>
        /// <item>解析成功返回 true；异常返回 false</item>
        /// </list>
        /// </summary>
        public virtual async ValueTask<bool> PingAsync(CancellationToken ct = default) {
            try {
                _ = await RequestGuardedAsync(BuildReadStatusCommand(), ParseStatusResponse, ct);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// 回零（如有需要，子类可覆盖实际实现；默认直接下发 Home 帧）。
        /// </summary>
        public virtual async ValueTask HomeAsync(CancellationToken ct = default)
            => await SendGuardedAsync(BuildHomeCommand(), ct);

        /// <summary>
        /// 下发写RPM命令（实现细节层面的写速；对外请用 <see cref="WriteSpeedAsync"/>）。
        /// </summary>
        public virtual async ValueTask SetRpmAsync(AxisRpm rpm, CancellationToken ct = default)
            => await SendGuardedAsync(BuildSetRpmCommand(rpm), ct);

        /// <summary>
        /// 读取状态（返回 Planner 状态与报警码）。
        /// <list type="number">
        /// <item>构建“ReadStatus”帧</item>
        /// <item>请求-应答并拿到响应</item>
        /// <item>解析响应得到 (status, alarm)</item>
        /// </list>
        /// </summary>
        public virtual async ValueTask<(PlannerStatus status, int alarm)> ReadStatusAsync(CancellationToken ct = default)
            => await RequestGuardedAsync(BuildReadStatusCommand(), ParseStatusResponse, ct);

        // —— 子类需要提供的协议帧 / 解析器 —— //

        /// <summary>构建“使能”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildEnableCommand();

        /// <summary>构建“禁用”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildDisableCommand();

        /// <summary>构建“停止”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildStopCommand();

        /// <summary>构建“回零”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildHomeCommand();

        /// <summary>构建“写 RPM”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildSetRpmCommand(AxisRpm rpm);

        /// <summary>构建“读状态”协议帧。</summary>
        protected abstract ReadOnlyMemory<byte> BuildReadStatusCommand();

        /// <summary>
        /// 解析“读状态”响应。
        /// </summary>
        /// <param name="rsp">响应的有效字节切片（零拷贝）。</param>
        /// <returns>规划器状态与报警码。</returns>
        protected abstract (PlannerStatus status, int alarm) ParseStatusResponse(ReadOnlySpan<byte> rsp);

        /// <summary>
        /// 命令节流：确保两次下发间隔 ≥ <see cref="DriverOptions.CommandMinInterval"/>。
        /// <list type="number">
        /// <item>取当前tick与上次tick差</item>
        /// <item>若未到最小间隔则睡眠剩余时长</item>
        /// <item>更新“上一次下发时间”</item>
        /// </list>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Throttle() {
            // 1) 计算距离上次发送的间隔
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastCommandTicks);
            var minTicks = Opts.CommandMinInterval.Ticks;

            // 2) 未达最小间隔则等待
            if (now - last < minTicks) {
                var wait = new TimeSpan(minTicks - (now - last));
                if (wait > TimeSpan.Zero) Thread.Sleep(wait);
            }

            // 3) 记录本次发送时间
            Interlocked.Exchange(ref _lastCommandTicks, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// 带节流与指数退避的“发送”封装（无响应型命令）。
        /// <list type="number">
        /// <item>节流：<see cref="Throttle"/></item>
        /// <item>进入重试循环（最多 <see cref="DriverOptions.MaxRetries"/> 次）</item>
        /// <item>尝试发送：成功→状态置 Connected 并返回</item>
        /// <item>失败→状态置 Recovering，延迟并指数退避（封顶 <see cref="DriverOptions.MaxBackoff"/>）</item>
        /// <item>最终失败→状态置 Disconnected 并抛出</item>
        /// </list>
        /// </summary>
        private async ValueTask SendGuardedAsync(ReadOnlyMemory<byte> payload, CancellationToken ct) {
            // 1) 速率限制
            Throttle();

            var delay = TimeSpan.FromMilliseconds(50);

            // 2) 重试循环
            for (var attempt = 1; ; attempt++) {
                try {
                    // 3) 下发命令
                    await Port.SendAsync(payload, ct);

                    // 4) 成功：在线
                    _status = DriverStatus.Connected;
                    return;
                }
                catch when (attempt <= Opts.MaxRetries) {
                    // 5) 可重试：置为恢复中，指数退避
                    _status = DriverStatus.Recovering;
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, Opts.MaxBackoff.TotalMilliseconds));
                }
                catch {
                    // 6) 不可重试或重试用尽：置为离线并上抛
                    _status = DriverStatus.Disconnected;
                    throw;
                }
            }
        }

        /// <summary>
        /// 带节流与指数退避的“请求-应答”封装（有响应型命令）。
        /// <list type="number">
        /// <item>节流：<see cref="Throttle"/></item>
        /// <item>准备小缓冲区（避免大分配）</item>
        /// <item>进入重试循环</item>
        /// <item>发送请求并接收响应</item>
        /// <item>成功：状态置 Connected，调用解析器并返回结果</item>
        /// <item>失败：状态置 Recovering，指数退避；最终失败置 Disconnected 并抛出</item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">解析后的结果类型。</typeparam>
        /// <param name="request">请求帧。</param>
        /// <param name="parser">零拷贝解析器：<c>ReadOnlySpan&lt;byte&gt; -&gt; T</c>。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask<T> RequestGuardedAsync<T>(
            ReadOnlyMemory<byte> request,
            SpanParser<T> parser,
            CancellationToken ct) {
            // 1) 速率限制
            Throttle();

            // 2) 准备小缓冲（必要时可换池化：ArrayPool.Shared）
            var delay = TimeSpan.FromMilliseconds(50);
            var buffer = new byte[64];

            // 3) 重试循环
            for (var attempt = 1; ; attempt++) {
                try {
                    // 4) 请求-应答：端口一次性完成发送与接收
                    var len = await Port.RequestAsync(request, buffer, ct);

                    // 5) 成功：更新状态并解析
                    _status = DriverStatus.Connected;
                    return parser(buffer.AsSpan(0, len));
                }
                catch when (attempt <= Opts.MaxRetries) {
                    // 6) 可重试：置恢复中 + 指数退避
                    _status = DriverStatus.Recovering;
                    await Task.Delay(delay, ct);
                    var nextMs = Math.Min(delay.TotalMilliseconds * 2, Opts.MaxBackoff.TotalMilliseconds);
                    delay = TimeSpan.FromMilliseconds(nextMs);
                }
                catch {
                    // 7) 失败：置离线并上抛
                    _status = DriverStatus.Disconnected;
                    throw;
                }
            }
        }

        /// <summary>
        /// 释放资源：标记状态为 Disposed 并释放端口。
        /// </summary>
        public virtual async ValueTask DisposeAsync() {
            _status = DriverStatus.Disposed;
            await Port.DisposeAsync();
        }
    }
}