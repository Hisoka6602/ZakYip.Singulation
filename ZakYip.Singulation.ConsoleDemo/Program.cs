using csLTDMC;
using System.Globalization;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Registry;
using ZakYip.Singulation.Drivers.Leadshine;
using static System.Net.Mime.MediaTypeNames;
using ZakYip.Singulation.Drivers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using static System.Runtime.CompilerServices.RuntimeHelpers;

internal static class Program {

    private static void Main(string[] args) {
        // ===== 可按需修改的运行参数 =====
        ushort cardNo = 8;
        ushort portNo = 2;
        string? controllerIp = "192.168.5.11";      // 有网口就填 IP，直连卡可为 null
        string vendor = "Leadshine";      // 未来接入别的厂商时，改这个即可

        // 速度与加减速（线速度为一等公民）
        decimal targetMmps = 1500m;       // 目标线速度 mm/s
        decimal accelMmps2 = 1000m;       // 加速度 mm/s²
        decimal decelMmps2 = 1000m;       // 减速度 mm/s²

        // 机械参数（按你的默认）
        decimal drumDiameterMm = 79m;
        decimal gearRatio = 0.4m;

        int? overrideAxisCount = null;    // =null 从总线读轴数；否则用指定数量
                                          // =================================

        // ===== 组装 DI 容器 =====
        var services = new ServiceCollection();

        // 事件聚合器（把多轴事件统一转发）
        services.AddSingleton<IAxisEventAggregator, AxisEventAggregator>();

        // Registry：把厂商名映射到对应的 IAxisDrive 工厂
        // 如果你已实现 DriveRegistry，请引用你的命名空间；下面这行假设你把实现放在 Drivers.Common
        services.AddSingleton<IDriveRegistry>(sp => {
            var r = new DefaultDriveRegistry();
            r.Register("leadshine", (axisId, port, opts) => new LeadshineLtdmcAxisDrive(opts));
            // 未来在这里再注册其它品牌：
            // r.Register("Inovance", (axisId, port, opts) => new InovanceAxisDrive(opts));
            return r;
        });

        // BusAdapter：控制器/总线级操作（初始化、读轴数、冷复位等）
        services.AddSingleton<IBusAdapter>(sp => new LeadshineLtdmcBusAdapter(cardNo, portNo, controllerIp));

        // 轴群编排器：用 Bus + Registry 批量创建/管理多根轴
        services.AddSingleton<IAxisController, AxisController>();

        var provider = services.BuildServiceProvider();

        // ===== 订阅聚合事件（只订一次）=====
        var aggregator = provider.GetRequiredService<IAxisEventAggregator>();
        aggregator.CommandIssued += (_, e) =>
            Console.WriteLine($"[CMD][{e.Timestamp:HH:mm:ss.fff}][Axis:{e.Axis}] {e}{(e.Note is null ? "" : $" // {e.Note}")}");
        aggregator.SpeedFeedback += (_, e) =>
            Console.WriteLine($"[SPD][Axis:{e.Axis}] rpm={e.Rpm,6}  mm/s={e.SpeedMps,8:F2}  pps={e.PulsesPerSec,8:F2}");
        aggregator.AxisFaulted += (_, e) =>
            Console.WriteLine($"[ERR][Axis:{e.Axis}] {e.Exception.Message}");
        aggregator.AxisDisconnected += (_, e) =>
            Console.WriteLine($"[DISC][Axis:{e.Axis}] {e.Reason}");
        aggregator.DriverNotLoaded += (_, e) =>
            Console.WriteLine($"[DRV][{e.LibraryName}] {e.Message}");

        // ===== 运行主流程 =====
        var controller = provider.GetRequiredService<IAxisController>();

        // 模板选项：每根轴会复制一份并覆盖 NodeId
        var template = new DriverOptions {
            Card = cardNo,
            Port = portNo,
            PulleyPitchDiameterMm = drumDiameterMm,
            GearRatio = gearRatio,
            EnableHealthMonitor = true,
            HealthPingInterval = TimeSpan.FromMilliseconds(50),
            ConsecutiveFailThreshold = 3,
            NodeId = 0
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        try {
            // 1) 初始化总线 + 按轴数创建/Attach 驱动
            controller.InitializeAsync(vendor, template, overrideAxisCount, cts.Token)
                      .GetAwaiter().GetResult();

            // 2) 统一上电 & 配置加减速
            controller.EnableAllAsync(cts.Token).GetAwaiter().GetResult();
            controller.SetAccelDecelAllAsync(accelMmps2, decelMmps2, cts.Token)
                      .GetAwaiter().GetResult();

            // 3) 统一设速
            controller.WriteSpeedAllAsync(targetMmps, cts.Token).GetAwaiter().GetResult();

            Console.WriteLine("Running... 按 Ctrl+C 停止。");

            // 4) 简单等待：直到 Ctrl+C
            while (!cts.IsCancellationRequested) {
                Thread.Sleep(100);
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"[FATAL] {ex}");
        }
        finally {
            // 停机并释放
            try {
                controller.StopAllAsync(CancellationToken.None).GetAwaiter().GetResult();
                controller.DisposeAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { /* 忽略收尾异常 */ }
            provider?.Dispose();
        }
    }
}