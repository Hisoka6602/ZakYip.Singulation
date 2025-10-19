using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Host.Safety {

    public sealed class DefaultCommissioningSequence : ICommissioningSequence {
        private readonly ILogger<DefaultCommissioningSequence> _log;
        private readonly IAxisController _controller;

        public DefaultCommissioningSequence(ILogger<DefaultCommissioningSequence> log, IAxisController controller) {
            _log = log;
            _controller = controller;
        }

        public async Task PowerOnAsync(CancellationToken ct) {
            _log.LogInformation("Commissioning: Enable all axes.");
            await _controller.EnableAllAsync(ct).ConfigureAwait(false);
        }

        public async Task HomeAsync(CancellationToken ct) {
            _log.LogInformation("Commissioning: Home sequence (stop all, zero position stub).");
            await _controller.StopAllAsync(ct).ConfigureAwait(false);
        }

        public async Task AlignAsync(CancellationToken ct) {
            _log.LogInformation("Commissioning: Align sequence (prepare zero speed).");
            await _controller.WriteSpeedAllAsync(0m, ct).ConfigureAwait(false);
        }

        public async Task FailToSafeAsync(string reason, CancellationToken ct) {
            _log.LogWarning("Commissioning: Fail to safe due to {Reason}.", reason);
            await _controller.StopAllAsync(ct).ConfigureAwait(false);
        }
    }
}
