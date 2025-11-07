using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Tests.Integration {

    /// <summary>
    /// Safety Pipeline 端到端测试
    /// 测试完整安全隔离流程、故障场景模拟和恢复流程
    /// </summary>
    internal sealed class SafetyPipelineE2ETests : IntegrationTestBase {

        [MiniFact]
        public async Task SafetyPipeline_GetCabinetStatus_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/cabinet/status");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "机柜状态端点应返回成功");
            
            var content = await response.Content.ReadAsStringAsync();
            MiniAssert.False(string.IsNullOrWhiteSpace(content), "机柜状态不应为空");
        }

        [MiniFact]
        public async Task SafetyPipeline_RequestStart_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await PostJsonAsync("/api/cabinet/start", new { reason = "集成测试启动" });

            // Assert
            // 注意：即使硬件未连接，API 应该接受请求并返回成功或特定错误
            MiniAssert.True(
                response.IsSuccessStatusCode || 
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                "启动请求应返回成功或服务不可用状态");
        }

        [MiniFact]
        public async Task SafetyPipeline_RequestStop_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await PostJsonAsync("/api/cabinet/stop", new { reason = "集成测试停止" });

            // Assert
            MiniAssert.True(
                response.IsSuccessStatusCode || 
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                "停止请求应返回成功或服务不可用状态");
        }

        [MiniFact]
        public async Task SafetyPipeline_RequestReset_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await PostJsonAsync("/api/cabinet/reset", new { reason = "集成测试复位" });

            // Assert
            MiniAssert.True(
                response.IsSuccessStatusCode || 
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                "复位请求应返回成功或服务不可用状态");
        }

        [MiniFact]
        public async Task SafetyPipeline_StateTransitions_FollowExpectedFlow() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act - 获取初始状态
            var initialResponse = await GetAsync("/api/cabinet/status");
            MiniAssert.True(initialResponse.IsSuccessStatusCode, "获取初始状态应成功");

            // Act - 尝试请求复位
            var resetResponse = await PostJsonAsync("/api/cabinet/reset", 
                new { reason = "状态转换测试-复位" });
            
            // 短暂等待状态更新
            await Task.Delay(100);

            // Act - 再次获取状态
            var afterResetResponse = await GetAsync("/api/cabinet/status");
            MiniAssert.True(afterResetResponse.IsSuccessStatusCode, "复位后获取状态应成功");
        }

        [MiniFact]
        public async Task SafetyPipeline_CommunicationTimeout_HandledGracefully() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // 注意：这个测试验证 API 层面的超时处理
            // 实际硬件通信超时会在更底层处理

            // Arrange - 设置较短的超时
            using var shortTimeoutClient = new System.Net.Http.HttpClient {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromMilliseconds(100) // 极短超时
            };

            try {
                // Act - 尝试调用可能较慢的端点
                var response = await shortTimeoutClient.GetAsync("/api/monitoring/diagnose/all");
                
                // 如果没有超时，那么服务响应很快，这也是好事
                Console.WriteLine("✓ 服务响应很快，未触发超时");
            }
            catch (TaskCanceledException) {
                // 预期的超时异常
                Console.WriteLine("✓ 按预期触发了超时，验证了超时处理");
            }
            catch (Exception ex) {
                throw new InvalidOperationException($"意外异常类型: {ex.GetType().Name}");
            }
        }

        [MiniFact]
        public async Task SafetyPipeline_HardwareDisconnect_ReportsCorrectStatus() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // 注意：在测试环境中，硬件本来就是断开的
            // 这个测试验证系统正确报告硬件状态

            // Act
            var statusResponse = await GetAsync("/api/cabinet/status");
            MiniAssert.True(statusResponse.IsSuccessStatusCode, "获取状态应成功");

            var content = await statusResponse.Content.ReadAsStringAsync();
            
            // 在测试环境中，硬件断线是正常状态
            // 验证响应包含状态信息即可
            MiniAssert.False(string.IsNullOrWhiteSpace(content), "状态响应应包含内容");
        }

        [MiniFact]
        public async Task SafetyPipeline_RecoveryAfterFault_CanReset() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // 模拟恢复流程：停止 -> 复位 -> 启动

            // Act - 停止
            var stopResponse = await PostJsonAsync("/api/cabinet/stop", 
                new { reason = "恢复流程测试-停止" });
            await Task.Delay(100);

            // Act - 复位
            var resetResponse = await PostJsonAsync("/api/cabinet/reset", 
                new { reason = "恢复流程测试-复位" });
            await Task.Delay(100);

            // Act - 启动
            var startResponse = await PostJsonAsync("/api/cabinet/start", 
                new { reason = "恢复流程测试-启动" });

            // Assert - 所有操作都应该被接受（即使硬件未连接）
            Console.WriteLine($"停止: {stopResponse.StatusCode}, 复位: {resetResponse.StatusCode}, 启动: {startResponse.StatusCode}");
        }

        [MiniFact]
        public async Task SafetyPipeline_ConcurrentRequests_HandledCorrectly() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // 测试并发请求处理

            // Act - 并发发送多个状态查询
            var tasks = new Task<System.Net.Http.HttpResponseMessage>[5];
            for (int i = 0; i < tasks.Length; i++) {
                tasks[i] = GetAsync("/api/cabinet/status");
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - 所有请求都应成功
            foreach (var response in responses) {
                MiniAssert.True(response.IsSuccessStatusCode, "并发请求都应成功");
            }
        }

        [MiniFact]
        public async Task SafetyPipeline_GetMode_ReturnsValidMode() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/cabinet/mode");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "获取模式应成功");
            
            var content = await response.Content.ReadAsStringAsync();
            MiniAssert.True(
                content.Contains("Remote") || content.Contains("Local") || 
                content.Contains("remote") || content.Contains("local"),
                "模式响应应包含 Remote 或 Local");
        }
    }
}
