using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Simulated {

    /// <summary>
    /// 模拟轴驱动（无硬件环境下的可替代实现）。
    /// <para>
    /// - 实现 IAxisDrive：写速、停机、心跳；<br/>
    /// - 背景循环按加速度限制把“当前RPM”逼近“目标RPM”；<br/>
    /// - 支持命令节流（遵守 <see cref="DriverOptions.CommandMinInterval"/>）；<br/>
    /// - 可注入循环频率、噪声与故障概率，便于联调/压测；<br/>
    /// - 不做任何真实 I/O，零依赖、零外设。
    /// </para>
    /// </summary>
    public sealed class SimAxisDrive : IAxisDrive {
        private readonly DriverOptions _opts;
        private readonly double _loopHz;
        private readonly double _noiseRpm;     // 随机抖动幅度（±）
        private readonly double _failProb;     // 触发“心跳失败/退化”的概率（0~1）
        private readonly Random _rng = new Random();

        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        private long _lastCmdTicks;
        private volatile DriverStatus _status = DriverStatus.Connected;

        // 目标/当前转速（RPM）
        private double _targetRpm;

        private double _currentRpm;

        public SimAxisDrive(AxisId axis, DriverOptions opts, double loopHz = 200, double noiseRpm = 0, double failProb = 0) {
            Axis = axis;
            _opts = opts ?? new DriverOptions();
            _loopHz = loopHz <= 0 ? 200 : loopHz;
            _noiseRpm = Math.Max(0, noiseRpm);
            _failProb = Math.Clamp(failProb, 0, 1);

            // 启动后台逼近循环
            _loop = Task.Run(LoopAsync);
        }

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        /// <summary>轴标识（只读）。</summary>
        public AxisId Axis { get; }

        /// <summary>当前模拟驱动状态。</summary>
        public DriverStatus Status => _status;

        /// <summary>
        /// 写入目标转速（RPM）。
        /// 执行步骤：
        /// 1) 命令节流：保证两次命令间隔 ≥ MinInterval；
        /// 2) 限幅：夹在 [-MaxRpm, +MaxRpm]；
        /// 3) 记录为“目标转速”，后台循环会按加速度限制逐步逼近。
        /// </summary>
        public async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            var max = Math.Abs(_opts.MaxRpm);
            var clamped = Math.Max(-max, Math.Min(max, rpm.Value));
            Volatile.Write(ref _targetRpm, clamped);
            _status = DriverStatus.Connected;
        }

        public ValueTask WriteSpeedAsync(double mmPerSec, CancellationToken ct = default) {
            return default;
        }

        public ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            return default;
        }

        public ValueTask SetAccelDecelAsync(double accelMmPerSec, double decelMmPerSec, CancellationToken ct = default) {
            return default;
        }

        /// <summary>
        /// 停止轴运动。
        /// 执行步骤：
        /// 1) 命令节流；
        /// 2) 将目标转速设置为 0；
        /// 3) 后台循环会在加速度限制下把当前转速减至 0。
        /// </summary>
        public async ValueTask StopAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);
            Volatile.Write(ref _targetRpm, 0d);
            _status = DriverStatus.Connected;
        }

        /// <summary>
        /// 心跳探测（模拟）。
        /// 执行步骤：
        /// 1) 按故障概率随机返回失败以模拟“退化/掉线”；
        /// 2) 正常情况下快速返回 true；
        /// 3) 一旦失败，将状态置为 Degraded（上层可据此告警）。
        /// </summary>
        public ValueTask<bool> PingAsync(CancellationToken ct = default) {
            if (_rng.NextDouble() < _failProb) {
                _status = DriverStatus.Degraded;
                return ValueTask.FromResult(false);
            }
            _status = DriverStatus.Connected;
            return ValueTask.FromResult(true);
        }

        /// <summary>
        /// 释放模拟器：结束后台循环并标记为离线。
        /// </summary>
        public async ValueTask DisposeAsync() {
            try {
                _cts.Cancel();
                await Task.WhenAny(_loop, Task.Delay(200));
            }
            finally {
                _status = DriverStatus.Disconnected;
                _cts.Dispose();
            }
        }

        // ================= 背景循环：按加速度限制逼近目标RPM =================

        /// <summary>
        /// 后台逼近循环。
        /// 步骤（每帧）：
        /// 1) 计算本帧时间步长 dt；
        /// 2) 求允许的最大速度变化量 = AccelRpmPerSec * dt；
        /// 3) 把当前转速向目标转速推进不超过该变化量；
        /// 4) 加入可选噪声（小随机数，便于观察/联调）；
        /// 5) 睡眠到下帧（基于 loopHz）。
        /// </summary>
        private async Task LoopAsync() {
            var sw = Stopwatch.StartNew();
            var last = sw.Elapsed;

            var frame = TimeSpan.FromSeconds(1 / _loopHz);
            var maxRpm = Math.Abs(_opts.MaxRpm);
            var accel = Math.Abs(_opts.MaxAccelRpmPerSec);

            try {
                while (!_cts.IsCancellationRequested) {
                    var now = sw.Elapsed;
                    var dt = (now - last).TotalSeconds;
                    if (dt <= 0) dt = 1.0 / _loopHz; // 兜底
                    last = now;

                    // 读取目标与当前
                    var target = Volatile.Read(ref _targetRpm);
                    var current = Volatile.Read(ref _currentRpm);

                    // 允许的最大变化量（加速度限制）
                    var maxDelta = accel * dt;
                    var diff = target - current;

                    if (Math.Abs(diff) <= maxDelta) {
                        current = target;
                    }
                    else {
                        current += Math.Sign(diff) * maxDelta;
                    }

                    // 限幅再写回
                    current = Math.Max(-maxRpm, Math.Min(maxRpm, current));

                    // 可选噪声（小幅抖动）
                    if (_noiseRpm > 0) {
                        current += (_rng.NextDouble() * 2 - 1) * _noiseRpm;
                    }

                    Volatile.Write(ref _currentRpm, current);

                    // 下一帧
                    try {
                        await Task.Delay(frame, _cts.Token);
                    }
                    catch (OperationCanceledException) { /* 正常退出 */ }
                }
            }
            catch {
                _status = DriverStatus.Degraded; // 异常视为退化
                throw;
            }
        }

        // ================= 命令节流（遵守 MinInterval） =================

        private async ValueTask ThrottleAsync(CancellationToken ct) {
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastCmdTicks);
            var minTicks = _opts.CommandMinInterval.Ticks;

            if (now - last < minTicks) {
                var wait = new TimeSpan(minTicks - (now - last));
                if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            }
            Interlocked.Exchange(ref _lastCmdTicks, DateTime.UtcNow.Ticks);
        }
    }
}