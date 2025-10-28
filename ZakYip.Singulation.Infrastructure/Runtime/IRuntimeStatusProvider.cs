using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    public interface IRuntimeStatusProvider {

        SystemRuntimeStatus Snapshot();

        void OnTransportState(string name, string role, string status, string? remote);

        void OnTransportBytes(string name, int bytes);

        void OnUpstreamHeartbeat(DateTime utc, double? fps = null);

        void OnControllerInfo(bool online, string? vendor, string? ip, int axisCount);
    }
}