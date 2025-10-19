using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.Runtime {

    /// <summary>
    /// 基于当前进程镜像的重启实现，默认延迟 500ms 触发。
    /// </summary>
    public sealed class ProcessApplicationRestarter : IApplicationRestarter {
        private readonly ILogger<ProcessApplicationRestarter> _logger;
        private readonly TimeSpan _delay;
        private readonly object _gate = new();
        private volatile bool _scheduled;

        public ProcessApplicationRestarter(ILogger<ProcessApplicationRestarter> logger)
            : this(logger, TimeSpan.FromMilliseconds(500)) { }

        public ProcessApplicationRestarter(ILogger<ProcessApplicationRestarter> logger, TimeSpan delay) {
            _logger = logger;
            _delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        public Task RestartAsync(string reason, CancellationToken ct = default) {
            lock (_gate) {
                if (_scheduled) return Task.CompletedTask;
                _scheduled = true;
            }

            var message = string.IsNullOrWhiteSpace(reason) ? "未知原因" : reason;
            _logger.LogWarning("由于 {Reason}，系统将自动重启。", message);

            var _ = Task.Run(async () => {
                try {
                    await Task.Delay(_delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    lock (_gate) {
                        _scheduled = false;
                    }
                    _logger.LogWarning("重启请求在延迟过程中被取消，已放弃重启。");
                    return;
                }

                try {
                    var executable = Environment.ProcessPath;
                    if (string.IsNullOrWhiteSpace(executable)) {
                        _logger.LogError("无法解析当前可执行文件路径，重启流程中止。");
                        return;
                    }

                    var args = Environment.GetCommandLineArgs();
                    var startInfo = new ProcessStartInfo(executable) {
                        UseShellExecute = false,
                        WorkingDirectory = Environment.CurrentDirectory
                    };

                    if (args is { Length: > 1 }) {
                        for (var i = 1; i < args.Length; i++) {
                            startInfo.ArgumentList.Add(args[i]);
                        }
                    }

                    _logger.LogInformation("即将启动新进程 {Path} 以完成重启。", executable);
                    Process.Start(startInfo);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "进程重启失败");
                }
                finally {
                    try {
                        _logger.LogInformation("当前进程准备退出，交由外部守护拉起。");
                    }
                    catch { }
                    Environment.Exit(3);
                }
            }, CancellationToken.None);

            return Task.CompletedTask;
        }
    }
}