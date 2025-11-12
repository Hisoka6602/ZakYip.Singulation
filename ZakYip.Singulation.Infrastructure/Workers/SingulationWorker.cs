using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 后台工作服务，用于执行分料系统的周期性任务。
    /// </summary>
    /// <remarks>
    /// 此服务在应用程序启动时自动启动，并在应用程序停止时自动停止。
    /// 当前实现包含一个保持服务活动的空循环。
    /// </remarks>
    public class SingulationWorker : BackgroundService {
        private readonly ILogger<SingulationWorker> _logger;

        /// <summary>
        /// 初始化 <see cref="SingulationWorker"/> 类的新实例。
        /// </summary>
        /// <param name="logger">用于记录工作器活动的日志记录器。</param>
        public SingulationWorker(ILogger<SingulationWorker> logger) {
            _logger = logger;
        }

        /// <summary>
        /// 执行后台工作任务的主方法。
        /// </summary>
        /// <param name="stoppingToken">用于通知任务应停止的取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法在后台线程中持续运行，直到应用程序停止。
        /// 当前实现为占位符，保持服务处于运行状态。
        /// </remarks>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                if (_logger.IsEnabled(LogLevel.Information)) {
                    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}