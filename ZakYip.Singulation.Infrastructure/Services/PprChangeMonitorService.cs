using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Services {
    /// <summary>
    /// PPR 变化监控服务，定期检测 PPR 值变化并记录
    /// </summary>
    public sealed class PprChangeMonitorService : BackgroundService {
        private readonly ILogger<PprChangeMonitorService> _logger;
        private readonly IAxisController _axisController;
        private readonly IPprChangeRecordStore _recordStore;
        private readonly IHubContext<Hub> _hubContext;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        // 缓存上次检测到的 PPR 值
        private readonly Dictionary<string, int> _lastKnownPpr = new();
        private readonly object _cacheLock = new();

        public PprChangeMonitorService(
            ILogger<PprChangeMonitorService> logger,
            IAxisController axisController,
            IPprChangeRecordStore recordStore,
            IHubContext<Hub> hubContext) {
            _logger = logger;
            _axisController = axisController;
            _recordStore = recordStore;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("PPR 变化监控服务启动");

            // 初始化：加载当前所有轴的 PPR 值
            await InitializePprCacheAsync();

            try {
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Delay(_checkInterval, stoppingToken);
                    await CheckPprChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) {
                _logger.LogInformation("PPR 变化监控服务已取消");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "PPR 变化监控服务发生错误");
            }
        }

        private async Task InitializePprCacheAsync() {
            try {
                var drives = _axisController.Drives.ToList();
                lock (_cacheLock) {
                    foreach (var drive in drives) {
                        if (drive.Ppr.HasValue && drive.Ppr.Value > 0) {
                            _lastKnownPpr[drive.Axis.ToString()] = drive.Ppr.Value;
                        }
                    }
                }
                _logger.LogInformation("初始化 PPR 缓存，共 {Count} 个轴", _lastKnownPpr.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "初始化 PPR 缓存失败");
            }
        }

        private async Task CheckPprChangesAsync(CancellationToken ct) {
            try {
                var drives = _axisController.Drives.ToList();

                foreach (var drive in drives) {
                    var axisId = drive.Axis.ToString();
                    var currentPpr = drive.Ppr;

                    if (!currentPpr.HasValue || currentPpr.Value <= 0) {
                        continue; // PPR 未初始化或无效，跳过
                    }

                    int? oldPpr;
                    bool hasChange;

                    lock (_cacheLock) {
                        hasChange = _lastKnownPpr.TryGetValue(axisId, out var lastPpr) && lastPpr != currentPpr.Value;
                        oldPpr = hasChange ? lastPpr : (int?)null;

                        // 更新缓存
                        _lastKnownPpr[axisId] = currentPpr.Value;
                    }

                    if (hasChange && oldPpr.HasValue) {
                        // 检测到 PPR 变化
                        var reason = DetectChangeReason(oldPpr.Value, currentPpr.Value);
                        var isAnomalous = IsAnomalousChange(oldPpr.Value, currentPpr.Value);

                        var record = new PprChangeRecord {
                            AxisId = axisId,
                            OldPpr = oldPpr.Value,
                            NewPpr = currentPpr.Value,
                            Reason = reason,
                            IsAnomalous = isAnomalous,
                            ChangedAt = DateTime.Now
                        };

                        await _recordStore.SaveAsync(record, ct);

                        var logLevel = isAnomalous ? LogLevel.Warning : LogLevel.Information;
                        _logger.Log(logLevel, 
                            "检测到轴 {AxisId} 的 PPR 值变化: {OldPpr} -> {NewPpr}, 原因: {Reason}, 异常: {IsAnomalous}",
                            axisId, oldPpr.Value, currentPpr.Value, reason, isAnomalous);

                        // 推送通知到客户端
                        var dto = new PprChangeRecordDto {
                            Id = record.Id,
                            AxisId = axisId,
                            OldPpr = oldPpr.Value,
                            NewPpr = currentPpr.Value,
                            Reason = reason,
                            ChangedAt = record.ChangedAt,
                            IsAnomalous = isAnomalous
                        };

                        await _hubContext.Clients.All.SendAsync("ReceivePprChange", dto, ct);

                        // 如果是异常变化，发送告警
                        if (isAnomalous) {
                            await SendAnomalyAlertAsync(dto, ct);
                        }
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "检查 PPR 变化失败");
            }
        }

        private string DetectChangeReason(int oldPpr, int newPpr) {
            // 根据 PPR 变化的模式推断可能的原因
            var ratio = (double)newPpr / oldPpr;

            if (Math.Abs(ratio - 2.0) < 0.1 || Math.Abs(ratio - 0.5) < 0.1) {
                return "可能的传动比调整（2倍或0.5倍关系）";
            }
            else if (newPpr == oldPpr * 4 || oldPpr == newPpr * 4) {
                return "可能的细分数调整（4倍关系）";
            }
            else if (Math.Abs(newPpr - oldPpr) < 100) {
                return "小幅调整，可能是参数微调";
            }
            else {
                return "PPR 值显著变化，可能是硬件更换或重新配置";
            }
        }

        private bool IsAnomalousChange(int oldPpr, int newPpr) {
            // 判断是否为异常变化
            // 1. PPR 值不应该在运行时频繁变化
            // 2. 变化幅度过大（超过 50%）可能是异常
            var ratio = (double)Math.Abs(newPpr - oldPpr) / oldPpr;
            
            // 如果变化超过 50% 认为是异常
            if (ratio > 0.5) {
                return true;
            }

            // 如果 PPR 值变为非常见值（不是 1000, 2000, 4000, 8000, 10000 等常见值）
            var commonPprValues = new[] { 1000, 2000, 2500, 4000, 5000, 8000, 10000, 20000 };
            if (!commonPprValues.Contains(newPpr) && !commonPprValues.Contains(oldPpr)) {
                return true;
            }

            return false;
        }

        private async Task SendAnomalyAlertAsync(PprChangeRecordDto changeRecord, CancellationToken ct) {
            try {
                // 发送异常告警到监控中心
                await _hubContext.Clients.All.SendAsync("ReceivePprAnomalyAlert", new {
                    Type = "PPR_ANOMALY",
                    Severity = "Warning",
                    Message = $"轴 {changeRecord.AxisId} 的 PPR 值发生异常变化: {changeRecord.OldPpr} -> {changeRecord.NewPpr}",
                    AxisId = changeRecord.AxisId,
                    Details = changeRecord,
                    Timestamp = DateTime.Now
                }, ct);

                _logger.LogWarning("已发送 PPR 异常告警: 轴 {AxisId}, {OldPpr} -> {NewPpr}",
                    changeRecord.AxisId, changeRecord.OldPpr, changeRecord.NewPpr);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "发送 PPR 异常告警失败");
            }
        }
    }
}
