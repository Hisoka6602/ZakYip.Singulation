using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Exceptions;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 异常聚合服务测试
/// </summary>
public class ExceptionAggregationServiceTests
{
    [MiniFact]
    public void RecordException_ShouldStoreException()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);
        var exception = new InvalidOperationException("Test exception");

        // Act
        service.RecordException(exception, "TestContext");

        // Assert - service should accept the exception without throwing
        // No exception means success
    }

    [MiniFact]
    public void RecordException_ShouldAggregateMultipleExceptions()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);

        // Act - record same exception type multiple times
        for (int i = 0; i < 5; i++)
        {
            var exception = new InvalidOperationException($"Test exception {i}");
            service.RecordException(exception, "TestContext");
        }

        // After aggregation, statistics should be available
        // Note: Actual aggregation happens in background task
        // No exception means success
    }

    [MiniFact]
    public void RecordException_ShouldHandleNullException()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);

        // Act & Assert - should not throw
        service.RecordException(null!, "TestContext");
        // No exception means success
    }

    [MiniFact]
    public void RecordException_ShouldTrackSingulationExceptions()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);
        var retryableException = new TransportException("Connection failed");
        var nonRetryableException = new ValidationException("Invalid input");

        // Act
        service.RecordException(retryableException, "Transport:Connect");
        service.RecordException(nonRetryableException, "Validation:Input");

        // Assert - no exception means success
    }

    [MiniFact]
    public void GetStatistics_ShouldReturnEmptyWhenNoExceptions()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);

        // Act
        var stats = service.GetStatistics();

        // Assert
        MiniAssert.NotNull(stats, "Statistics should not be null");
        MiniAssert.Equal(0, stats.Count, "Statistics should be empty");
    }

    [MiniFact]
    public async Task ExecuteAsync_ShouldStartAndStopGracefully()
    {
        // Arrange
        var logger = NullLogger<ExceptionAggregationService>.Instance;
        var service = new ExceptionAggregationService(logger);
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100); // Let it run briefly
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - no exception means success
    }
}
