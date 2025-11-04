using System;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Runtime;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的假 RuntimeStatusProvider 实现。
    /// 提供可配置的系统状态，用于测试 Infrastructure 层服务。
    /// </summary>
    internal sealed class FakeRuntimeStatusProvider : IRuntimeStatusProvider {
        public SystemState SystemState { get; set; } = SystemState.Stopped;
        public bool HasError { get; set; } = false;

        public SystemRuntimeStatus Snapshot() {
            // 返回默认状态的运行时快照对象
            // 测试场景中通常不关注快照的具体内容，仅需要满足接口要求
            return new SystemRuntimeStatus();
        }

        // 以下方法在测试场景中不需要实现具体逻辑
        // 它们是 IRuntimeStatusProvider 接口的一部分，但对测试辅助类无实际作用

        public void OnTransportState(string name, string role, string status, string? remote) {
            // 测试中不关注传输层状态变化，空实现
        }

        public void OnTransportBytes(string name, int bytes) {
            // 测试中不关注传输层字节统计，空实现
        }

        public void OnUpstreamHeartbeat(DateTime utc, double? fps = null) {
            // 测试中不关注上游心跳事件，空实现
        }

        public void OnControllerInfo(bool online, string? vendor, string? ip, int axisCount) {
            // 测试中不关注控制器信息更新，空实现
        }
    }
}
