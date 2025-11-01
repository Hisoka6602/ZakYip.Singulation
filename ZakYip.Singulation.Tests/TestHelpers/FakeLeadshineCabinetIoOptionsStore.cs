using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Tests.TestHelpers {

    internal sealed class FakeLeadshineCabinetIoOptionsStore : ILeadshineCabinetIoOptionsStore {
        public Task<LeadshineCabinetIoOptions> GetAsync(CancellationToken ct = default) {
            return Task.FromResult(new LeadshineCabinetIoOptions {
                Enabled = false,
                PollingIntervalMs = 50,
                CabinetInputPoint = new CabinetInputPoint(),
                CabinetIndicatorPoint = new CabinetIndicatorPoint {
                    RunningWarningSeconds = 0
                }
            });
        }

        public Task SaveAsync(LeadshineCabinetIoOptions options, CancellationToken ct = default) {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            return Task.CompletedTask;
        }
    }
}
