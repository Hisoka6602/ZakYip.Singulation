using Microsoft.Extensions.Logging;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    public class RuntimeStatusProvider : IRuntimeStatusProvider {
        private readonly ILogger<RuntimeStatusProvider> _log;
        private readonly object _gate = new();

        private readonly DateTime _startUtc = DateTime.UtcNow;
        private readonly Dictionary<string, TransportStatusItem> _transports = new(StringComparer.OrdinalIgnoreCase);

        private DateTime? _upHeartbeatUtc;
        private double? _upFps;

        private bool _ctrlOnline;
        private string? _ctrlVendor;
        private string? _ctrlIp;
        private int _axisCount;

        public RuntimeStatusProvider(ILogger<RuntimeStatusProvider> log) => _log = log;

        public SystemRuntimeStatus Snapshot() {
            lock (_gate) {
                return new SystemRuntimeStatus {
                    UptimeUtc = _startUtc,
                    Transports = _transports.Values.OrderBy(t => t.Name).ToList(),
                    UpstreamHeartbeatUtc = _upHeartbeatUtc,
                    UpstreamFps = _upFps,
                    AxisCount = _axisCount,
                    ControllerOnline = _ctrlOnline,
                    ControllerVendor = _ctrlVendor,
                    ControllerIp = _ctrlIp,
                    PlannerState = null,
                    PlannerLatencyMsP50 = null,
                    PlannerLatencyMsP95 = null
                };
            }
        }

        public void OnTransportState(string name, string role, string status, string? remote) {
            lock (_gate) {
                if (!_transports.TryGetValue(name, out var item)) {
                    item = new TransportStatusItem { Name = name };
                    _transports[name] = item;
                }

                _transports[name] = new TransportStatusItem {
                    Name = name,
                    Role = role,
                    Status = status,
                    Remote = remote,
                    LastStateChangedUtc = DateTime.UtcNow,
                    ReceivedBytes = item.ReceivedBytes
                };
            }
        }

        public void OnTransportBytes(string name, int bytes) {
            lock (_gate) {
                if (!_transports.TryGetValue(name, out var item)) {
                    item = new TransportStatusItem { Name = name, LastStateChangedUtc = DateTime.UtcNow };
                }
                _transports[name] = new TransportStatusItem {
                    Name = name,
                    Role = item.Role,
                    Status = item.Status,
                    Remote = item.Remote,
                    LastStateChangedUtc = item.LastStateChangedUtc,
                    ReceivedBytes = item.ReceivedBytes + Math.Max(0, bytes)
                };
            }
        }

        public void OnUpstreamHeartbeat(DateTime utc, double? fps = null) {
            lock (_gate) {
                _upHeartbeatUtc = utc;
                _upFps = fps ?? _upFps;
            }
        }

        public void OnControllerInfo(bool online, string? vendor, string? ip, int axisCount) {
            lock (_gate) {
                _ctrlOnline = online;
                _ctrlVendor = vendor;
                _ctrlIp = ip;
                _axisCount = axisCount;
            }
        }
    }
}