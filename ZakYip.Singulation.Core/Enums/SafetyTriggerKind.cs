namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 安全触发来源分类。
    /// </summary>
    public enum SafetyTriggerKind {
        Unknown = 0,
        EmergencyStop = 1,
        StopButton = 2,
        StartButton = 3,
        ResetButton = 4,
        AxisFault = 5,
        AxisDisconnected = 6,
        HeartbeatTimeout = 7,
        HealthRecovered = 8,
        CommissioningFailure = 9,
        RemoteStartCommand = 10,
        RemoteStopCommand = 11,
        RemoteResetCommand = 12
    }
}
