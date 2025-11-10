using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Infrastructure.Runtime;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// Windows 防火墙管理器测试
/// </summary>
public class WindowsFirewallManagerTests
{
    [MiniFact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        MiniAssert.Throws<ArgumentNullException>(() => {
            var manager = new WindowsFirewallManager(null!);
        }, "构造函数应拒绝 null logger");
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithValidUrls_ShouldReturnPorts()
    {
        // Arrange
        var urls = new[] {
            "http://localhost:5005",
            "https://localhost:5006",
            "http://example.com:8080"
        };

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.Equal(3, ports.Length, "Should extract 3 ports");
        MiniAssert.Contains(ports, 5005, "Should contain port 5005");
        MiniAssert.Contains(ports, 5006, "Should contain port 5006");
        MiniAssert.Contains(ports, 8080, "Should contain port 8080");
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithoutExplicitPort_ShouldUseDefaultPorts()
    {
        // Arrange
        var urls = new[] {
            "http://localhost",
            "https://localhost"
        };

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.Equal(2, ports.Length, "Should extract 2 ports");
        MiniAssert.Contains(ports, 80, "HTTP should default to port 80");
        MiniAssert.Contains(ports, 443, "HTTPS should default to port 443");
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithDuplicatePorts_ShouldReturnDistinct()
    {
        // Arrange
        var urls = new[] {
            "http://localhost:5005",
            "http://example.com:5005",
            "http://test.com:5005"
        };

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.Equal(1, ports.Length, "Should return only one distinct port");
        MiniAssert.Equal(5005, ports[0], "Should be port 5005");
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithEmptyArray_ShouldReturnEmpty()
    {
        // Arrange
        var urls = Array.Empty<string>();

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.Equal(0, ports.Length, "Should return empty array");
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithInvalidUrls_ShouldIgnoreThem()
    {
        // Arrange
        var urls = new[] {
            "not-a-valid-url",
            "http://localhost:5005",
            "",
            null!
        };

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.Equal(1, ports.Length, "Should only extract valid port");
        MiniAssert.Equal(5005, ports[0], "Should be port 5005");
    }

    [MiniFact]
    public void CheckAndConfigureFirewall_OnNonWindows_ShouldLogWarning()
    {
        // This test only makes sense on non-Windows platforms
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Skip test on Windows
            return;
        }

        // Arrange
        var logger = NullLogger<WindowsFirewallManager>.Instance;
        var manager = new WindowsFirewallManager(logger);
        var ports = new[] { 5005 };

        // Act - should not throw
        manager.CheckAndConfigureFirewall("/path/to/app", "TestApp", ports);

        // Assert - no exception means success
    }

    [MiniFact]
    public void CheckAndConfigureFirewall_WithEmptyPorts_ShouldNotThrow()
    {
        // Arrange
        var logger = NullLogger<WindowsFirewallManager>.Instance;
        var manager = new WindowsFirewallManager(logger);
        var ports = Array.Empty<int>();

        // Act - should not throw
        manager.CheckAndConfigureFirewall("/path/to/app", "TestApp", ports);

        // Assert - no exception means success
    }

    [MiniFact]
    public void ExtractPortsFromUrls_WithMixedValidAndInvalidPorts_ShouldExtractValid()
    {
        // Arrange
        var urls = new[] {
            "http://localhost:80",
            "https://example.com:443",
            "http://test.com:8080",
            "invalid://broken:99999" // Invalid port range
        };

        // Act
        var ports = WindowsFirewallManager.ExtractPortsFromUrls(urls);

        // Assert
        MiniAssert.NotNull(ports, "Ports should not be null");
        MiniAssert.True(ports.Length >= 3, "Should extract at least 3 valid ports");
        MiniAssert.Contains(ports, 80, "Should contain port 80");
        MiniAssert.Contains(ports, 443, "Should contain port 443");
        MiniAssert.Contains(ports, 8080, "Should contain port 8080");
    }
}
