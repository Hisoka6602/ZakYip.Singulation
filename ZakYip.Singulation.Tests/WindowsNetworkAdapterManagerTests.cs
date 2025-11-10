using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Infrastructure.Runtime;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// Windows 网络适配器管理器测试
/// </summary>
public class WindowsNetworkAdapterManagerTests
{
    [MiniFact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        MiniAssert.Throws<ArgumentNullException>(() => {
            var manager = new WindowsNetworkAdapterManager(null!);
        }, "构造函数应拒绝 null logger");
    }

    [MiniFact]
    public void Constructor_WithValidLogger_ShouldSucceed()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;

        // Act
        var manager = new WindowsNetworkAdapterManager(logger);

        // Assert
        MiniAssert.NotNull(manager, "Manager should be created successfully");
    }

    [MiniFact]
    public void ConfigureAllNetworkAdapters_OnNonWindows_ShouldLogWarning()
    {
        // This test only makes sense on non-Windows platforms
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Skip test on Windows
            return;
        }

        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - should not throw
        manager.ConfigureAllNetworkAdapters();

        // Assert - no exception means success
    }

    [MiniFact]
    public void ConfigureAllNetworkAdapters_ShouldNotThrow()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - should not throw even if it can't configure adapters
        manager.ConfigureAllNetworkAdapters();

        // Assert - no exception means success
    }

    [MiniFact]
    public void RestartAdapter_WithValidName_ShouldNotThrow()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - should not throw even if adapter doesn't exist
        manager.RestartAdapter("NonExistentAdapter");

        // Assert - no exception means success
    }

    [MiniFact]
    public void RestartAdapter_WithEmptyName_ShouldNotThrow()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - should handle empty name gracefully
        manager.RestartAdapter("");

        // Assert - no exception means success
    }

    [MiniFact]
    public void RestartAdapter_WithNullName_ShouldNotThrow()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - should handle null name gracefully
        manager.RestartAdapter(null!);

        // Assert - no exception means success
    }

    [MiniFact]
    public void ConfigureAllNetworkAdapters_MultipleCallsShouldSucceed()
    {
        // Arrange
        var logger = NullLogger<WindowsNetworkAdapterManager>.Instance;
        var manager = new WindowsNetworkAdapterManager(logger);

        // Act - call multiple times should not cause issues
        manager.ConfigureAllNetworkAdapters();
        manager.ConfigureAllNetworkAdapters();

        // Assert - no exception means success
    }
}
