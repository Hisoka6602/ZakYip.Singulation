using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Workers {

    public class SingulationWorker : BackgroundService {
        private readonly ILogger<SingulationWorker> _logger;

        public SingulationWorker(ILogger<SingulationWorker> logger) {
            _logger = logger;
        }

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