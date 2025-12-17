using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ZakYip.Singulation.Core.Abstractions;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// EMC 复位协调器，负责发送和接收跨进程复位通知。
    /// <para>
    /// 使用内存映射文件（Memory-Mapped File）作为 IPC 机制，允许多个进程共享复位通知。
    /// </para>
    /// </summary>
    public sealed class EmcResetCoordinator : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly ISystemClock _clock;
        private readonly ushort _cardNo;
        private readonly string _mmfName;
        private readonly Timer? _pollingTimer;
        private MemoryMappedFile? _mmf;
        private bool _disposed;
        private string? _lastNotificationHash;

        /// <summary>
        /// 当接收到其他进程的复位通知时触发。
        /// </summary>
        public event EventHandler<EmcResetEventArgs>? ResetNotificationReceived;

        /// <summary>
        /// 初始化一个新的 EMC 复位协调器。
        /// </summary>
        /// <param name="cardNo">控制器卡号。</param>
        /// <param name="clock">系统时钟。</param>
        /// <param name="enablePolling">是否启用轮询接收通知（默认启用）。</param>
        /// <param name="pollingInterval">轮询间隔（默认 500ms）。</param>
        public EmcResetCoordinator(ushort cardNo, ISystemClock clock, bool enablePolling = true, TimeSpan? pollingInterval = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _cardNo = cardNo;
            _mmfName = $"Global\\ZakYip_EMC_Reset_Card{cardNo}";

            // 检查平台是否支持命名内存映射文件（仅 Windows）
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // 创建或打开内存映射文件（4KB 足够传输通知）
                    _mmf = MemoryMappedFile.CreateOrOpen(_mmfName, 4096, MemoryMappedFileAccess.ReadWrite);
                    _logger.Info($"[EMC Coordinator] 内存映射文件已创建/打开: {_mmfName}");
                }
                catch (Exception ex) // Intentional: MMF creation failure should not crash the component
                {
                    _logger.Error(ex, $"[EMC Coordinator] 创建内存映射文件失败: {_mmfName}");
                    _mmf = null;
                }
            }
            else
            {
                _logger.Warn($"[EMC Coordinator] 当前平台不支持命名内存映射文件，复位通知功能将被禁用: {_mmfName}");
                _mmf = null;
            }

            // 启动轮询接收通知
            if (enablePolling && _mmf != null)
            {
                var interval = pollingInterval ?? TimeSpan.FromMilliseconds(500);
                _pollingTimer = new Timer(PollForNotifications, null, interval, interval);
                _logger.Info($"[EMC Coordinator] 通知轮询已启动，间隔: {interval.TotalMilliseconds}ms");
            }
        }

        /// <summary>
        /// 发送复位通知到其他进程。
        /// </summary>
        /// <param name="resetType">复位类型。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task BroadcastResetNotificationAsync(EmcResetType resetType, CancellationToken ct = default)
        {
            if (_disposed || _mmf == null)
            {
                _logger.Warn("[EMC Coordinator] 协调器已释放或未初始化，无法发送通知");
                return;
            }

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var notification = new EmcResetNotification(
                    _cardNo,
                    resetType,
                    currentProcess.Id,
                    currentProcess.ProcessName,
                    _clock.UtcNow
                );

                var serialized = notification.Serialize();
                var bytes = Encoding.UTF8.GetBytes(serialized);

                await Task.Run(() =>
                {
                    using var accessor = _mmf.CreateViewAccessor(0, 4096, MemoryMappedFileAccess.Write);
                    
                    // 写入数据长度（前 4 字节）
                    accessor.Write(0, bytes.Length);
                    
                    // 写入数据内容
                    accessor.WriteArray(4, bytes, 0, bytes.Length);
                    
                    // 强制刷新到物理内存
                    accessor.Flush();

                }, ct);

                _logger.Info($"[EMC Coordinator] 复位通知已广播: 卡号={_cardNo}, 类型={resetType}, 进程={currentProcess.ProcessName}({currentProcess.Id})");
            }
            catch (Exception ex) // Intentional: Broadcast failure should not crash the coordinator
            {
                _logger.Error(ex, "[EMC Coordinator] 广播复位通知失败");
            }
        }

        /// <summary>
        /// 轮询接收来自其他进程的通知。
        /// </summary>
        private void PollForNotifications(object? state)
        {
            if (_disposed || _mmf == null)
                return;

            try
            {
                using var accessor = _mmf.CreateViewAccessor(0, 4096, MemoryMappedFileAccess.Read);

                // 读取数据长度
                var length = accessor.ReadInt32(0);
                if (length <= 0 || length > 4000)
                    return; // 无效数据

                // 读取数据内容
                var bytes = new byte[length];
                accessor.ReadArray(4, bytes, 0, length);

                var serialized = Encoding.UTF8.GetString(bytes);
                
                // 检查是否是新通知（避免重复处理）
                var hash = serialized.GetHashCode().ToString();
                if (hash == _lastNotificationHash)
                    return;

                _lastNotificationHash = hash;

                var notification = EmcResetNotification.Deserialize(serialized);
                if (notification == null)
                {
                    _logger.Warn($"[EMC Coordinator] 反序列化通知失败: {serialized}");
                    return;
                }

                // 忽略自己发送的通知
                var currentProcessId = Process.GetCurrentProcess().Id;
                if (notification.ProcessId == currentProcessId)
                    return;

                // 检查通知是否过期（超过 30 秒视为过期）
                var age = _clock.UtcNow - notification.Timestamp;
                if (age.TotalSeconds > 30)
                {
                    _logger.Debug($"[EMC Coordinator] 忽略过期通知: 年龄={age.TotalSeconds:F1}秒");
                    return;
                }

                _logger.Info($"[EMC Coordinator] 收到复位通知: 卡号={notification.CardNo}, 类型={notification.ResetType}, 来源进程={notification.ProcessName}({notification.ProcessId})");

                // 触发事件
                ResetNotificationReceived?.Invoke(this, new EmcResetEventArgs(notification));
            }
            catch (Exception ex) // Intentional: Polling failure should not crash the timer thread
            {
                _logger.Error(ex, "[EMC Coordinator] 轮询通知失败");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _pollingTimer?.Dispose();
            _mmf?.Dispose();
            _disposed = true;

            _logger.Debug($"[EMC Coordinator] 协调器已释放: {_mmfName}");
        }
    }
}
