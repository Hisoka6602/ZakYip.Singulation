namespace ZakYip.Singulation.Core.Contracts.Events {

    // 小型状态载体，避免闭包分配
    public readonly record struct EvState<T>(object Sender, EventHandler<T> Handler, T Args);
}