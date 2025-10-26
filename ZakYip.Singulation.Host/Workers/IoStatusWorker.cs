using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Host.Services;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// IO 状态监控后台服务，定期轮询所有 IO 状态并通过 SignalR 广播。
    /// </summary>
    public sealed class IoStatusWorker : BackgroundService {
        private readonly ILogger<IoStatusWorker> _logger;
        private readonly IoStatusService _ioStatusService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IOptionsMonitor<IoStatusMonitorOptions> _optionsMonitor;

        public IoStatusWorker(
            ILogger<IoStatusWorker> logger,
            IoStatusService ioStatusService,
            IRealtimeNotifier notifier,
            IOptionsMonitor<IoStatusMonitorOptions> optionsMonitor) {
            _logger = logger;
            _ioStatusService = ioStatusService;
            _notifier = notifier;
            _optionsMonitor = optionsMonitor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var options = _optionsMonitor.CurrentValue;

            if (!options.Enabled) {
                _logger.LogInformation("IO 状态监控已禁用");
                return;
            }

            _logger.LogInformation(
                "IO 状态监控已启动：输入 IO [{InputStart}-{InputEnd}]，输出 IO [{OutputStart}-{OutputEnd}]，轮询间隔={PollingMs}ms，广播频道={Channel}",
                options.InputStart, options.InputStart + options.InputCount - 1,
                options.OutputStart, options.OutputStart + options.OutputCount - 1,
                options.PollingIntervalMs, options.SignalRChannel);

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var currentOptions = _optionsMonitor.CurrentValue;

                    if (!currentOptions.Enabled) {
                        _logger.LogInformation("IO 状态监控已禁用，停止监控");
                        break;
                    }

                    // 查询所有 IO 状态
                    var ioStatus = await _ioStatusService.GetAllIoStatusAsync(
                        currentOptions.InputStart,
                        currentOptions.InputCount,
                        currentOptions.OutputStart,
                        currentOptions.OutputCount,
                        stoppingToken);

                    // 通过 SignalR 广播 IO 状态
                    await _notifier.PublishAsync(currentOptions.SignalRChannel, ioStatus, stoppingToken);

                    _logger.LogDebug(
                        "IO 状态已广播：总计 {TotalCount} 个 IO，成功 {ValidCount} 个，失败 {ErrorCount} 个",
                        ioStatus.TotalCount, ioStatus.ValidCount, ioStatus.ErrorCount);

                    // 等待下一次轮询
                    await Task.Delay(currentOptions.PollingIntervalMs, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "查询或广播 IO 状态时发生异常");
                    // 发生异常后等待一段时间再重试，避免高频错误
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("IO 状态监控已停止");
        }
    }
}
