using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的假 IoLinkageOptionsStore 实现。
    /// 提供基于内存的配置存储，无需依赖 LiteDB 数据库。
    /// 适用于单元测试场景，支持配置的快速读写和状态重置。
    /// </summary>
    internal sealed class FakeIoLinkageStore : IIoLinkageOptionsStore {
        public IoLinkageOptions Options { get; set; } = new IoLinkageOptions();

        public Task<IoLinkageOptions> GetAsync(CancellationToken ct = default) => Task.FromResult(Options);

        public Task SaveAsync(IoLinkageOptions options, CancellationToken ct = default) {
            Options = options;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            Options = new IoLinkageOptions();
            return Task.CompletedTask;
        }
    }
}
