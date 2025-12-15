using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Core.Exceptions;
using ZakYip.Singulation.Infrastructure.Logging;

namespace ZakYip.Singulation.Infrastructure.Services;

/// <summary>
/// 异常聚合服务：收集、聚合和上报系统异常
/// Exception Aggregation Service: Collects, aggregates, and reports system exceptions
/// </summary>
public sealed class ExceptionAggregationService : BackgroundService
{
    private readonly ILogger<ExceptionAggregationService> _logger;
    private readonly ISystemClock _clock;
    private readonly ConcurrentQueue<ExceptionRecord> _exceptionQueue = new();
    private readonly ConcurrentDictionary<string, ExceptionStatistics> _exceptionStats = new();
    private readonly TimeSpan _aggregationInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _reportInterval = TimeSpan.FromMinutes(15);
    private readonly int _maxQueueSize = 10000;
    private DateTime _lastReportTime;

    public ExceptionAggregationService(ILogger<ExceptionAggregationService> logger, ISystemClock clock)
    {
        _logger = logger;
        _clock = clock;
        _lastReportTime = _clock.UtcNow;
    }

    /// <summary>
    /// 记录异常
    /// Record an exception
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="context">上下文信息</param>
    public void RecordException(Exception exception, string? context = null)
    {
        if (exception == null) return;

        var record = new ExceptionRecord
        {
            Exception = exception,
            Context = context ?? "Unknown",
            Timestamp = _clock.UtcNow,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace
        };

        // 限制队列大小
        if (_exceptionQueue.Count >= _maxQueueSize)
        {
            _exceptionQueue.TryDequeue(out _);
        }

        _exceptionQueue.Enqueue(record);
    }

    /// <summary>
    /// 获取异常统计信息
    /// Get exception statistics
    /// </summary>
    public IReadOnlyDictionary<string, ExceptionStatistics> GetStatistics()
    {
        return new Dictionary<string, ExceptionStatistics>(_exceptionStats);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.ExceptionAggregationServiceStarted(_aggregationInterval.TotalMinutes);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_aggregationInterval, stoppingToken);
                await AggregateExceptionsAsync(stoppingToken);

                // 定期上报统计信息
                if (_clock.UtcNow - _lastReportTime >= _reportInterval)
                {
                    ReportStatistics();
                    _lastReportTime = _clock.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.ExceptionAggregationServiceCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异常聚合服务发生错误");
        }
    }

    private Task AggregateExceptionsAsync(CancellationToken ct)
    {
        var processedCount = 0;
        var batchSize = 1000;

        while (processedCount < batchSize && _exceptionQueue.TryDequeue(out var record))
        {
            processedCount++;
            var key = $"{record.ExceptionType}:{record.Context}";

            _exceptionStats.AddOrUpdate(
                key,
                _ => new ExceptionStatistics
                {
                    ExceptionType = record.ExceptionType,
                    Context = record.Context,
                    Count = 1,
                    FirstOccurrence = record.Timestamp,
                    LastOccurrence = record.Timestamp,
                    LastMessage = record.Message,
                    IsRetryable = record.Exception is SingulationException se && se.IsRetryable
                },
                (_, existing) =>
                {
                    existing.Count++;
                    existing.LastOccurrence = record.Timestamp;
                    existing.LastMessage = record.Message;
                    return existing;
                });
        }

        if (processedCount > 0)
        {
            _logger.ExceptionRecordsAggregated(processedCount);
        }

        return Task.CompletedTask;
    }

    private void ReportStatistics()
    {
        if (_exceptionStats.IsEmpty)
        {
            _logger.ExceptionReportEmpty();
            return;
        }

        var sortedStats = _exceptionStats.Values
            .OrderByDescending(s => s.Count)
            .ToList();

        _logger.ExceptionAggregationReport(sortedStats.Count, sortedStats.Sum(s => s.Count));

        // 报告前10个最频繁的异常
        var topExceptions = sortedStats.Take(10);
        foreach (var stat in topExceptions)
        {
            if (stat.Count > 100)
            {
                _logger.HighFrequencyException(stat.ExceptionType, stat.Context, stat.Count);
            }
            else
            {
                _logger.ExceptionStatistics(stat.ExceptionType, stat.Context, stat.Count, stat.IsRetryable);
            }
        }

        // 清理旧统计数据（保留最近的数据）
        var cutoffTime = _clock.UtcNow.AddHours(-1);
        var keysToRemove = _exceptionStats
            .Where(kvp => kvp.Value.LastOccurrence < cutoffTime && kvp.Value.Count < 5)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _exceptionStats.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 异常记录
    /// Exception record
    /// </summary>
    private sealed class ExceptionRecord
    {
        public required Exception Exception { get; init; }
        public required string Context { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string ExceptionType { get; init; }
        public required string Message { get; init; }
        public string? StackTrace { get; init; }
    }

    /// <summary>
    /// 异常统计信息
    /// Exception statistics
    /// </summary>
    public sealed class ExceptionStatistics
    {
        public required string ExceptionType { get; init; }
        public required string Context { get; init; }
        public long Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public string? LastMessage { get; set; }
        public bool IsRetryable { get; set; }
    }
}
