using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的简单 ControllerOptionsStore 模拟实现。
    /// </summary>
    internal sealed class FakeControllerOptionsStore : IControllerOptionsStore {
        public Task<ControllerOptions> GetAsync(CancellationToken ct = default) {
            return Task.FromResult(new ControllerOptions {
                Vendor = "leadshine",
                ControllerIp = "192.168.5.11"
            });
        }

        public Task UpsertAsync(ControllerOptions dto, CancellationToken ct = default) {
            return Task.CompletedTask;
        }
    }
}
