using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Infrastructure.Workers;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 日志清理配置测试
/// </summary>
public class LogsCleanupOptionsTests
{
    [MiniFact]
    public void LogsCleanupOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new LogsCleanupOptions();

        // Assert
        MiniAssert.Equal(2, options.MainLogRetentionDays, "MainLogRetentionDays default should be 2");
        MiniAssert.Equal(2, options.HighFreqLogRetentionDays, "HighFreqLogRetentionDays default should be 2");
        MiniAssert.Equal(2, options.ErrorLogRetentionDays, "ErrorLogRetentionDays default should be 2");
    }

    [MiniFact]
    public void LogsCleanupOptions_CanBeConfiguredFromJson()
    {
        // Arrange
        var json = @"
        {
            ""LogsCleanup"": {
                ""MainLogRetentionDays"": 7,
                ""HighFreqLogRetentionDays"": 3,
                ""ErrorLogRetentionDays"": 30
            }
        }";

        var configPath = Path.Combine(Path.GetTempPath(), $"appsettings-test-{Guid.NewGuid()}.json");
        File.WriteAllText(configPath, json);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();

            var services = new ServiceCollection();
            services.Configure<LogsCleanupOptions>(configuration.GetSection("LogsCleanup"));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<LogsCleanupOptions>>().Value;

            // Assert
            MiniAssert.Equal(7, options.MainLogRetentionDays, "MainLogRetentionDays should be 7");
            MiniAssert.Equal(3, options.HighFreqLogRetentionDays, "HighFreqLogRetentionDays should be 3");
            MiniAssert.Equal(30, options.ErrorLogRetentionDays, "ErrorLogRetentionDays should be 30");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [MiniFact]
    public void LogsCleanupOptions_WithPartialConfig_UsesDefaults()
    {
        // Arrange
        var json = @"
        {
            ""LogsCleanup"": {
                ""MainLogRetentionDays"": 10
            }
        }";

        var configPath = Path.Combine(Path.GetTempPath(), $"appsettings-test-{Guid.NewGuid()}.json");
        File.WriteAllText(configPath, json);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();

            var services = new ServiceCollection();
            services.Configure<LogsCleanupOptions>(configuration.GetSection("LogsCleanup"));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<LogsCleanupOptions>>().Value;

            // Assert
            MiniAssert.Equal(10, options.MainLogRetentionDays, "MainLogRetentionDays should be 10");
            MiniAssert.Equal(2, options.HighFreqLogRetentionDays, "HighFreqLogRetentionDays should use default of 2");
            MiniAssert.Equal(2, options.ErrorLogRetentionDays, "ErrorLogRetentionDays should use default of 2");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [MiniFact]
    public void LogsCleanupOptions_WithMissingSection_UsesDefaults()
    {
        // Arrange
        var json = @"
        {
            ""OtherSection"": {
                ""SomeValue"": 123
            }
        }";

        var configPath = Path.Combine(Path.GetTempPath(), $"appsettings-test-{Guid.NewGuid()}.json");
        File.WriteAllText(configPath, json);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();

            var services = new ServiceCollection();
            services.Configure<LogsCleanupOptions>(configuration.GetSection("LogsCleanup"));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<LogsCleanupOptions>>().Value;

            // Assert
            MiniAssert.Equal(2, options.MainLogRetentionDays, "MainLogRetentionDays should use default of 2");
            MiniAssert.Equal(2, options.HighFreqLogRetentionDays, "HighFreqLogRetentionDays should use default of 2");
            MiniAssert.Equal(2, options.ErrorLogRetentionDays, "ErrorLogRetentionDays should use default of 2");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }
}
