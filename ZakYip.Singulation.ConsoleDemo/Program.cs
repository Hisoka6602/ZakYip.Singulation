using csLTDMC;
using System.Globalization;
using System.Collections.Concurrent;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Leadshine;
using ZakYip.Singulation.Drivers.Simulated;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

internal static class Program {

    private static void Main(string[] args) {
        // ===== 手动配置区（改这些变量就行） =====
        int AxisCount = 3;           // 轴数
        double TargetRpm = 1200;        // 目标转速 (rpm)
        ushort CardNo = 8;           // 控制器卡号
        string ControllerIp = "192.168.5.11";          // 留空=本地初始化；填IP=以太网初始化，如 "192.168.1.10"
        double RpmTo60ffScale = 1.0;         // rpm → 0x60FF 单位比例（不清楚就先用 1.0）
        int HoldSeconds = 0;          // 运行保持时间（秒）；设 0 则按任意键停止
        // =====================================

        // 1) 初始化 LTDMC
        short initRet = string.IsNullOrWhiteSpace(ControllerIp)
            ? LTDMC.dmc_board_init()
            : LTDMC.dmc_board_init_eth(CardNo, ControllerIp);

        if (initRet != 0) {
            Console.WriteLine($"[ERR] LTDMC init failed, ret={initRet}");
            return;
        }

        try {
            // 2) 准备驱动（NodeID = 1000 + i，axisNo = i，EtherCAT端口=2 在驱动内部固定）
            var opts = new DriverOptions {
                MaxRpm = Math.Max(100, Math.Abs(TargetRpm)) + 500,
                MaxAccelRpmPerSec = 5000,
                CommandMinInterval = TimeSpan.FromMilliseconds(5),
                MaxRetries = 3,
                MaxBackoff = TimeSpan.FromSeconds(5)
            };

            IAxisDrive[] drives = new IAxisDrive[AxisCount];
            for (int i = 1; i <= AxisCount; i++) {
                drives[i - 1] = new LeadshineLtdmcAxisDrive(
                    cardNo: CardNo,
                    axisNo: (ushort)i,
                    nodeIndex: (ushort)i,
                    axisId: new AxisId(i),
                    opts: opts,
                    rpmTo60ff: RpmTo60ffScale
                );
            }

            // 3) 使能
            Console.WriteLine("[STEP] Enable...");
            foreach (var d in drives.OfType<LeadshineLtdmcAxisDrive>())
                d.EnableAsync().GetAwaiter().GetResult();

            // 4) 统一设速
            Console.WriteLine($"[STEP] Set ALL => {TargetRpm} rpm");
            Parallel.ForEach(drives, d => d.WriteSpeedAsync(new AxisRpm(TargetRpm)).GetAwaiter().GetResult());

            // 5) 运行保持
            if (HoldSeconds > 0) {
                Console.WriteLine($"[HOLD] running {HoldSeconds}s ...");
                Thread.Sleep(TimeSpan.FromSeconds(HoldSeconds));
            }
            else {
                Console.WriteLine("[HOLD] press any key to STOP ...");
                Console.ReadKey(true);
            }

            // 6) 停机 & 释放
            Console.WriteLine("[STEP] Stop ALL...");
            Parallel.ForEach(drives, d => d.StopAsync().GetAwaiter().GetResult());

            foreach (var d in drives)
                d.DisposeAsync().GetAwaiter().GetResult();

            Console.WriteLine("[DONE] Stopped and disposed.");
        }
        finally {
            // 7) 关闭控制器
            LTDMC.dmc_board_close();
        }
    }
}