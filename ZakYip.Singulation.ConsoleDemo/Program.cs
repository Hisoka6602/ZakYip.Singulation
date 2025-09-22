using csLTDMC;
using System.Globalization;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Leadshine;
using ZakYip.Singulation.Drivers.Simulated;
using static System.Net.Mime.MediaTypeNames;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using static System.Runtime.CompilerServices.RuntimeHelpers;

internal static class Program {

    private static void Main(string[] args) {
        // ===== 手动配置区（改这些变量就行） =====
        var axisCount = 24;           // 轴数
        double targetRpm = 1200;        // 目标转速 (rpm)
        ushort cardNo = 8;           // 控制器卡号
        var controllerIp = "192.168.5.11";          // 留空=本地初始化；填IP=以太网初始化，如 "192.168.1.10"
        var rpmTo60FfScale = 1.0;         // rpm → 0x60FF 单位比例（不清楚就先用 1.0）
        var holdSeconds = 0;          // 运行保持时间（秒）；设 0 则按任意键停止
        // =====================================

        // 1) 初始化 LTDMC
        var initRet = string.IsNullOrWhiteSpace(controllerIp)
            ? LTDMC.dmc_board_init()
            : LTDMC.dmc_board_init_eth(cardNo, controllerIp);

        if (initRet != 0) {
            Console.WriteLine($"[ERR] LTDMC init failed, ret={initRet}");
            return;
        }

        try {
            //获取总轴
            ushort totalSlaves = 0;
            LTDMC.nmc_get_total_slaves(cardNo, 2, ref totalSlaves);
            if (totalSlaves > 0) axisCount = totalSlaves;
            //获取总线状态
            ushort errcode = 0;
            do {
                LTDMC.nmc_get_errcode(cardNo, 2, ref errcode);
                if (errcode != 0) {
                    //复位
                    LTDMC.dmc_soft_reset(cardNo);
                    LTDMC.dmc_board_close();

                    for (int i = 0; i < 15; i++)//总线卡软件复位耗时15s左右
                    {
                        Thread.Sleep(1000);
                    }
                    LTDMC.dmc_board_init_eth(cardNo, controllerIp);
                }
            } while (errcode != 0);

            // 2) 准备驱动（NodeID = 1000 + i，axisNo = i，EtherCAT端口=2 在驱动内部固定）
            var opts = new DriverOptions {
                MaxRpm = Math.Max(100, Math.Abs(targetRpm)) + 500,
                MaxAccelRpmPerSec = 5000,
                CommandMinInterval = TimeSpan.FromMilliseconds(5),
                MaxRetries = 3,
                MaxBackoff = TimeSpan.FromSeconds(5)
            };

            var drives = new IAxisDrive[axisCount];
            for (int i = 1; i <= axisCount; i++) {
                drives[i - 1] = new LeadshineLtdmcAxisDrive(
                    cardNo: cardNo,
                    axisNo: (ushort)i,
                    nodeIndex: (ushort)i,
                    axisId: new AxisId(i),
                    opts: opts,
                    rpmTo60ff: rpmTo60FfScale,
                    i % 2 == 0
                );
            }

            // 3) 使能
            Console.WriteLine("[STEP] Enable...");
            foreach (var d in drives.OfType<LeadshineLtdmcAxisDrive>())
                d.EnableAsync().GetAwaiter().GetResult();
            // 3.5) 设置加/减速度（外部统一设置一次，再写速度）
            decimal accelRpmPerSec = 1200;   // 你要的加速度
            decimal decelRpmPerSec = 1200;   // 你要的减速度
            Console.WriteLine($"[STEP] Set Acc/Dec => {accelRpmPerSec}/{decelRpmPerSec} rpm/s");
            Parallel.ForEach(drives, d => {
                d.SetAccelDecelAsync(accelRpmPerSec, decelRpmPerSec).GetAwaiter().GetResult();
            });

            // 4) 统一设速
            Console.WriteLine($"[STEP] Set ALL => {targetRpm} rpm");
            Parallel.ForEach(drives, d => d.WriteSpeedAsync(new AxisRpm(targetRpm)).GetAwaiter().GetResult());

            // 5) 运行保持
            if (holdSeconds > 0) {
                Console.WriteLine($"[HOLD] running {holdSeconds}s ...");
                Thread.Sleep(TimeSpan.FromSeconds(holdSeconds));
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