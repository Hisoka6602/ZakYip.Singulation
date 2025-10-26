using System;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 IoStatusMonitorOptions 配置的正确性。
    /// </summary>
    internal sealed class IoStatusMonitorOptionsTests {

        [MiniFact]
        public void DefaultValuesAreCorrect() {
            var options = new IoStatusMonitorOptions();
            
            MiniAssert.Equal(true, options.Enabled, "IO 状态监控默认应启用");
            MiniAssert.Equal(0, options.InputStart, "输入 IO 起始位号默认应为 0");
            MiniAssert.Equal(32, options.InputCount, "输入 IO 数量默认应为 32");
            MiniAssert.Equal(0, options.OutputStart, "输出 IO 起始位号默认应为 0");
            MiniAssert.Equal(32, options.OutputCount, "输出 IO 数量默认应为 32");
            MiniAssert.Equal(500, options.PollingIntervalMs, "轮询间隔默认应为 500ms");
            MiniAssert.Equal("/io/status", options.SignalRChannel, "SignalR 频道默认应为 /io/status");
        }

        [MiniFact]
        public void CustomValuesAreRespected() {
            var options = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 10,
                InputCount = 16,
                OutputStart = 20,
                OutputCount = 8,
                PollingIntervalMs = 1000,
                SignalRChannel = "/custom/io"
            };
            
            MiniAssert.Equal(false, options.Enabled, "自定义启用状态应生效");
            MiniAssert.Equal(10, options.InputStart, "自定义输入起始位号应生效");
            MiniAssert.Equal(16, options.InputCount, "自定义输入数量应生效");
            MiniAssert.Equal(20, options.OutputStart, "自定义输出起始位号应生效");
            MiniAssert.Equal(8, options.OutputCount, "自定义输出数量应生效");
            MiniAssert.Equal(1000, options.PollingIntervalMs, "自定义轮询间隔应生效");
            MiniAssert.Equal("/custom/io", options.SignalRChannel, "自定义频道应生效");
        }

        [MiniFact]
        public void HighVolumeIoConfiguration() {
            // 测试大量 IO 端口配置（如 1024 个 IO）
            var options = new IoStatusMonitorOptions {
                InputStart = 0,
                InputCount = 512,
                OutputStart = 0,
                OutputCount = 512,
                PollingIntervalMs = 1000
            };
            
            MiniAssert.Equal(512, options.InputCount, "应支持大量输入 IO");
            MiniAssert.Equal(512, options.OutputCount, "应支持大量输出 IO");
            MiniAssert.Equal(1000, options.PollingIntervalMs, "高负载下应使用较长轮询间隔");
        }

        [MiniFact]
        public void MinimalIoConfiguration() {
            // 测试最小 IO 配置（如仅监控几个关键 IO）
            var options = new IoStatusMonitorOptions {
                InputStart = 0,
                InputCount = 4,
                OutputStart = 0,
                OutputCount = 4,
                PollingIntervalMs = 100
            };
            
            MiniAssert.Equal(4, options.InputCount, "应支持少量输入 IO");
            MiniAssert.Equal(4, options.OutputCount, "应支持少量输出 IO");
            MiniAssert.Equal(100, options.PollingIntervalMs, "低负载下可使用较短轮询间隔");
        }
    }
}
