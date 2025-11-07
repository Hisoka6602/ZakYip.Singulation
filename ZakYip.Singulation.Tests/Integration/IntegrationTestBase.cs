using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Tests.Integration {

    /// <summary>
    /// 集成测试基类，提供 HttpClient 和通用测试工具
    /// 
    /// 注意：这些测试需要实际运行的 Host 服务，使用方法：
    /// 1. 启动 ZakYip.Singulation.Host 项目（dotnet run）
    /// 2. 运行集成测试（dotnet test --filter Category=Integration）
    /// </summary>
    public class IntegrationTestBase : IDisposable {
        protected readonly HttpClient Client;
        protected readonly string BaseUrl;

        public IntegrationTestBase() {
            BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "http://localhost:5005";
            Client = new HttpClient {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        protected async Task<HttpResponseMessage> GetAsync(string url) {
            return await Client.GetAsync(url);
        }

        protected async Task<T?> GetJsonAsync<T>(string url) {
            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data) {
            return await Client.PostAsJsonAsync(url, data);
        }

        protected async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T data) {
            return await Client.PutAsJsonAsync(url, data);
        }

        protected async Task<HttpResponseMessage> DeleteAsync(string url) {
            return await Client.DeleteAsync(url);
        }

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        protected async Task<bool> IsServiceAvailableAsync() {
            try {
                var response = await Client.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch {
                return false;
            }
        }

        public void Dispose() {
            Client?.Dispose();
        }
    }
}
