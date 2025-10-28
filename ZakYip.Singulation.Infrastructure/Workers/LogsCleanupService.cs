using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
﻿using ZakYip.Singulation.Core.Utils;
using System.Text.RegularExpressions;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 日志清理服务
    /// </summary>
    public class LogsCleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly ILogger<LogsCleanupService> _logger;
        private DateTime _lastDeleteTime = DateTime.MinValue;

        public LogsCleanupService(
            ILogger<LogsCleanupService> logger) {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                //删除本地日志文件
                var logsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logsFolderPath)) {
                    // 匹配日期命名的.log文件
                    var regex = new Regex(@"\d{4}-\d{2}-\d{2}\.log$", RegexOptions.IgnoreCase);
                    // 调用递归方法来处理logs文件夹及其所有子文件夹
                    var logFiles = FileUtils.GetLogFiles(logsFolderPath, regex);
                    foreach (var file in from file in logFiles
                                         let creationTime = File.GetCreationTime(file)
                                         let difference =
                                             DateTime.Now - creationTime
                                         where difference.TotalDays > 3
                                         select file) {
                        File.Delete(file);
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}