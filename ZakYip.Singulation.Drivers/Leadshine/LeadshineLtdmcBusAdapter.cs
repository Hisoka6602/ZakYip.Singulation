using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛 LTDMC 总线适配器：封装 dmc_board_init_eth / nmc_get_total_slaves / nmc_get_errcode / dmc_cool_reset / dmc_board_close。
    /// </summary>
    public sealed class LeadshineLtdmcBusAdapter : IBusAdapter {
        private readonly ushort _cardNo;
        private readonly ushort _portNo;
        private readonly string? _controllerIp;
        private volatile bool _inited;

        public LeadshineLtdmcBusAdapter(ushort cardNo, ushort portNo, string? controllerIp) {
            _cardNo = cardNo;
            _portNo = portNo;
            _controllerIp = string.IsNullOrWhiteSpace(controllerIp) ? null : controllerIp;
        }

        public Task InitializeAsync(CancellationToken ct = default) {
            if (_inited) return Task.CompletedTask;

            short ret = _controllerIp is null
                ? LTDMC.dmc_board_init()
                : LTDMC.dmc_board_init_eth(_cardNo, _controllerIp);

            if (ret != 0) throw new InvalidOperationException($"LTDMC init failed, ret={ret}");

            _inited = true;
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken ct = default) {
            if (!_inited) return Task.CompletedTask;
            LTDMC.dmc_board_close();
            _inited = false;
            return Task.CompletedTask;
        }

        public Task<int> GetAxisCountAsync(CancellationToken ct = default) {
            ushort total = 0;
            var ret = LTDMC.nmc_get_total_slaves(_cardNo, _portNo, ref total);
            if (ret != 0) throw new InvalidOperationException($"nmc_get_total_slaves failed, ret={ret}");
            return Task.FromResult((int)total);
        }

        public Task<int> GetErrorCodeAsync(CancellationToken ct = default) {
            ushort err = 0;
            var ret = LTDMC.nmc_get_errcode(_cardNo, _portNo, ref err);
            if (ret != 0) throw new InvalidOperationException($"nmc_get_errcode failed, ret={ret}");
            return Task.FromResult((int)err);
        }

        public async Task ResetAsync(CancellationToken ct = default) {
            LTDMC.dmc_cool_reset(_cardNo);
            await CloseAsync(ct);
            // 官方经验：冷复位耗时约 15s
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
            await InitializeAsync(ct);
        }

        public async Task WarmResetAsync(CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();

            // 若还未初始化，直接做一次初始化即可
            if (!_inited) {
                await InitializeAsync(ct).ConfigureAwait(false);
                return;
            }

            // 1) 软复位控制器（不掉电）
            var retSoft = LTDMC.dmc_soft_reset(_cardNo);
            if (retSoft != 0) throw new InvalidOperationException($"dmc_soft_reset failed, ret={retSoft}");

            // 2) 关闭当前连接
            LTDMC.dmc_board_close();
            _inited = false;

            // 3) 短暂等待：给控制器复位/网卡栈切换的时间（可按现场调 300~1500ms）
            await Task.Delay(TimeSpan.FromMilliseconds(800), ct).ConfigureAwait(false);

            // 4) 重新初始化（以太网）
            if (string.IsNullOrWhiteSpace(_controllerIp))
                throw new InvalidOperationException("WarmReset requires controller IP for dmc_board_init_eth.");

            var retInit = LTDMC.dmc_board_init_eth(_cardNo, _controllerIp);
            if (retInit != 0) throw new InvalidOperationException($"dmc_board_init_eth failed, ret={retInit}");

            _inited = true;
        }

        public ushort TranslateNodeId(ushort logicalNodeId) {
            // 传入：物理 1,2,3…；输出：逻辑 1001,1002,1003…
            // 若传入已是 1000+，保持不变（防止重复映射）
            return logicalNodeId >= 1000 ? logicalNodeId : (ushort)(1000 + logicalNodeId);
        }
    }
}