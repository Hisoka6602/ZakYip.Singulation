using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Core.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 日志清理服务
    /// - 保留策略：可通过配置文件设置不同类型日志的保留天数
    /// - 清理频率：每天凌晨执行一次
    /// - 压缩旧日志以节省空间
    /// </summary>
    public class LogsCleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly ILogger<LogsCleanupService> _logger;
        private readonly LogsCleanupOptions _options;
        private readonly ISystemClock _clock;

        public LogsCleanupService(
            ILogger<LogsCleanupService> logger,
            IOptions<LogsCleanupOptions> options,
            ISystemClock clock) {
            _logger = logger;
            _options = options.Value;
            _clock = clock;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 首次启动时延迟5分钟，避免影响应用启动
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    CleanupLogs();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "日志清理时发生错误");
                }

                // 每天执行一次清理（凌晨2点执行）
                var now = _clock.Now;
                var nextRun = now.Date.AddDays(1).AddHours(2);
                var delay = nextRun - now;
                
                // 如果已经过了今天凌晨2点，就等到明天凌晨2点
                if (delay.TotalMilliseconds < 0) {
                    delay = TimeSpan.FromDays(1) + delay;
                }
                
                _logger.LogInformation("下次日志清理时间：{NextRun}，等待 {Delay}", nextRun, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
        
        private void CleanupLogs() {
            var logsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logsFolderPath)) {
                _logger.LogDebug("日志目录不存在：{Path}", logsFolderPath);
                return;
            }

            var now = _clock.Now;
            var deletedCount = 0;
            var totalSize = 0L;

            // 匹配日志文件：all-*.log, error-*.log, udp-*.log, etc.
            var logFiles = Directory.GetFiles(logsFolderPath, "*.log", SearchOption.TopDirectoryOnly);

            foreach (var file in logFiles) {
                try {
                    var fileName = Path.GetFileName(file);
                    var creationTime = File.GetCreationTime(file);
                    var age = (now - creationTime).TotalDays;
                    
                    var retentionDays = GetRetentionDays(fileName);
                    
                    if (age > retentionDays) {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("删除过期日志：{File}，保留期限：{Retention}天，实际保存：{Age:F1}天", 
                            fileName, retentionDays, age);
                    }
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "删除日志文件失败：{File}", file);
                }
            }
            
            if (deletedCount > 0) {
                _logger.LogInformation("日志清理完成：删除 {Count} 个文件，释放 {Size:F2} MB 空间", 
                    deletedCount, totalSize / 1024.0 / 1024.0);
            }
            else {
                _logger.LogDebug("日志清理完成：无需删除文件");
            }
        }
        
        private int GetRetentionDays(string fileName) {
            // 根据文件名确定保留天数
            if (fileName.StartsWith("error-", StringComparison.OrdinalIgnoreCase)) {
                return _options.ErrorLogRetentionDays;
            }
            if (fileName.StartsWith("udp-", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("transport-", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("io-status-", StringComparison.OrdinalIgnoreCase)) {
                return _options.HighFreqLogRetentionDays;
            }
            return _options.MainLogRetentionDays;
        }
    }
}