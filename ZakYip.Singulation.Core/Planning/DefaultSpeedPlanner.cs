using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Planning {

    /// <summary>
    /// 默认速度规划器：
    /// 输入：拓扑 + 上游速度帧（SpeedSet，单位 m/s 或 RPM）
    /// 输出：与拓扑轴顺序一致的 RPM 序列（AxisRpm[]）
    /// 处理：单位换算 → 限幅 → 平滑 → 斜率限制；并做缺帧/乱序降级。
    /// </summary>
    /// <remarks>
    /// 低分配实现要点：
    /// - 复用输出缓存与平滑缓冲；下一次 Plan 会覆盖上一次输出；
    /// - 运行时参数采用 Volatile 写入，保证在线调参原子可见；
    /// - 采用简单滑动平均 + 线性斜率限制（可后续升级到 S 曲线）。
    /// </remarks>
    public sealed class DefaultSpeedPlanner : ISpeedPlanner, IDisposable {
        private readonly PlannerConfig _cfg;

        // 运行参数，支持在线调参（使用 Volatile 确保原子可见性）
        private PlannerParams _params;

        // 状态
        private volatile PlannerStatus _status = PlannerStatus.Idle;

        /// <summary>当前规划器运行状态。</summary>
        public PlannerStatus Status => _status;

        // 历史与平滑缓冲（提供默认初始化，防止极端情况下空引用）
        private decimal[] _lastRpm = [];          // 上次输出

        private decimal[] _smoothSum = [];          // 滑窗和值
        private decimal[,] _smoothBuf = new decimal[0, 0]; // [axis, k] 环形缓冲
        private int[] _smoothCount = [];          // 已填充量
        private int[] _smoothIndex = [];          // 写指针

        // 输出缓存（避免重复分配；注意：下一次 Plan 会覆盖）
        private AxisRpm[] _out = [];

        // 帧序跟踪
        private long _lastFrameSeq = -1;

        /// <summary>
        /// 构造规划器并分配所需缓冲。
        /// </summary>
        /// <param name="cfg">硬件/物理参数配置（轴数、直径、齿比、速度上限等）。</param>
        /// <param name="initialParams">运行时调参（限幅、平滑窗、斜率、缺帧策略等）。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="cfg"/> 或 <paramref name="initialParams"/> 为空时抛出。</exception>
        /// <exception cref="ArgumentException">当 <see cref="PlannerConfig.AxisCount"/> 非正数时抛出。</exception>
        public DefaultSpeedPlanner(PlannerConfig cfg, PlannerParams initialParams) {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _params = initialParams ?? throw new ArgumentNullException(nameof(initialParams));

            if (_cfg.AxisCount <= 0)
                throw new ArgumentException("AxisCount must be positive.", nameof(cfg));

            AllocateBuffers(_cfg.AxisCount, Math.Max(1, _params.SmoothWindow));
        }

        /// <summary>
        /// 在线更新规划器运行参数（原子可见）。
        /// 若平滑窗口大小发生变化，将重建内部平滑缓冲并重置状态为 Idle。
        /// </summary>
        /// <param name="params">新的运行参数（限幅、平滑窗口、最大加速度、缺帧策略、采样周期等）。</param>
        public void Configure(PlannerParams @params) {
            // 原子更新运行参数
            Volatile.Write(ref _params, @params);

            // 若滑窗大小变化，需要重建平滑缓冲（整体重置以保证线程安全与一致性）
            var newWin = Math.Max(1, @params.SmoothWindow);
            if (_smoothBuf.GetLength(1) != newWin) {
                AllocateBuffers(_cfg.AxisCount, newWin);
                // 置 Idle 并重置帧序，避免窗口切换瞬间的抖动与误判
                _status = PlannerStatus.Idle;
                _lastFrameSeq = -1;
            }
        }

        /// <summary>
        /// 执行一次速度规划：将输入速度（m/s 或 RPM）转换为各轴目标 RPM，并进行限幅/平滑/斜率限制。
        /// </summary>
        /// <param name="topology">输送机拓扑（轴集合与顺序，输出结果顺序与其一致）。</param>
        /// <param name="input">上游速度集合（含帧序号/时间戳/单位/来源，以及每段速度）。</param>
        /// <returns>与 <paramref name="topology"/> 轴数量一致、顺序一致的 RPM 序列（复用内部缓冲）。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="topology"/> 为空时抛出。</exception>
        /// <exception cref="ArgumentException">当拓扑轴数与配置轴数或输入速度长度不一致时抛出。</exception>
        public ReadOnlyMemory<AxisRpm> Plan(ConveyorTopology topology, in SpeedSet input) {
            if (topology is null) throw new ArgumentNullException(nameof(topology));

            var speeds = input.SegmentSpeeds.Span;
            var n = topology.Axes.Count;
            if (n != _cfg.AxisCount || speeds.Length != n)
                throw new ArgumentException("Topology/Axes count or input speed length mismatch.");

            var p = Volatile.Read(ref _params);
            var dt = Math.Max(1e-6, p.SamplingPeriod.TotalSeconds);

            UpdateStatusByFrameSeq(input.FrameSeq);

            for (var i = 0; i < n; i++) {
                // 1) 单位换算：m/s → RPM（若输入本身即 RPM 则直通）
                var rpm = input.Unit == SpeedUnit.MetersPerSecond
                    ? LinearSpeedToRpm(speeds[i], i)
                    : speeds[i];

                // 2) 限幅：运行参数限幅 + 叠加硬件线速度上限（换算成该轴的 RPM）
                rpm = Math.Clamp(rpm, p.MinRpm, p.MaxRpm);
                rpm = ClampByHardwareSpeedLimit(rpm, i);

                // 3) 缺帧策略：Degraded 且 HoldOnNoFrame = true 时保持上次输出（也可改为缓降策略）
                if (_status == PlannerStatus.Degraded && p.HoldOnNoFrame) {
                    rpm = _lastRpm[i];
                }

                // 4) 平滑（滑动平均）
                rpm = Smooth(i, rpm);

                // 5) 斜率限制（加速度限制，单位 RPM/s）
                rpm = RampLimit(i, rpm, p.MaxAccelRpmPerSec, (decimal)dt);

                // 输出、保存历史（注意：_out 为复用缓冲，下一次 Plan 会覆盖）
                _out[i] = new AxisRpm(rpm);
                _lastRpm[i] = rpm;
            }

            if (_status == PlannerStatus.Idle)
                _status = PlannerStatus.Running;

            return _out;
        }

        /// <summary>
        /// 释放资源（当前无非托管资源，预留扩展）。
        /// </summary>
        public void Dispose() {
            // 当前无非托管资源；若未来引入池化或非托管句柄，在此处释放。
        }

        // ---------------- 内部工具 ----------------

        /// <summary>
        /// 按轴数与滑窗大小分配/重建内部缓冲并清零。
        /// </summary>
        /// <param name="axisCount">轴数量（必须为正）。</param>
        /// <param name="smoothWindow">滑动平均窗口大小（>= 1；值越大越平滑，但响应变慢）。</param>
        private void AllocateBuffers(int axisCount, int smoothWindow) {
            _lastRpm = new decimal[axisCount];
            _smoothSum = new decimal[axisCount];
            _smoothBuf = new decimal[axisCount, smoothWindow];
            _smoothCount = new int[axisCount];
            _smoothIndex = new int[axisCount];
            _out = new AxisRpm[axisCount];
        }

        /// <summary>
        /// 线速度（m/s）按该轴直径与齿轮比换算为 RPM。
        /// 公式：rpm = (v / (π * D)) * gearRatio * 60
        /// </summary>
        /// <param name="beltMetersPerSec">该轴对应段的线速度（单位：米/秒）。</param>
        /// <param name="axisIndex">轴索引（0..AxisCount-1），用于读取直径/齿比。</param>
        /// <returns>换算得到的电机转速（RPM）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal LinearSpeedToRpm(decimal beltMetersPerSec, int axisIndex) {
            var d = _cfg.BeltDiameter[axisIndex]; // 皮带直径（米）
            var gr = _cfg.GearRatio[axisIndex];    // 齿轮比（电机：皮带）
            if (d <= 0) return 0.0m;

            var revPerSec = (beltMetersPerSec / (decimal)(Math.PI * d)) * (decimal)gr;
            return revPerSec * 60.0m;
        }

        /// <summary>
        /// 叠加硬件线速度上限（以 m/s 表示）后，换算为该轴 RPM 上下限并进行二次限幅。
        /// </summary>
        /// <param name="rpm">一次限幅后的目标 RPM（基于运行参数）。</param>
        /// <param name="axisIndex">轴索引（0..AxisCount-1），用于读取硬件上/下限。</param>
        /// <returns>考虑硬件限制后的 RPM。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal ClampByHardwareSpeedLimit(decimal rpm, int axisIndex) {
            var maxV = _cfg.MaxBeltSpeed;
            if (maxV <= 0) return rpm;

            var maxRpmHw = LinearSpeedToRpm((decimal)maxV, axisIndex);
            var minRpmHw = _cfg.MinBeltSpeed > 0 ? LinearSpeedToRpm((decimal)_cfg.MinBeltSpeed, axisIndex) : 0.0m;

            if (rpm > maxRpmHw) rpm = maxRpmHw;
            if (rpm < minRpmHw) rpm = minRpmHw;
            return rpm;
        }

        /// <summary>
        /// 简单滑动平均平滑：固定窗口环形缓冲，减少抖动。
        /// </summary>
        /// <param name="axis">轴索引（0..AxisCount-1），使用各自的滑窗状态。</param>
        /// <param name="value">当前周期欲输出的 RPM 值（未平滑）。</param>
        /// <returns>平滑后的 RPM 值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal Smooth(int axis, decimal value) {
            var cap = _smoothBuf.GetLength(1);
            if (cap <= 1) return value;

            var idx = _smoothIndex[axis];
            var old = _smoothBuf[axis, idx];

            if (_smoothCount[axis] < cap) {
                _smoothCount[axis]++;
            }
            else {
                _smoothSum[axis] -= old; // 移除旧值贡献
            }

            _smoothBuf[axis, idx] = value;
            _smoothSum[axis] += value;

            _smoothIndex[axis] = (idx + 1) % cap;

            var denom = _smoothCount[axis];
            return denom > 0 ? _smoothSum[axis] / denom : value;
        }

        /// <summary>
        /// 斜率限制（加速度限制）：限制 RPM 在每周期内的最大变化量（MaxAccelRpmPerSec * dt）。
        /// </summary>
        /// <param name="axis">轴索引（0..AxisCount-1）。</param>
        /// <param name="target">希望到达的目标 RPM（已平滑/限幅后）。</param>
        /// <param name="maxAccelRpmPerSec">最大加速度（单位：RPM/s）。</param>
        /// <param name="dt">当前控制周期时长（秒）。</param>
        /// <returns>应用斜率限制后的 RPM。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal RampLimit(int axis, decimal target, decimal maxAccelRpmPerSec, decimal dt) {
            if (maxAccelRpmPerSec <= 0) return target;

            var current = _lastRpm[axis];
            var delta = target - current;
            var maxDelta = maxAccelRpmPerSec * dt;

            if (delta > maxDelta) return current + maxDelta;
            if (delta < -maxDelta) return current - maxDelta;
            return target;
        }

        /// <summary>
        /// 根据帧序检查乱序/丢帧：正常递增为 Running，非递增或跳变置为 Degraded。
        /// </summary>
        /// <param name="seq">本帧的序号（与上一帧相比用于判定连续/丢失/乱序）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateStatusByFrameSeq(long seq) {
            if (_lastFrameSeq < 0) {
                _lastFrameSeq = seq;
                _status = PlannerStatus.Idle;
                return;
            }

            var gap = seq - _lastFrameSeq;
            _lastFrameSeq = seq;

            // gap: 本帧序号与上一帧序号的差值（seq - lastSeq）
            _status = gap switch {
                1 => PlannerStatus.Running,   // 连续
                > 1 => PlannerStatus.Degraded,  // 丢帧
                <= 0 => PlannerStatus.Degraded   // 乱序/重放
            };
        }
    }
}