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
    /// 默认速度规划器（mm/s 原生版）：
    /// 输入：<see cref="SpeedSet"/>（mm/s）
    /// 处理：一次限幅（mm/s）→ 平滑（滑动平均）→ 斜率限制（mm/s²）
    /// 输出：各轴 <see cref="AxisRpm"/>（末端按轴几何从 mm/s 换算为 RPM 以对接驱动）
    /// </summary>
    /// <remarks>
    /// 与旧实现的差异：
    /// 1）不再读取 input 的单位枚举，不再使用旧的 SegmentSpeeds / FrameSeq 等字段；
    /// 2）内部状态全部采用 mm/s 表示，仅在出口做一次性 mm/s→RPM 的换算；
    /// 3）运行参数改为 <see cref="LinearPlannerParams"/>（mm/s 语义）。
    /// </remarks>
    public sealed class DefaultSpeedPlanner : ISpeedPlanner, IDisposable {
        private readonly PlannerConfig _cfg;
        private LinearPlannerParams _params;

        private volatile PlannerStatus _status = PlannerStatus.Idle;

        /// <summary>当前规划器运行状态。</summary>
        public PlannerStatus Status => _status;

        // —— 历史与平滑缓冲（单位：mm/s） ——
        private decimal[] _lastMmps = Array.Empty<decimal>();     // 上次输出（mm/s）

        private decimal[] _smoothSum = Array.Empty<decimal>();     // 各轴滑窗和值
        private decimal[,] _smoothBuf = new decimal[0, 0];         // [axis, k] 环形缓冲
        private int[] _smoothCount = Array.Empty<int>();           // 已填充计数
        private int[] _smoothIndex = Array.Empty<int>();           // 写指针

        // —— 输出缓存（RPM）——
        private AxisRpm[] _outRpm = Array.Empty<AxisRpm>();

        // —— 帧序跟踪（来自 SpeedSet.Sequence） ——
        private long _lastSeq = -1;

        /// <summary>
        /// 使用轴几何/上限配置与 mm/s 规划参数创建规划器。
        /// </summary>
        /// <param name="cfg">硬件/物理参数（轴数、皮带直径、齿比、线速度上/下限等）。</param>
        /// <param name="initialParams">mm/s 语义下的规划器运行参数。</param>
        public DefaultSpeedPlanner(PlannerConfig cfg, LinearPlannerParams initialParams) {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _params = initialParams ?? throw new ArgumentNullException(nameof(initialParams));

            if (_cfg.AxisCount <= 0)
                throw new ArgumentException("AxisCount must be positive.", nameof(cfg));

            AllocateBuffers(_cfg.AxisCount, Math.Max(1, _params.SmoothWindow));
        }

        /// <summary>
        /// 在线更新规划参数。若平滑窗口大小发生变化，会重建内部平滑缓冲并复位状态。
        /// </summary>
        /// <param name="p">新的 mm/s 规划参数。</param>
        public void Configure(LinearPlannerParams p) {
            Volatile.Write(ref _params, p);
            var newWin = Math.Max(1, p.SmoothWindow);
            if (_smoothBuf.GetLength(1) != newWin) {
                AllocateBuffers(_cfg.AxisCount, newWin);
                _status = PlannerStatus.Idle;
                _lastSeq = -1;
            }
        }

        /// <summary>释放资源（当前无非托管资源）。</summary>
        public void Dispose() { /* 预留 */ }

        // ---------------- 内部：缓冲/工具 ----------------

        /// <summary>
        /// 分配或重建缓冲（mm/s 的历史与平滑；RPM 的输出缓存）。
        /// </summary>
        private void AllocateBuffers(int axisCount, int smoothWindow) {
            _lastMmps = new decimal[axisCount];
            _smoothSum = new decimal[axisCount];
            _smoothBuf = new decimal[axisCount, smoothWindow];
            _smoothCount = new int[axisCount];
            _smoothIndex = new int[axisCount];
            _outRpm = new AxisRpm[axisCount];
        }

        /// <summary>
        /// 将输入的分离段速度转换为与拓扑轴数一致的 mm/s 向量。
        /// 注意：疏散段速度（EjectMmps）在当前阶段不使用，预留给未来阶段实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal[] GetConcatenatedMmps(in SpeedSet input, int expectedCount) {
            var main = input.MainMmps;
            // Note: EjectMmps (evacuation units) are intentionally NOT used at this stage.
            // They are reserved for future phase implementation.

            var total = main?.Count ?? 0;
            if (total != expectedCount)
                throw new ArgumentException($"Input mm/s length {total} != expected {expectedCount}");

            var arr = new decimal[total];
            int k = 0;
            if (main is not null)
                for (int i = 0; i < main.Count; i++) arr[k++] = main[i];
            return arr;
        }

        /// <summary>
        /// 简单滑动平均平滑（mm/s）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal Smooth(int axis, decimal value) {
            var cap = _smoothBuf.GetLength(1);
            if (cap <= 1) return value;

            var idx = _smoothIndex[axis];
            var old = _smoothBuf[axis, idx];

            if (_smoothCount[axis] < cap) _smoothCount[axis]++;
            else _smoothSum[axis] -= old;

            _smoothBuf[axis, idx] = value;
            _smoothSum[axis] += value;

            _smoothIndex[axis] = (idx + 1) % cap;

            var denom = _smoothCount[axis];
            return denom > 0 ? _smoothSum[axis] / denom : value;
        }

        /// <summary>
        /// 斜率限制（mm/s²）：控制每周期最大速度变化量。
        /// </summary>
        /// <param name="axis">轴索引。</param>
        /// <param name="targetMmps">目标速度（mm/s）。</param>
        /// <param name="maxAccelMmps2">最大加速度（mm/s²）。</param>
        /// <param name="dt">周期（秒）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal RampLimit(int axis, decimal targetMmps, decimal maxAccelMmps2, decimal dt) {
            if (maxAccelMmps2 <= 0) return targetMmps;

            var current = _lastMmps[axis];
            var delta = targetMmps - current;
            var maxDelta = maxAccelMmps2 * dt; // 每周期允许的最大变化量（mm/s）

            if (delta > maxDelta) return current + maxDelta;
            if (delta < -maxDelta) return current - maxDelta;
            return targetMmps;
        }

        /// <summary>
        /// 用帧序更新运行状态（Running/Degraded），处理丢帧/乱序。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateStatusBySeq(long seq) {
            if (_lastSeq < 0) { _lastSeq = seq; _status = PlannerStatus.Idle; return; }

            var gap = seq - _lastSeq;
            _lastSeq = seq;

            _status = gap switch {
                1 => PlannerStatus.Running,    // 连续
                > 1 => PlannerStatus.Degraded,   // 丢帧
                _ => PlannerStatus.Degraded    // 乱序/重放/相同
            };
        }
    }
}