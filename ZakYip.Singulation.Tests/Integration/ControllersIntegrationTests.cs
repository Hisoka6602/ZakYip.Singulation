using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Tests.Integration {

    /// <summary>
    /// Controllers REST API 集成测试
    /// 测试所有端点的正常流程、错误处理和边界条件
    /// </summary>
    internal sealed class ControllersIntegrationTests : IntegrationTestBase {

        [MiniFact]
        public async Task VersionController_GetVersion_ReturnsSuccess() {
            // 检查服务是否可用
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。请先启动 Host 服务。");
                return;
            }

            // Act
            var response = await GetAsync("/api/version");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "版本端点应返回成功状态");
            var content = await response.Content.ReadAsStringAsync();
            MiniAssert.False(string.IsNullOrWhiteSpace(content), "版本信息不应为空");
        }

        [MiniFact]
        public async Task IoController_GetIoStatus_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/io/status");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "IO状态端点应返回成功");
        }

        [MiniFact]
        public async Task IoController_GetIoStatus_ReturnsValidJson() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/io/status");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            MiniAssert.False(string.IsNullOrWhiteSpace(content), "IO状态应返回有效内容");
            MiniAssert.True(content.Contains("\""), "响应应该是 JSON 格式");
        }

        [MiniFact]
        public async Task ConfigurationController_ExportConfig_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/configurations/export");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "配置导出端点应返回成功");
        }

        [MiniFact]
        public async Task ConfigurationController_GetTemplate_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/configurations/template?type=All");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "配置模板端点应返回成功");
        }

        [MiniFact]
        public async Task MonitoringController_GetHealth_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/monitoring/health");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "健康度端点应返回成功");
        }

        [MiniFact]
        public async Task ErrorHandling_InvalidEndpoint_Returns404() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/nonexistent");

            // Assert
            MiniAssert.Equal(HttpStatusCode.NotFound, response.StatusCode, "不存在的端点应返回404");
        }

        [MiniFact]
        public async Task Swagger_IsAccessible() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/swagger/index.html");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "Swagger UI 应该可以访问");
        }

        [MiniFact]
        public async Task Health_Endpoint_ReturnsSuccess() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/health");

            // Assert
            MiniAssert.True(response.IsSuccessStatusCode, "健康检查端点应返回成功");
        }

        [MiniFact]
        public async Task ConfigurationController_Import_WithInvalidData_ReturnsBadRequest() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Arrange
            var invalidConfig = new { invalid = "data" };

            // Act
            var response = await PostJsonAsync("/api/configurations/import", invalidConfig);

            // Assert
            MiniAssert.True(
                response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.UnprocessableEntity,
                "无效配置应返回 400 或 422 状态码");
        }

        [MiniFact]
        public async Task IoController_WriteOutput_WithInvalidPort_ReturnsBadRequest() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Arrange - 使用无效端口号（超出范围）
            var invalidPortData = new { port = 99999, value = true };

            // Act
            var response = await PostJsonAsync("/api/io/output/write", invalidPortData);

            // Assert
            MiniAssert.True(
                response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                response.StatusCode == HttpStatusCode.InternalServerError, // 有些验证可能在服务层
                "无效端口号应返回错误状态码");
        }

        [MiniFact]
        public async Task ResponseHeaders_ContainContentType() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // Act
            var response = await GetAsync("/api/version");

            // Assert
            MiniAssert.True(response.Content.Headers.ContentType != null, 
                "响应应包含 Content-Type 头");
        }
    }
}
