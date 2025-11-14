using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Drivers.Leadshine;

namespace ZakYip.Singulation.Examples
{
    /// <summary>
    /// 演示 EMC 分布式锁和复位通知功能的示例。
    /// </summary>
    public class EmcDistributedLockExample
    {
        /// <summary>
        /// 示例 1：基本的锁使用模式。
        /// </summary>
        public static async Task Example1_BasicLockUsage()
        {
            Console.WriteLine("=== 示例 1：基本的锁使用模式 ===\n");

            // 创建分布式锁（基于卡号）
            using var resourceLock = new EmcNamedMutexLock("CardNo_0");

            try
            {
                // 尝试获取锁（30 秒超时）
                Console.WriteLine("尝试获取 EMC 资源锁...");
                var acquired = await resourceLock.TryAcquireAsync(TimeSpan.FromSeconds(30));

                if (acquired)
                {
                    Console.WriteLine("✓ 成功获取锁");

                    // 在这里执行需要独占访问的操作
                    Console.WriteLine("执行操作...");
                    await Task.Delay(2000); // 模拟操作

                    Console.WriteLine("操作完成");
                }
                else
                {
                    Console.WriteLine("✗ 无法获取锁（超时）");
                }
            }
            finally
            {
                // 释放锁
                resourceLock.Release();
                Console.WriteLine("锁已释放\n");
            }
        }

        /// <summary>
        /// 示例 2：使用总线适配器的集成功能。
        /// </summary>
        public static async Task Example2_BusAdapterIntegration()
        {
            Console.WriteLine("=== 示例 2：使用总线适配器的集成功能 ===\n");

            // 创建总线适配器（自动包含分布式锁和复位协调器）
            var adapter = new LeadshineLtdmcBusAdapter(
                cardNo: 0,
                portNo: 2,
                controllerIp: "192.168.1.100"
            );

            // 订阅复位通知事件
            adapter.EmcResetNotificationReceived += (sender, args) =>
            {
                var notification = args.Notification;
                Console.WriteLine($"\n⚠️  收到复位通知！");
                Console.WriteLine($"   类型: {notification.ResetType}");
                Console.WriteLine($"   来源: {notification.ProcessName} (PID: {notification.ProcessId})");
                Console.WriteLine($"   预计恢复时间: {notification.EstimatedRecoverySeconds} 秒");
                Console.WriteLine("   正在采取应对措施...\n");

                // 在这里实现应对策略
                // - 暂停所有操作
                // - 保存状态
                // - 等待复位完成
                // - 重新初始化
            };

            try
            {
                // 初始化总线
                Console.WriteLine("初始化总线...");
                var initResult = await adapter.InitializeAsync();
                if (initResult.Key)
                {
                    Console.WriteLine($"✓ 初始化成功: {initResult.Value}");
                }
                else
                {
                    Console.WriteLine($"✗ 初始化失败: {initResult.Value}");
                    return;
                }

                // 执行冷复位（会自动处理锁和通知）
                Console.WriteLine("\n执行冷复位（会自动获取锁并广播通知）...");
                await adapter.ResetAsync();
                Console.WriteLine("✓ 冷复位完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 操作失败: {ex.Message}");
            }
            finally
            {
                // 关闭总线
                await adapter.CloseAsync();
                Console.WriteLine("总线已关闭\n");
            }
        }

        /// <summary>
        /// 示例 3：实现应对复位通知的策略（使用自动重新连接功能）。
        /// </summary>
        public static async Task Example3_ResetHandlingStrategy()
        {
            Console.WriteLine("=== 示例 3：实现应对复位通知的策略（使用自动重新连接）===\n");

            var adapter = new LeadshineLtdmcBusAdapter(0, 2, "192.168.1.100");

            // 状态管理
            var isOperating = false;
            var savedState = new object(); // 假设的状态对象

            // 订阅重新连接开始事件
            adapter.ReconnectionStarting += (sender, e) =>
            {
                Console.WriteLine("\n⚠️  重新连接开始 - 所有操作将被阻止");
                
                // 步骤 1：暂停所有操作
                if (isOperating)
                {
                    Console.WriteLine("   [1/3] 暂停所有操作...");
                    isOperating = false;
                    // 实际代码：await StopAllAxes();
                }

                // 步骤 2：保存当前状态
                Console.WriteLine("   [2/3] 保存当前状态...");
                // 实际代码：await SaveStateAsync(savedState);
                
                // 步骤 3：标记为"等待恢复"
                Console.WriteLine("   [3/3] 标记为等待恢复状态...");
            };
            
            // 订阅重新连接完成事件
            adapter.ReconnectionCompleted += (sender, e) =>
            {
                Console.WriteLine("\n✓ 重新连接完成 - 操作已解除阻止");
                
                // 恢复状态并继续操作
                Console.WriteLine("   恢复状态并继续操作...");
                // 实际代码：await RestoreStateAsync(savedState);
                isOperating = true;
            };

            // 订阅复位通知（可选，用于额外的日志记录）
            adapter.EmcResetNotificationReceived += (sender, args) =>
            {
                var notification = args.Notification;
                Console.WriteLine($"\n⚠️  收到复位通知 - {notification.ResetType}");
                Console.WriteLine($"    来源: {notification.ProcessName}({notification.ProcessId})");
                Console.WriteLine($"    预计恢复时间: {notification.EstimatedRecoverySeconds} 秒");
                Console.WriteLine("    适配器将自动处理重新连接...");
            };

            Console.WriteLine("应对策略已配置，等待复位通知...");
            Console.WriteLine("（适配器会自动处理 Close → Wait → Reconnect，期间所有操作被阻止）\n");

            // 清理
            await adapter.CloseAsync();
        }

        /// <summary>
        /// 示例 4：使用复位协调器直接发送和接收通知。
        /// </summary>
        public static async Task Example4_DirectCoordinatorUsage()
        {
            Console.WriteLine("=== 示例 4：直接使用复位协调器 ===\n");

            using var coordinator = new EmcResetCoordinator(
                cardNo: 0,
                enablePolling: true,
                pollingInterval: TimeSpan.FromMilliseconds(500)
            );

            // 订阅通知
            coordinator.ResetNotificationReceived += (sender, args) =>
            {
                Console.WriteLine($"收到通知: {args.Notification.ResetType} 从 {args.Notification.ProcessName}");
            };

            // 广播复位通知
            Console.WriteLine("广播热复位通知...");
            await coordinator.BroadcastResetNotificationAsync(EmcResetType.Warm, CancellationToken.None);
            Console.WriteLine("✓ 通知已发送");

            // 等待一段时间以接收可能的响应
            Console.WriteLine("等待可能的响应（1 秒）...");
            await Task.Delay(1000);
            Console.WriteLine("示例完成\n");
        }

        /// <summary>
        /// 示例 5：多实例协同场景。
        /// </summary>
        public static void Example5_MultiInstanceScenario()
        {
            Console.WriteLine("=== 示例 5：多实例协同场景 ===\n");

            Console.WriteLine("场景描述：");
            Console.WriteLine("  实例 A (进程 1234) ───┐");
            Console.WriteLine("  实例 B (进程 5678) ───┼──→ EMC 控制器 (CardNo: 0)");
            Console.WriteLine("  实例 C (进程 9012) ───┘");
            Console.WriteLine();

            Console.WriteLine("步骤：");
            Console.WriteLine("1. 实例 A 需要执行冷复位");
            Console.WriteLine("   → 调用 adapter.ResetAsync()");
            Console.WriteLine();

            Console.WriteLine("2. 实例 A 自动执行：");
            Console.WriteLine("   → 获取分布式锁（确保独占访问）");
            Console.WriteLine("   → 广播复位通知到内存映射文件");
            Console.WriteLine("   → 等待 500ms（给其他实例准备时间）");
            Console.WriteLine("   → 执行冷复位操作");
            Console.WriteLine("   → 等待 10 秒恢复");
            Console.WriteLine("   → 重新初始化");
            Console.WriteLine("   → 释放分布式锁");
            Console.WriteLine();

            Console.WriteLine("3. 实例 B 和 C 自动响应：");
            Console.WriteLine("   → 通过轮询检测到复位通知");
            Console.WriteLine("   → 触发 EmcResetNotificationReceived 事件");
            Console.WriteLine("   → 触发 ReconnectionStarting 事件（应用程序暂停操作并保存状态）");
            Console.WriteLine("   → **所有雷赛方法调用和 IO 监控被自动阻止**");
            Console.WriteLine("   → 自动关闭当前连接");
            Console.WriteLine("   → 等待恢复时间（冷复位: 15秒，热复位: 2秒）");
            Console.WriteLine("   → 自动重新初始化连接");
            Console.WriteLine("   → 触发 ReconnectionCompleted 事件（应用程序恢复操作）");
            Console.WriteLine("   → **解除所有操作阻止**");
            Console.WriteLine();

            Console.WriteLine("结果：");
            Console.WriteLine("✓ 所有实例协同完成复位");
            Console.WriteLine("✓ 无数据丢失");
            Console.WriteLine("✓ 重新连接期间操作被自动阻止，避免冲突");
            Console.WriteLine("✓ 系统平滑过渡\n");
        }

        /// <summary>
        /// 运行所有示例。
        /// </summary>
        public static async Task RunAllExamples()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   EMC 分布式锁和复位通知功能示例                           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            try
            {
                await Example1_BasicLockUsage();
                // await Example2_BusAdapterIntegration(); // 需要实际硬件
                // await Example3_ResetHandlingStrategy(); // 需要实际硬件
                // await Example4_DirectCoordinatorUsage(); // 需要实际硬件
                Example5_MultiInstanceScenario();

                Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║   所有示例运行完成                                         ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ 示例运行失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
