using System;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace ZakYip.Singulation.Infrastructure.Runtime
{
    /// <summary>
    /// Windows 网卡配置管理工具类
    /// 用于配置网卡的巨帧和传输缓存等高级设置
    /// </summary>
    public class WindowsNetworkAdapterManager
    {
        private readonly ILogger<WindowsNetworkAdapterManager> _logger;

        public WindowsNetworkAdapterManager(ILogger<WindowsNetworkAdapterManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 配置所有网卡：启用巨帧和最大化传输缓存
        /// </summary>
        public void ConfigureAllNetworkAdapters()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("当前操作系统不是 Windows，跳过网卡配置");
                return;
            }

            try
            {
                _logger.LogInformation("开始配置网络适配器...");

                // 获取所有网卡
                var adapters = GetNetworkAdapters();
                if (adapters.Length == 0)
                {
                    _logger.LogWarning("未找到任何网络适配器");
                    return;
                }

                _logger.LogInformation($"找到 {adapters.Length} 个网络适配器");

                foreach (var adapter in adapters)
                {
                    ConfigureAdapter(adapter);
                }

                _logger.LogInformation("网络适配器配置完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置网络适配器时发生错误");
            }
        }

        /// <summary>
        /// 获取所有网络适配器名称
        /// </summary>
        private string[] GetNetworkAdapters()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Select-Object -ExpandProperty Name\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning("无法启动 PowerShell 进程");
                    return Array.Empty<string>();
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning($"获取网络适配器列表失败，退出代码: {process.ExitCode}");
                    return Array.Empty<string>();
                }

                return output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取网络适配器列表时发生错误");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 配置单个网卡
        /// </summary>
        private void ConfigureAdapter(string adapterName)
        {
            try
            {
                _logger.LogInformation($"正在配置网络适配器: {adapterName}");

                // 启用巨帧
                EnableJumboFrames(adapterName);

                // 设置最大传输缓存
                MaximizeTransmitBuffers(adapterName);

                // 禁用节能功能
                DisablePowerManagement(adapterName);

                _logger.LogInformation($"网络适配器 '{adapterName}' 配置完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"配置网络适配器 '{adapterName}' 时发生错误");
            }
        }

        /// <summary>
        /// 启用巨帧 (Jumbo Frames)
        /// </summary>
        private void EnableJumboFrames(string adapterName)
        {
            try
            {
                // 常见的巨帧设置名称
                var jumboFrameProperties = new[] {
                    "*JumboPacket",
                    "JumboFrame",
                    "*JumboMTU"
                };

                // 尝试设置巨帧为 9014 字节（常见的最大值）
                var jumboFrameValue = "9014";

                foreach (var property in jumboFrameProperties)
                {
                    // 检查属性是否存在
                    if (CheckAdapterProperty(adapterName, property))
                    {
                        SetAdapterProperty(adapterName, property, jumboFrameValue);
                        _logger.LogInformation($"网络适配器 '{adapterName}' 的巨帧已设置为 {jumboFrameValue} 字节 (属性: {property})");
                        return; // 只要成功设置一个就退出
                    }
                }

                _logger.LogWarning($"网络适配器 '{adapterName}' 不支持巨帧或未找到相应属性");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"启用网络适配器 '{adapterName}' 的巨帧时发生错误");
            }
        }

        /// <summary>
        /// 最大化传输缓存
        /// </summary>
        private void MaximizeTransmitBuffers(string adapterName)
        {
            try
            {
                // 常见的传输缓存设置名称
                var transmitBufferProperties = new[] {
                    "*TransmitBuffers",
                    "NumTxBuffers",
                    "TransmitDescriptors"
                };

                foreach (var property in transmitBufferProperties)
                {
                    // 获取该属性的最大值并设置
                    var maxValue = GetAdapterPropertyMaxValue(adapterName, property);
                    if (!string.IsNullOrEmpty(maxValue))
                    {
                        SetAdapterProperty(adapterName, property, maxValue);
                        _logger.LogInformation($"网络适配器 '{adapterName}' 的传输缓存已设置为最大值 {maxValue} (属性: {property})");
                        return; // 只要成功设置一个就退出
                    }
                }

                _logger.LogWarning($"网络适配器 '{adapterName}' 未找到传输缓存属性或无法获取最大值");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"最大化网络适配器 '{adapterName}' 的传输缓存时发生错误");
            }
        }

        /// <summary>
        /// 检查网卡属性是否存在
        /// </summary>
        private bool CheckAdapterProperty(string adapterName, string propertyName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{propertyName}' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RegistryKeyword\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output) && process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"检查网络适配器 '{adapterName}' 的属性 '{propertyName}' 时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 获取网卡属性的最大值
        /// </summary>
        private string? GetAdapterPropertyMaxValue(string adapterName, string propertyName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{propertyName}' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ValidRegistryValues\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    return null;
                }

                // ValidRegistryValues 可能是一个数组，取最大值
                var values = output
                    .Split(new[] { '\r', '\n', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(v => int.TryParse(v.Trim(), out _))
                    .Select(v => int.Parse(v.Trim()))
                    .ToArray();

                if (values.Length > 0)
                {
                    return values.Max().ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"获取网络适配器 '{adapterName}' 的属性 '{propertyName}' 最大值时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 设置网卡属性
        /// </summary>
        private void SetAdapterProperty(string adapterName, string propertyName, string value)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Set-NetAdapterAdvancedProperty -Name '{adapterName}' -RegistryKeyword '{propertyName}' -RegistryValue '{value}' -ErrorAction Stop\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // 需要管理员权限
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning($"无法启动 PowerShell 进程以设置属性 '{propertyName}'");
                    return;
                }

                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    _logger.LogDebug($"成功设置网络适配器 '{adapterName}' 的属性 '{propertyName}' 为 '{value}'");
                }
                else
                {
                    _logger.LogWarning($"设置网络适配器属性失败: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置网络适配器 '{adapterName}' 的属性 '{propertyName}' 时发生错误");
            }
        }

        /// <summary>
        /// 禁用网卡节能功能
        /// </summary>
        private void DisablePowerManagement(string adapterName)
        {
            try
            {
                _logger.LogInformation($"正在禁用网络适配器 '{adapterName}' 的节能功能...");

                // 禁用"允许计算机关闭此设备以节省电源"
                DisableDevicePowerSaving(adapterName);

                // 禁用"允许此设备唤醒计算机"（可选）
                // DisableWakeOnLan(adapterName);

                // 禁用节能以太网 (Energy-Efficient Ethernet, EEE)
                DisableEnergyEfficientEthernet(adapterName);

                _logger.LogInformation($"网络适配器 '{adapterName}' 的节能功能已禁用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"禁用网络适配器 '{adapterName}' 的节能功能时发生错误");
            }
        }

        /// <summary>
        /// 禁用设备级别的电源节省
        /// </summary>
        private void DisableDevicePowerSaving(string adapterName)
        {
            try
            {
                // 使用 PowerShell 获取网卡的设备实例路径
                var getInstanceCmd = $@"
                    $adapter = Get-NetAdapter -Name '{adapterName}' -ErrorAction SilentlyContinue
                    if ($adapter) {{
                        $adapter | Get-NetAdapterPowerManagement | Select-Object -ExpandProperty AllowComputerToTurnOffDevice
                    }}
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{getInstanceCmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var checkProcess = Process.Start(startInfo);
                if (checkProcess != null)
                {
                    string output = checkProcess.StandardOutput.ReadToEnd();
                    checkProcess.WaitForExit();

                    // 如果当前状态是启用的（True），则禁用它
                    if (output.Contains("True", StringComparison.OrdinalIgnoreCase))
                    {
                        // 禁用电源管理
                        var disableCmd = $@"
                            $adapter = Get-NetAdapter -Name '{adapterName}' -ErrorAction SilentlyContinue
                            if ($adapter) {{
                                Disable-NetAdapterPowerManagement -Name '{adapterName}' -ErrorAction SilentlyContinue
                            }}
                        ";

                        var disableInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -Command \"{disableCmd}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Verb = "runas" // 需要管理员权限
                        };

                        using var disableProcess = Process.Start(disableInfo);
                        if (disableProcess != null)
                        {
                            disableProcess.WaitForExit();
                            if (disableProcess.ExitCode == 0)
                            {
                                _logger.LogInformation($"已禁用网络适配器 '{adapterName}' 的设备电源节省");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"网络适配器 '{adapterName}' 的设备电源节省已经是禁用状态");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"禁用网络适配器 '{adapterName}' 的设备电源节省时发生错误");
            }
        }

        /// <summary>
        /// 禁用节能以太网 (Energy-Efficient Ethernet, EEE)
        /// </summary>
        private void DisableEnergyEfficientEthernet(string adapterName)
        {
            try
            {
                // 常见的 EEE 设置名称
                var eeeProperties = new[] {
                    "*EEE",
                    "EnergyEfficientEthernet",
                    "*EEELinkAdvertisement"
                };

                foreach (var property in eeeProperties)
                {
                    // 检查属性是否存在
                    if (CheckAdapterProperty(adapterName, property))
                    {
                        // 尝试设置为 0 (禁用) 或 "Disabled"
                        var values = new[] { "0", "Disabled" };

                        foreach (var value in values)
                        {
                            try
                            {
                                SetAdapterProperty(adapterName, property, value);
                                _logger.LogInformation($"已禁用网络适配器 '{adapterName}' 的节能以太网 (属性: {property})");
                                return; // 成功设置后退出
                            }
                            catch
                            {
                                // 尝试下一个值
                                continue;
                            }
                        }
                    }
                }

                _logger.LogDebug($"网络适配器 '{adapterName}' 不支持节能以太网或已经禁用");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"禁用网络适配器 '{adapterName}' 的节能以太网时发生错误");
            }
        }

        /// <summary>
        /// 重启网络适配器（如果需要的话）
        /// </summary>
        public void RestartAdapter(string adapterName)
        {
            try
            {
                _logger.LogInformation($"正在重启网络适配器: {adapterName}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Restart-NetAdapter -Name '{adapterName}' -Confirm:$false\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // 需要管理员权限
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning($"无法启动 PowerShell 进程以重启网络适配器 '{adapterName}'");
                    return;
                }

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"网络适配器 '{adapterName}' 已重启");
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    _logger.LogWarning($"重启网络适配器失败: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"重启网络适配器 '{adapterName}' 时发生错误");
            }
        }
    }
}
