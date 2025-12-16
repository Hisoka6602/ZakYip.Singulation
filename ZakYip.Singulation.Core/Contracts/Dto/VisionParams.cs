namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 视觉软件公开的参数视图（通过“获取参数”接口或厂商自定义上行获得）。
    /// </summary>
    public sealed record class VisionParams {
        /// <summary>速度端口号（TCP）。</summary>
        public int SpeedPort { get; init; }

        /// <summary>位置端口号（TCP）。</summary>
        public int PositionPort { get; init; }

        /// <summary>心跳/指令端口号（TCP）。</summary>
        public int HeartbeatPort { get; init; }

        /// <summary>疏散/扩散单元数量。</summary>
        public int EjectUnitCount { get; init; }

        /// <summary>疏散/扩散单元默认速度（mm/s）。</summary>
        public int EjectDefaultMmps { get; init; }

        /// <summary>自动开始触发延时（秒）。</summary>
        public int AutoStartDelaySec { get; init; }

        /// <summary>视觉软件版本（可空）。</summary>
        public string? Version { get; init; }
    }
}
