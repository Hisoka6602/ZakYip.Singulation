using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// Windows 防火墙管理工具类
    /// 用于检测和配置 Windows 防火墙规则
    /// </summary>
    public class WindowsFirewallManager {
        private readonly ILogger<WindowsFirewallManager> _logger;

        public WindowsFirewallManager(ILogger<WindowsFirewallManager> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检查并配置防火墙
        /// </summary>
        /// <param name="applicationPath">应用程序路径</param>
        /// <param name="applicationName">应用程序名称</param>
        /// <param name="ports">需要开放的端口列表</param>
        public void CheckAndConfigureFirewall(string applicationPath, string applicationName, int[] ports) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                _logger.LogWarning("当前操作系统不是 Windows，跳过防火墙配置");
                return;
            }

            try {
                _logger.LogInformation("开始检查 Windows 防火墙配置...");

                // 检查防火墙状态
                bool isFirewallEnabled = IsFirewallEnabled();
                _logger.LogInformation($"防火墙状态: {(isFirewallEnabled ? "已启用" : "已禁用")}");

                if (isFirewallEnabled) {
                    _logger.LogInformation("检测到防火墙已启用，正在尝试禁用...");
                    DisableFirewall();
                }

                // 检查并添加防火墙规则
                foreach (var port in ports) {
                    CheckAndAddFirewallRule(applicationName, port);
                }

                _logger.LogInformation("防火墙配置检查完成");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "配置防火墙时发生错误");
            }
        }

        /// <summary>
        /// 检查防火墙是否已启用
        /// </summary>
        private bool IsFirewallEnabled() {
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "netsh",
                    Arguments = "advfirewall show allprofiles state",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) {
                    _logger.LogWarning("无法启动 netsh 进程");
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 检查输出中是否包含 "ON"
                return output.Contains("ON", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "检查防火墙状态时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 禁用防火墙
        /// </summary>
        private void DisableFirewall() {
            try {
                var profiles = new[] { "domainprofile", "privateprofile", "publicprofile" };

                foreach (var profile in profiles) {
                    var startInfo = new ProcessStartInfo {
                        FileName = "netsh",
                        Arguments = $"advfirewall set {profile} state off",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas" // 需要管理员权限
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null) {
                        _logger.LogWarning($"无法启动 netsh 进程以禁用 {profile}");
                        continue;
                    }

                    process.WaitForExit();

                    if (process.ExitCode == 0) {
                        _logger.LogInformation($"成功禁用 {profile} 防火墙");
                    }
                    else {
                        string error = process.StandardError.ReadToEnd();
                        _logger.LogWarning($"禁用 {profile} 防火墙失败: {error}");
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "禁用防火墙时发生错误");
            }
        }

        /// <summary>
        /// 检查并添加防火墙规则
        /// </summary>
        private void CheckAndAddFirewallRule(string applicationName, int port) {
            try {
                string ruleName = $"{applicationName}_Port_{port}";

                // 检查入站规则是否存在
                if (!IsRuleExists(ruleName, "in")) {
                    _logger.LogInformation($"入站规则 '{ruleName}' 不存在，正在添加...");
                    AddFirewallRule(ruleName, port, "in");
                }
                else {
                    _logger.LogInformation($"入站规则 '{ruleName}' 已存在");
                }

                // 检查出站规则是否存在
                if (!IsRuleExists(ruleName, "out")) {
                    _logger.LogInformation($"出站规则 '{ruleName}' 不存在，正在添加...");
                    AddFirewallRule(ruleName, port, "out");
                }
                else {
                    _logger.LogInformation($"出站规则 '{ruleName}' 已存在");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"检查或添加端口 {port} 的防火墙规则时发生错误");
            }
        }

        /// <summary>
        /// 检查防火墙规则是否存在
        /// </summary>
        private bool IsRuleExists(string ruleName, string direction) {
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\" dir={direction}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) {
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 如果规则存在，输出会包含规则名称
                return output.Contains($"规则名称", StringComparison.OrdinalIgnoreCase) ||
                       output.Contains($"Rule Name", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"检查规则 '{ruleName}' 是否存在时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 添加防火墙规则
        /// </summary>
        private void AddFirewallRule(string ruleName, int port, string direction) {
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action=allow protocol=TCP localport={port}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // 需要管理员权限
                };

                using var process = Process.Start(startInfo);
                if (process == null) {
                    _logger.LogWarning($"无法启动 netsh 进程以添加规则 '{ruleName}'");
                    return;
                }

                process.WaitForExit();

                if (process.ExitCode == 0) {
                    _logger.LogInformation($"成功添加防火墙规则: {ruleName} (端口: {port}, 方向: {direction})");
                }
                else {
                    string error = process.StandardError.ReadToEnd();
                    _logger.LogWarning($"添加防火墙规则失败: {error}");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"添加防火墙规则 '{ruleName}' 时发生错误");
            }
        }

        /// <summary>
        /// 从 URL 中提取端口号
        /// </summary>
        public static int[] ExtractPortsFromUrls(string[] urls) {
            return urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => {
                    try {
                        var uri = new Uri(url);
                        return uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);
                    }
                    catch {
                        return -1;
                    }
                })
                .Where(port => port > 0)
                .Distinct()
                .ToArray();
        }
    }
}
