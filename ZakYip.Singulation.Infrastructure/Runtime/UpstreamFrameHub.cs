using Microsoft.Extensions.Logging;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// UpstreamFrameHub：集中扇出上游帧（Speed/Heartbeat/Position）。
    /// 仅分发，不做解析、统计、重活。保证低延迟：发布为 TryWrite；订阅为有界 DropOldest。
    /// </summary>
    public sealed class UpstreamFrameHub : IUpstreamFrameHub, IDisposable {
        private readonly ILogger<UpstreamFrameHub> _log;

        private readonly object _gate = new();

        private readonly List<Channel<ReadOnlyMemory<byte>>> _speedSubs = new();
        private readonly List<Channel<ReadOnlyMemory<byte>>> _heartbeatSubs = new();

        // —— 本次新增：Position 通道订阅集合 —— //
        private readonly List<Channel<ReadOnlyMemory<byte>>> _positionSubs = new();

        public UpstreamFrameHub(ILogger<UpstreamFrameHub> log) => _log = log;

        // ========== Speed ==========
        public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribeSpeed(int capacity = 256)
            => AddSubscriber(_speedSubs, capacity);

        public void PublishSpeed(ReadOnlyMemory<byte> payload)
            => Broadcast(_speedSubs, payload, nameof(PublishSpeed));

        // ========== Heartbeat ==========
        public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribeHeartbeat(int capacity = 256)
            => AddSubscriber(_heartbeatSubs, capacity);

        public void PublishHeartbeat(ReadOnlyMemory<byte> payload)
            => Broadcast(_heartbeatSubs, payload, nameof(PublishHeartbeat));

        // ========== Position ==========
        public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribePosition(int capacity = 256)
            => AddSubscriber(_positionSubs, capacity);

        public void PublishPosition(ReadOnlyMemory<byte> payload)
            => Broadcast(_positionSubs, payload, nameof(PublishPosition));

        // ========== 内部通用 ==========

        private (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) AddSubscriber(
            List<Channel<ReadOnlyMemory<byte>>> bag, int capacity) {
            if (capacity <= 0) capacity = 256;

            var ch = Channel.CreateBounded<ReadOnlyMemory<byte>>(new BoundedChannelOptions(capacity) {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            lock (_gate) bag.Add(ch);

            var disposer = new Unsubscriber(() => {
                lock (_gate) {
                    if (bag.Remove(ch)) {
                        try { ch.Writer.TryComplete(); } catch { /* ignore */ }
                    }
                }
            });

            return (ch.Reader, disposer);
        }

        private void Broadcast(List<Channel<ReadOnlyMemory<byte>>> bag, ReadOnlyMemory<byte> payload, string op) {
            Channel<ReadOnlyMemory<byte>>[] snapshot;
            lock (_gate) snapshot = bag.ToArray();

            // TryWrite：不阻塞；Channel 满时按照 DropOldest 丢弃旧项，由 Channel 内部处理。
            foreach (var ch in snapshot) {
                try {
                    _ = ch.Writer.TryWrite(payload);
                }
                catch (Exception ex) {
                    // 理论上不会抛；防御性日志，不影响快速扇出。
                    _log.LogDebug(ex, "[Hub] {Op} TryWrite failed (subscriber removed?)", op);
                }
            }
        }

        public void Dispose() {
            lock (_gate) {
                foreach (var ch in _speedSubs.Concat(_heartbeatSubs).Concat(_positionSubs)) {
                    try { ch.Writer.TryComplete(); } catch { /* ignore */ }
                }
                _speedSubs.Clear();
                _heartbeatSubs.Clear();
                _positionSubs.Clear();
            }
        }

        private sealed class Unsubscriber : IDisposable {
            private readonly Action _dispose;

            public Unsubscriber(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}