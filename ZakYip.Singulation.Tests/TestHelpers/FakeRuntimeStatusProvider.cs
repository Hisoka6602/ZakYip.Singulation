using System;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Runtime;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的假 RuntimeStatusProvider 实现。
    /// </summary>
    internal sealed class FakeRuntimeStatusProvider : IRuntimeStatusProvider {
        public SystemState SystemState { get; set; } = SystemState.Stopped;
        public bool HasError { get; set; } = false;

        public SystemRuntimeStatus Snapshot() {
            return new SystemRuntimeStatus {
                // 填充基本的状态快照字段
            };
        }

        public void OnTransportState(string name, string role, string status, string? remote) {
            // 不需要实现
        }

        public void OnTransportBytes(string name, int bytes) {
            // 不需要实现
        }

        public void OnUpstreamHeartbeat(DateTime utc, double? fps = null) {
            // 不需要实现
        }

        public void OnControllerInfo(bool online, string? vendor, string? ip, int axisCount) {
            // 不需要实现
        }
    }
}
